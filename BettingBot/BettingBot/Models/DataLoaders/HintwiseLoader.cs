using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DomainParser.Library;
using MoreLinq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using BettingBot.Common;

namespace BettingBot.Models.DataLoaders
{
    public class HintWiseLoader : DataLoader
    {
        public HintWiseLoader(string url) : base(url)
        {
            Sdm.OpenOrReuseDriver();
            Sdm.NavigateAndWaitForUrl(Url);
        }

        public HintWiseLoader(string url, string login, string password) : base(url)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
                throw new Exception("Niepoprawny login");

            var db = new LocalDbContext();
            if (db.Logins.Any() && db.Logins.Any(l => l.Name == login && l.Password == password))
            {
                User = db.Logins.Single(l => l.Name == login && l.Password == password);
            }
            else
            {
                User = new User(db.Logins.Any() ? db.Logins.Select(l => l.Id).Max() + 1 : 0, login, password);
                db.Logins.Add(User);
            }

            db.SaveChanges();
            db.Dispose();

            Sdm.OpenOrReuseDriver();
            Sdm.NavigateAndWaitForUrl(Url);
            ClosePopups();
        }

        public override string DownloadTipsterName()
        {
            return Sdm.Driver.FindElementByXPath(".//div[@id='content']/div[1]/div[1]/div[1]/div[1]/h4/b").Text;
        }

        public override string DownloadTipsterDomain()
        {
            DomainName completeDomain;
            return DomainName.TryParse(Url.Host, out completeDomain) ? completeDomain.SLD : "";
        }

        public override void EnsureLogin()
        {
            if (IsLogged()) return;
            Login();
            if (!IsLogged()) throw new Exception("Niepoprawne dane logowania do strony");
        }
        
        public override void Login()
        {
            var urlFromBeforeLogin = Sdm.Driver.Url;
            Sdm.NavigateAndWaitForUrl("http://www.hintwise.com/login", 5);
            
            var userNameField = Sdm.Driver.FindElementByName("LoginForm[username]");
            var userPasswordField = Sdm.Driver.FindElementByName("LoginForm[password]");
            var loginButton = Sdm.Driver.FindElementByXPath("//input[@value='Login']");
            
            userNameField.SendKeys(User.Name);
            userPasswordField.SendKeys(User.Password);

            ClosePopups();
            Sdm.ClickAndWaitForUrl(loginButton);
            Sdm.NavigateAndWaitForUrl(urlFromBeforeLogin);
        }

        private void ClosePopups(bool wait = false)
        {
            if (!wait) Sdm.Driver.DisableImplicitWait();
            var closeButtons = Sdm.Driver.FindElementsByXPath("//div[contains(text(), '[Close]') or contains(text(), '[Zamknij]')]").ToList(); //"
            if (!wait) Sdm.Driver.EnableImplicitWait();
            closeButtons.ForEach(cb => cb.Click());
        }

        public override bool IsLogged()
        {
            Sdm.Driver.DisableImplicitWait();
            var isLogged = !Sdm.Driver.FindElementsByXPath(".//a").Any(e => e.Text.ToLower().Contains("login"));
            Sdm.Driver.EnableImplicitWait();
            return isLogged;
        }

