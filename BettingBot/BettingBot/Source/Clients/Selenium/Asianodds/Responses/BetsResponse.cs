using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using MoreLinq;
using OpenQA.Selenium;

namespace BettingBot.Source.Clients.Selenium.Asianodds.Responses
{
    public class BetsResponse : AsianoddsResponse
    {
        public List<BetResponse> Bets { get; set; }

        public BetsResponse Parse(AsianoddsSeleniumDriverManager sdm, ExtendedTime fromDate, TimeZoneKind timeZone)
        {
            HandleErrors(sdm);
            
            OnInformationSending("Wczytywanie informacji o zakładach...");
            
            sdm.HideElement(By.Id("footer"));
            var ddlDisciplineType = sdm.FindElementByXPath("//*[@id='selAdvSearchSportType']");
            var optionSelectedDiscipline = ddlDisciplineType.FindElements(By.TagName("option")).Single(o => o.Selected);
            var discipline = optionSelectedDiscipline.Text.ToEnum<DisciplineType>();

            var liMenuHistory = sdm.FindElementByXPath("//*[@id='liMenuHistory']");
            liMenuHistory.Click();

            var txtDateFrom = sdm.FindElementByXPath("//*[@id='txtPlacementDateFrom']");
            txtDateFrom.Click();

            var divCalendar = sdm.FindElementByXPath("//*[@id='ui-datepicker-div']");
            IWebElement aDpPrevBtn() => divCalendar.FindElements(By.TagName("a")).Single(a => a.HasClass("ui-datepicker-prev"));
            while (!aDpPrevBtn().HasClass("ui-state-disabled"))
                aDpPrevBtn().Click();
            
            var tdDays = divCalendar.FindElements(By.XPath(".//td[@data-handler='selectDay']"));
            var tdFirstDay = tdDays.MinBy(td => td.FindElement(By.TagName("a")).Text.ToInt());
            tdFirstDay.Click();

            var btnSearchHistory = sdm.FindElementByXPath("//*[@id='btnSearchHistorySearchPanel']");
            btnSearchHistory.TryClickUntilNotCovered();

            const string df = "M/d/yyyy";
            var newBets = new List<BetResponse>();
            IWebElement divHistoricalBets() => sdm.FindElementById("HistoryPageBetsContanier");
            IEnumerable<IWebElement> spanDates() => divHistoricalBets().FindElements(By.ClassName("spanDateDay"))
                .Where(span => span.Text?.Length > 0);

            sdm.Wait.Until(d => spanDates().Any());
            var dates = spanDates().Select(span => span.Text.ToDateTimeExact(df)).Where(d => d.ToExtendedTime() >= fromDate).ToArray();
            var unusedDates = dates.ToList();
            
            while (unusedDates.Any())
            {
                OnInformationSending($"Ładowanie zakładów, dzień {dates.Length - unusedDates.Count + 1} z {dates.Length}...");

                var currDate = unusedDates.Min();

                var spanCurrDate = spanDates().Single(span => span.Text.ToDateTimeExact(df) == currDate);
                var trCurrStatementItem = spanCurrDate.FindElement(By.XPath(".//ancestor::tr[@class='trStatementItem']"));
                trCurrStatementItem.Click();

                var divDayHistoricalBets = sdm.FindElementById("HistoryPageBetsContanier");
                var trBets = divDayHistoricalBets.FindElements(By.ClassName("trSummaryItem")).Where(tr => tr.Displayed).ToArray();

                foreach (var trBet in trBets)
                {
                    var bet = new BetResponse();
                    var spanTeams = trBet.FindElement(By.ClassName("span_homeName_awayName"));
                    var homeAway = spanTeams.Text.AfterFirst("]").Trim().Split(" -VS- ");
                    var home = homeAway[0].BeforeFirst(" - ");
                    var away = homeAway[1].BeforeFirst(" - ");
                    var spanDate = trBet.FindElement(By.ClassName("spanDate"));
                    var strTime = spanDate.Text.Trim();
                    var spanLeague = trBet.FindElement(By.ClassName("span_leagueName"));
                    var strLeague = spanLeague.Text.Trim().AfterFirst("*").Split(" ")
                        .Select(w => w.Length > 0 ? w.Take(1).ToUpper() + w.Skip(1).ToLower() : w).JoinAsString(" ");
                    var spanPick = trBet.FindElement(By.ClassName("span_homeaway_hdporgoal"));
                    var strPick = spanPick.Text.Trim();
                    var spanOdds = trBet.FindElement(By.ClassName("span_odds"));
                    var strOdds = spanOdds.Text.Trim();
                    var spanStatus = trBet.FindElement(By.ClassName("spanStatus"));
                    var strStatus = spanStatus.Text.Trim();
                    var spanStake = trBet.FindElement(By.ClassName("spanStake"));
                    var strStake = spanStake.Text.Trim();
                    var spanScores = trBet.FindElement(By.ClassName("spanScores"));
                    var strMatchResult = spanScores.Text.AfterFirst("FT").Trim();

                    bet.Date = strTime.ToExtendedTime("MM/dd/yyyy h:mm tt", timeZone);
                    bet.Discipline = discipline;
                    bet.LeagueName = strLeague;
                    bet.MatchHomeName = home;
                    bet.MatchAwayName = away;
                    bet.PickChoice = ParsePickChoice(strPick, home, away);
                    bet.PickValue = strPick.ContainsAll("[", "]") ? strPick.Between("[", "]").ToDoubleN()?.Abs() : null;
                    bet.Odds = strOdds.ToDouble();
                    bet.BetResult = ParseBetResult(strStatus);
                    bet.Stake = strStake.ToDouble();
                    bet.HomeScore = strMatchResult.BeforeFirst(":").Trim().ToIntN();
                    bet.AwayScore = strMatchResult.AfterFirst(":").Trim().ToIntN();

                    newBets.Add(bet);
                }

                var btnBackToStatements = sdm.FindElementByClassName("BackToStatements");
                btnBackToStatements.Click();
                
                unusedDates.Remove(currDate);
            }
            
            OnInformationSending("Zakłady zostały pobrane z AsianOdds...");
            
            Bets = newBets;
            return this;
        }

