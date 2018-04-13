using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Clients.Agility.Betshoot.Responses
{
    public class BetsResponse : BetshootResponse
    {
        public TipsterResponse Tipster { get; set; }
        public List<BetResponse> Bets { get; set; }

        public BetsResponse Parse(string html, TipsterResponse tipster, ExtendedTime fromDateClientLocal, TimeZoneKind serverTimezone)
        {
            HandleErrors(html);

            var fromDate = fromDateClientLocal?.ToTimezone(serverTimezone); // konwertuj na strefę czasową serwera, nie znamy czasu serwera, prawdopodobnie UTC
            var newBets = new List<BetResponse>();

            var htmlBettingPicks = html.HtmlRoot()
                .Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("bettingpick"))
                .ToArray();
            
            var i = 0;

            foreach (var bp in htmlBettingPicks)
            {
                OnInformationSending($"Wczytywanie zakładów ({++i} z {htmlBettingPicks.Length})...");
                var strDate = bp.Descendants()
                    .Single(x => x.GetAttributeValue("class", "").Equals("bettingpickdate"))
                    .InnerText;
                var dateArr = strDate.Split('-').Swap(0, 2);
                var date = new ExtendedTime(new DateTime(dateArr[2].ToInt(), dateArr[1].ToInt(), dateArr[0].ToInt()));
                var mResult = bp.Descendants()
                    .Single(x => x.GetAttributeValue("class", "").ContainsAny("mgreen", "mred", "morange", "munits2"))
                    .FirstChild.GetAttributeValue("alt", "");
                
                var pickStr = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("predict")).InnerText.RemoveHTMLSymbols();
                var matchStr = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("pick-teams")).InnerText.RemoveHTMLSymbols();
                var rawMatchResultStr = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("mresult")).InnerText;
                var odds = bp.Descendants().Single(x => x.GetAttributeValue("class", "").Equals("pick-odd")).InnerText.ToDouble();

                var newBet = new BetResponse
                {
                    Date = date,
                    HomeName = matchStr.BeforeFirst(" - "),
                    AwayName = matchStr.AfterFirst(" - "),
                    Pick = PickConverter.ParseToPickResponse(pickStr, matchStr),
                    MatchResult = MatchResultConverter.ParseToMatchResultResponse(rawMatchResultStr),
                    BetResult = BetConverter.ParseBetshootResultStringToBetResult(mResult),
                    Odds = odds
                };

                if (fromDate != null && date < fromDate)
                    break;

                newBets.Add(newBet);
            }

            OnInformationSending("Wczytano zakłady");
            
            return new BetsResponse
            {
                Tipster = tipster,
                Bets = newBets
            };
        }

        public List<DbBet> ToDbBets()
        {
            return Bets.Select(b => b.ToDbBet()).ToList();
        }
    }
}