        public List<Bet> DownloadTips(DateTime? fromDate = null, bool loadToDb = true)
        {
            var db = new LocalDbContext();
            var tipsterName = DownloadTipsterName();
            var tipsterDomain = DownloadTipsterDomain();
            var tipsterId = db.Tipsters.Single(t => tipsterName == t.Name && tipsterDomain == t.Website.Address).Id;

            var currPredTable = Sdm.Driver.FindElementByXPath(".//div[@id='predictions-grid']");

            Sdm.Driver.DisableImplicitWait();
            var aList = currPredTable.FindElements(By.XPath(".//li[@class='last'][last()]//a"));
            Sdm.Driver.EnableImplicitWait();
            var currPredLastPage = 1;
            NameValueCollection currPredQueries = null;
            if (aList.Count > 0)
            {
                var currPredLastPageLink = aList.Single().GetAttribute("href").RemoveMany("https://", "www.", "hintwise.com");
                var currPredLastPageUri = new Uri($"{Url.Scheme}://{Url.Host}{currPredLastPageLink}");
                currPredQueries = HttpUtility.ParseQueryString(currPredLastPageUri.Query);
                currPredLastPage = Convert.ToInt32(currPredQueries["Prediction_page"]);
            }

            var histPredTable = Sdm.Driver.FindElementByXPath(".//div[@id='history-predictions']");
            var histPredLastPageLink = histPredTable.FindElement(By.XPath(".//li[@class='last'][last()]//a"))
                .GetAttribute("href").RemoveMany("https://", "www.", "hintwise.com");
            var histPredLastPageUri = new Uri($"{Url.Scheme}://{Url.Host}{histPredLastPageLink}");
            var histPredQueries = HttpUtility.ParseQueryString(histPredLastPageUri.Query);
            var histPredLastPage = Convert.ToInt32(histPredQueries["page"]);

            var currBetId = !db.Bets.Any() ? -1 : db.Bets.MaxBy(b => b.Id).Id;
            var previousDate = DateTime.Now;
            var year = previousDate.Year;
            var newBets = new List<Bet>();

            EnsureLogin();

            // Obecne Zakłady

            for (var i = 1; i <= currPredLastPage; i++) // 
            {
                if (currPredLastPage > 1 && currPredQueries != null)
                {
                    var uriBuilder = new UriBuilder(Url);
                    currPredQueries.Set("ajax", "predictions-grid");
                    currPredQueries.Set("Prediction_page", i.ToString());
                    uriBuilder.Query = currPredQueries.ToString();
                    var currResultsPageUri = uriBuilder.Uri;
                    Sdm.NavigateAndWaitForUrl(currResultsPageUri);
                }
                
                var currentBettingPicks = Sdm.Driver.FindElementsByXPath(".//div[@id='predictions-grid']//table[@class='items']/tbody/tr");

                foreach (var bp in currentBettingPicks)
                {
                    var pickCols = bp.FindElements(By.TagName("td"));
                    var noResults = pickCols.Count == 1 && pickCols.Single()
                        .FindElement(By.TagName("span")).Text.ToLower().Contains("no results found");
                    if (noResults) break;
                    Sdm.Driver.DisableImplicitWait();
                    var isFree = pickCols[3].FindElements(By.XPath("input[@type='button']")).Count == 0;
                    Sdm.Driver.EnableImplicitWait();

                    var date = DateTime.ParseExact($"{pickCols[0].Text.RemoveHTMLSymbols().Trim()} 2000", 
                        "d. MMM HH:mm yyyy", new CultureInfo("en-GB"));
                    date = new DateTime(date.Month > previousDate.Date.Month ? --year : year, date.Month, 
                        date.Day, date.Hour, date.Minute, date.Second).ToLocalTime();
                    var matchStr = pickCols[2].FindElement(By.TagName("a")).Text.RemoveHTMLSymbols().Trim();
                    var pickStr = isFree ? pickCols[3].Text.UntilWithout("(").RemoveHTMLSymbols().Trim() : "Ukryty";
                    var parsedPick = Pick.Parse(pickStr, matchStr);
                    var pickId = db.Picks.SingleOrDefault(p => p.Choice == parsedPick.Choice 
                        && p.Value == parsedPick.Value)?.Id;
                    if (pickId == null)
                    {
                        db.Picks.Add(parsedPick);
                        db.SaveChanges();
                        pickId = parsedPick.Id;
                    }

                    var newBet = new Bet
                    {
                        Id = ++currBetId, TipsterId = tipsterId, Date = date.ToLocalTime(),
                        Match = matchStr, PickId = (int) pickId, PickOriginalString = pickStr,
                        MatchResult = "", BetResult = Convert.ToInt32(Result.Pending),
                        Odds = isFree ? Convert.ToDouble(pickCols[3].Text.Between("(", ")")
                            .RemoveHTMLSymbols().Trim(), CultureInfo.InvariantCulture) : 0
                    };
                    newBets.Add(newBet);
                    previousDate = date;
                }
            }

            // Historyczne wyniki

            for (var i = 1; i <= histPredLastPage; i++) // 
            {
                var uriBuilder = new UriBuilder(Url);
                histPredQueries.Set("page", i.ToString());
                uriBuilder.Query = histPredQueries.ToString();
                var histResultsPageUri = uriBuilder.Uri;
                Sdm.NavigateAndWaitForUrl(histResultsPageUri);

                var historyBettingPicks = Sdm.Driver.FindElementsByXPath(".//div[@id='history-predictions']//table[@class='items']/tbody/tr");

                foreach (var bp in historyBettingPicks)
                {
                    var pickCols = bp.FindElements(By.TagName("td"));
                    var noResults = pickCols.Count == 1 && pickCols.Single().FindElement(By.TagName("span")).Text.ToLower().Contains("no results found");
                    if (noResults) break;
                    var date = DateTime.ParseExact($"{pickCols[0].Text.RemoveHTMLSymbols().Trim()} 2000", "d. MMM HH:mm yyyy", new CultureInfo("en-GB"));
                    date = new DateTime(date.Month > previousDate.Date.Month ? --year : year, date.Month, date.Day, date.Hour, date.Minute, date.Second).ToLocalTime();
                    var matchStr = pickCols[2].FindElement(By.TagName("a")).Text.RemoveHTMLSymbols().Trim();
                    var pickStr = pickCols[3].Text.UntilWithout("(").RemoveHTMLSymbols().Trim();
                    var parsedPick = Pick.Parse(pickStr, matchStr);
                    var pickId = db.Picks.SingleOrDefault(p => p.Choice == parsedPick.Choice && p.Value == parsedPick.Value)?.Id;
                    if (pickId == null)
                    {
                        db.Picks.Add(parsedPick);
                        db.SaveChanges();
                        pickId = parsedPick.Id;
                    }

                    var newBet = new Bet
                    {
                        Id = ++currBetId,
                        TipsterId = tipsterId,
                        Date = date,
                        Match = matchStr,
                        PickId = (int) pickId,
                        PickOriginalString = pickStr,
                        MatchResult = pickCols[4].Text.RemoveHTMLSymbols().Trim(),
                        BetResult = Convert.ToInt32(pickCols[5].Text.UntilWithout("(").RemoveHTMLSymbols().Trim().ToLower() == "win"
                            ? Result.Win : Result.Lose),
                        Odds = Convert.ToDouble(pickCols[3].Text.Between("(", ")").RemoveHTMLSymbols().Trim(), CultureInfo.InvariantCulture)
                    };

                    newBets.Add(newBet);
                    previousDate = date;
                    if (fromDate != null && date < fromDate) break;
                }

                if (fromDate != null && newBets.Last().Date < fromDate)
                {
                    newBets.Remove(newBets.Last());
                    break;
                }
            }
            
            if (newBets.Count > 0)
            {
                var minDate = newBets.Select(b => b.Date).Min(); // min d z nowych, zawiera wszystykie z tą datą
                db.Bets.RemoveBy(b => b.Date >= minDate && b.TipsterId == tipsterId);
                db.Bets.AddRange(newBets); //b => new { b.TipsterId, b.Date, b.Match }
                if (loadToDb) db.SaveChanges();
            }
            var bets = db.Bets.ToList();
            db.Dispose();
            return bets;
        }

        public override List<Bet> DownloadTips(bool loadToDb = true)
        {
            return DownloadTips(null, loadToDb);
        }
    }
}
