using System;
using System.Collections.Generic;
using System.Linq;
using DomainParser.Library;
using MoreLinq;
using BettingBot.Common;

namespace BettingBot.Models.DataLoaders
{
    public class BetShootLoader : DataLoader
    {
        public BetShootLoader(string url) : base(url) { }

        public override string DownloadTipsterName()
        {
            return Root.Descendants().Single(n => n.GetAttributeValue("class", "").Equals("top-info")).Descendants("h1").Single().InnerText;
        }

        public override string DownloadTipsterDomain()
        {
            DomainName completeDomain;
            return DomainName.TryParse(Url.Host, out completeDomain) ? completeDomain.SLD : "";
        }

        public override List<Bet> DownloadTips(bool loadToDb = true)
        {
            var db = new LocalDbContext();
            var tipsterName = DownloadTipsterName();
            var tipsterDomain = DownloadTipsterDomain();
            var tipsterId = db.Tipsters.Single(t => tipsterName == t.Name && tipsterDomain == t.Website.Address).Id;
            var newBets = new List<Bet>();

            var htmlBettingPicks = Root
                .Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("bettingpick"));

            var currId = !db.Bets.Any() ? -1 : db.Bets.MaxBy(b => b.Id).Id;

            foreach (var bp in htmlBettingPicks)
            {
                var date = bp.Descendants()
                    .Single(x => x.GetAttributeValue("class", "").Equals("bettingpickdate"))
                    .InnerText;
                var dateArr = date.Split('-').Swap(0, 2);
                date = string.Join("-", dateArr);
                var mResult = bp.Descendants()
                    .Single(x => new[] { "mgreen", "mred", "morange", "munits2" }.Contains(x.GetAttributeValue("class", "")))
                    .FirstChild.GetAttributeValue("alt", "");
                Result betResult;
                if (mResult.Equals("draw", StringComparison.OrdinalIgnoreCase))
                    betResult = Result.Canceled;
                else if (mResult.Equals("won", StringComparison.OrdinalIgnoreCase))
                    betResult = Result.Win;
                else if (mResult.Equals("lost", StringComparison.OrdinalIgnoreCase))
                    betResult = Result.Lose;
                else if (mResult.Equals("half lost", StringComparison.OrdinalIgnoreCase))
                    betResult = Result.HalfLost;
                else if (mResult.Equals("won 1/2", StringComparison.OrdinalIgnoreCase))
                    betResult = Result.HalfWon;
                else if (mResult.Equals("pending", StringComparison.OrdinalIgnoreCase))
                    betResult = Result.Pending;
                else throw new Exception("Błąd Parsowania");

                var pickStr = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("predict")).InnerText.RemoveHTMLSymbols();
                var matchStr = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("pick-teams")).InnerText.RemoveHTMLSymbols();
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
                    Id = ++currId,
                    TipsterId = tipsterId,
                    Date = new DateTime(Convert.ToInt32(dateArr[2]), Convert.ToInt32(dateArr[1]), Convert.ToInt32(dateArr[0])),
                    Match = matchStr,
                    PickId = (int) pickId,
                    PickOriginalString = pickStr,
                    MatchResult = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("mresult")).InnerText.RemoveHTMLSymbols().Replace(" ", "").Replace("-", " - "),
                    BetResult = Convert.ToInt32(betResult),
                    Odds = Convert.ToDouble(bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("pick-odd")).InnerText.Replace(".", ","))
                };
                newBets.Add(newBet);
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

        public override void EnsureLogin()
        { }

        public override void Login()
        { }

        public override bool IsLogged()
        {
            return false;
        }
    }
}