        private PickChoice ParsePickChoice(string strPick, string home, string away)
        {
            PickChoice pickChoice;
            var pickValue = strPick.Between("[", "]").ToDoubleN();
            if (strPick.StartsWithIgnoreCase(home))
            {
                if (pickValue < 0)
                    pickChoice = PickChoice.HomeAsianHandicapSubtract;
                else
                    pickChoice = PickChoice.HomeAsianHandicapAdd;
            }
            else if (strPick.StartsWithIgnoreCase(away))
            {
                if (pickValue < 0)
                    pickChoice = PickChoice.AwayAsianHandicapSubtract;
                else
                    pickChoice = PickChoice.AwayAsianHandicapAdd;
            }
            else if (strPick.StartsWithIgnoreCase("over"))
            {
                pickChoice = PickChoice.Over;
            }
            else if (strPick.StartsWithIgnoreCase("under"))
            {
                pickChoice = PickChoice.Under;
            }
            else if (strPick.EqIgnoreCase("1"))
            {
                pickChoice = PickChoice.Home;
            }
            else if (strPick.EqIgnoreCase("X"))
            {
                pickChoice = PickChoice.Draw;
            }
            else if (strPick.EqIgnoreCase("2"))
            {
                pickChoice = PickChoice.Away;
            }
            else
                throw new ArgumentOutOfRangeException();

            return pickChoice;
        }

        private BetResult ParseBetResult(string strStatus)
        {
            if (strStatus.EqAnyIgnoreCase("Won"))
                return BetResult.Win;
            if (strStatus.EqAnyIgnoreCase("Lost"))
                return BetResult.Lose;
            if (strStatus.EqAnyIgnoreCase("Stake returned"))
                return BetResult.Canceled;
            if (strStatus.EqAnyIgnoreCase("Half won", "Half-won"))
                return BetResult.HalfWon;
            if (strStatus.EqAnyIgnoreCase("Half lost", "Half-lost"))
                return BetResult.HalfLost;
            throw new ArgumentOutOfRangeException();
        }

        public List<DbBet> ToDbBets()
        {
            return Bets.Select(b => b.ToDbBet()).ToList();
        }

    }
}
