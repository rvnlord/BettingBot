using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Clients.Agility.Betshoot.Responses
{
    public class BetsResponse : BetshootResponse
    {
        public TipsterResponse Tipster { get; set; }
        public List<BetResponse> Bets { get; set; }

        public BetsResponse Parse(string html, AgilityRestManager arm, TipsterResponse tipster, ExtendedTime fromDateClientLocal, TimeZoneKind serverTimezone)
        {
            HandleErrors(html);

            var fromDate = fromDateClientLocal?.ToTimezone(serverTimezone); // konwertuj na strefę czasową serwera, nie znamy czasu serwera, prawdopodobnie UTC
            var newBets = new List<BetResponse>();

            var spanBettingPicks = html.HtmlRoot()
                .Descendants()
                .Where(n => n.HasClass("bettingpick"))
                .ToArray();
            
            var i = 0;

            var brpLs = LocalizationManager.GetLoaderBetshootResponseParseLocalizedStrings();

            foreach (var spanBp in spanBettingPicks)
            {
                OnInformationSending($"{brpLs[0][0]} ({++i} {brpLs[0][1]} {spanBettingPicks.Length})..."); // ($"Wczytywanie zakładów ({++i} z {spanBettingPicks.Length})...");
                var strDate = spanBp.Descendants()
                    .Single(x => x.HasClass("bettingpickdate"))
                    .InnerText;
                var dateArr = strDate.Split('.'); // year is currently always 2020
                var year = dateArr.Length > 2 ? dateArr[2] : DateTime.UtcNow.Year.ToString();
                var date = new ExtendedTime(new DateTime(year.ToInt(), dateArr[1].ToInt(), dateArr[0].ToInt()));
                var spanBetResultClass = spanBp.Descendants("span")
                    .Single(span => span.HasClass("mgreen", "mred", "morange", "munits2"))
                    .GetOnlyClass(); // fix opisany w konwerterze
                var stake = spanBp.Descendants("span").Single(span => span.HasClass("pick-stake")).InnerText.Trim().ToInt();
                var profit = spanBp.Descendants("span").Single(span => span.HasClass("munits")).InnerText.Trim().ToDoubleN();

                var pickStr = spanBp.Descendants("span").Single(span => span.HasClass("predict")).InnerText.RemoveHTMLSymbols();
                var spanPickTeams = spanBp.Descendants("span").Single(span => span.HasClass("pick-teams"));
                var aPickTeams = spanPickTeams.Descendants("a").SingleOrDefault();
                var matchStr = (aPickTeams ?? spanPickTeams).InnerText.RemoveHTMLSymbols();
                var rawMatchResultStr = spanBp.Descendants("span").Single(x => x.HasClass("mresult")).InnerText;
                var odds = spanBp.Descendants("span").Single(x => x.HasClass("pick-odd")).InnerText.ToDouble();

                var hrefAPickTeams = aPickTeams?.GetAttributeValue("href", "");
                string disciplineStr = null;
                string leagueName = null;
                if (hrefAPickTeams != null)
                {
                    var divs = arm.GetHtml(hrefAPickTeams).HtmlRoot().Descendants("div").ToArray();
                    var divBetContainer = divs.Single(div => div.HasClass("post-byline"));
                    var divsBetInfo = divBetContainer.Descendants("div").ToArray();
                    var disciplineLeagueStr = divsBetInfo[1].InnerText.Trim().RemoveHTMLSymbols().Split(" - ");
                    var dateStr = divsBetInfo.Single(p => p.InnerText.Trim().EndsWithAny(tipster.Name)).InnerText.RemoveHTMLSymbols().Between("Posted: ", " By ");
                    var tzOp = dateStr.First(c => c.In('+', '-')).ToString();
                    var timeShift = dateStr.After(tzOp).BeforeFirstOrWhole(" ").ToInt() * (tzOp == "+" ? 1 : -1);
                    var dateSplit = dateStr.Before(" ", 5).SplitByMany(" at ", ":", " ");
                    date = new ExtendedTime(
                        dateSplit[2].ToInt(), 
                        dateSplit[1].MonthNameToNumber(), 
                        dateSplit[0].ToInt(), 
                        (dateSplit[3].ToInt() + -timeShift) % 24,
                        dateSplit[4].ToInt(), 0);
                    disciplineStr = disciplineLeagueStr[0];
                    leagueName = disciplineLeagueStr[1];
                }
                
                var newBet = new BetResponse
                {
                    Date = date,
                    HomeName = matchStr.BeforeFirst(" vs "),
                    AwayName = matchStr.AfterFirst(" vs "),
                    Pick = PickConverter.ParseToPickResponse(pickStr, matchStr),
                    MatchResult = MatchConverter.ToMatchResultResponse(rawMatchResultStr),
                    BetResult = BetConverter.ParseBetshootResultStringToBetResult(spanBetResultClass, stake, odds, profit),
                    Odds = odds,
                    Discipline = DisciplineConverter.ToDisciplineTypeOrNull(disciplineStr),
                    LeagueName = leagueName
                };

                if (fromDate != null && date < fromDate)
                    break;

                newBets.Add(newBet);
            }

            OnInformationSending(brpLs[1][0]);

            Tipster = tipster;
            Bets = newBets;
            return this;
        }

        public List<DbBet> ToDbBets()
        {
            return Bets.Select(b => b.ToDbBet()).ToList();
        }
    }
}
