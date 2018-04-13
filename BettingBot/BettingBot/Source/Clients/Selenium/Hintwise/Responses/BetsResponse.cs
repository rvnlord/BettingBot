﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Responses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.Models;
using OpenQA.Selenium;

namespace BettingBot.Source.Clients.Selenium.Hintwise.Responses
{
    public class BetsResponse : HintwiseResponse
    {
        public TipsterResponse Tipster { get; set; }
        public List<BetResponse> Bets { get; set; }
        
        public BetsResponse Parse(HintwiseSeleniumDriverManager sdm, TipsterResponse tipster, ExtendedTime fromDateClientLocal, TimeZoneKind serverTimezone)
        {
            HandleErrors(sdm);

            OnInformationSending("Zbieranie dodatkowych informacji...");

            var matchSep = " vs ";
            var originalAddress = sdm.Url;
            var fromDate = fromDateClientLocal?.ToTimezone(serverTimezone); // konwertuj na strefę czasową serwera
            var newBets = new List<BetResponse>();
            
            IWebElement getDivCurrentPredictons() => sdm.FindElementByXPath(".//div[@id='predictions-grid']");
            var divCurrentPredictons = getDivCurrentPredictons();
            var currPredLastPageQueries = sdm.PagerLastPageQueries(divCurrentPredictons);
            var currPredPagesCount = sdm.PagerLastPage(currPredLastPageQueries);

            IWebElement getDivHistoricalPredictions() => sdm.FindElementByXPath(".//div[@id='history-predictions']");
            var divHistoricalPredictions = getDivHistoricalPredictions();
            var histPredLastPageQueries = sdm.PagerLastPageQueries(divHistoricalPredictions);
            var histPredPagesCount = sdm.PagerLastPage(histPredLastPageQueries);
            
            var previousDate = DateTime.Now.ToExtendedTime(TimeZoneKind.CurrentLocal).ToTimezone(serverTimezone); // w strefie czassowej serwera
            var year = previousDate.Rfc1123.Year;

            OnInformationSending("Wczytywanie informacji o zakładach...");

            // Obecne Zakłady

            for (var currPage = 1; currPage <= currPredPagesCount; currPage++)
            {
                OnInformationSending($"Wczytywanie nowych zakładów (strona {currPage} z {currPredPagesCount})...");

                if (divCurrentPredictons.FindElement(By.TagName("span")).Text.ToLower().Contains("no results found"))
                    break;

                sdm.PagerNavigateToCurrentPage(currPage, currPredLastPageQueries, originalAddress);
                divCurrentPredictons = getDivCurrentPredictons();
                var trCurrPredRows = divCurrentPredictons.FindElements(By.XPath(".//table[@class='items']/tbody/tr"));

                foreach (var trCurrPredRow in trCurrPredRows)
                {
                    var tdCurrPredRowCells = trCurrPredRow.FindElements(By.TagName("td"));

                    sdm.DisableWaitingForElements();
                    var isFree = tdCurrPredRowCells[3].FindElements(By.XPath("input[@type='button']")).Count == 0;
                    sdm.EnableWaitingForElements();

                    var extDate = ParseDate(tdCurrPredRowCells[0].Text, previousDate, serverTimezone, ref year);
                    var matchStr = tdCurrPredRowCells[2].FindElement(By.TagName("a")).Text.RemoveHTMLSymbols().Trim();
                    var pickStr = isFree ? tdCurrPredRowCells[3].Text.UntilWithout("(").RemoveHTMLSymbols().Trim() : "Ukryty";

                    var newBet = new BetResponse
                    {
                        Date = extDate, // czas pobierany ma być zawsze w UTC
                        HomeName = matchStr.BeforeFirst(matchSep),
                        AwayName = matchStr.AfterFirst(matchSep),
                        Pick = PickConverter.ParseToPickResponse(pickStr, matchStr),
                        MatchResult = MatchResult.Inconclusive(),
                        BetResult = BetResult.Pending,
                        Odds = 0
                    };
                    newBets.Add(newBet);
                }
            }

            // Historyczne wyniki

            for (var currPage = 1; currPage <= histPredPagesCount; currPage++) // 
            {
                OnInformationSending($"Wczytywanie historycznych zakładów (strona {currPage} z {histPredPagesCount})...");

                if (divHistoricalPredictions.FindElement(By.TagName("span")).Text.ToLower().Contains("no results found"))
                    break;

                sdm.PagerNavigateToCurrentPage(currPage, histPredLastPageQueries, originalAddress);
                divHistoricalPredictions = getDivHistoricalPredictions();
                var trHistPredRows = divHistoricalPredictions.FindElements(By.XPath(".//table[@class='items']/tbody/tr"));

                foreach (var trHistPredRow in trHistPredRows)
                {
                    var tdHistPredRowCells = trHistPredRow.FindElements(By.TagName("td"));

                    var extDate = ParseDate(tdHistPredRowCells[0].Text, previousDate, serverTimezone, ref year);
                    var matchStr = tdHistPredRowCells[2].FindElement(By.TagName("a")).Text.RemoveHTMLSymbols().Trim();
                    var pickStr = tdHistPredRowCells[3].Text.BeforeFirst("(").RemoveHTMLSymbols().Trim();
                    var rawMatchResultStr = tdHistPredRowCells[4].Text.RemoveHTMLSymbols().Trim();
                    var betResult = tdHistPredRowCells[5].Text.BeforeFirst("(").RemoveHTMLSymbols().Trim().ToLower() == "win"
                        ? BetResult.Win : BetResult.Lose;
                    var odds = tdHistPredRowCells[3].Text.Between("(", ")").RemoveHTMLSymbols().Trim().ToDouble();

                    var newBet = new BetResponse
                    {
                        Date = extDate,
                        HomeName = matchStr.BeforeFirst(matchSep),
                        AwayName = matchStr.AfterFirst(matchSep),
                        Pick = PickConverter.ParseToPickResponse(pickStr, matchStr),
                        MatchResult = MatchResultConverter.ParseToMatchResultResponse(rawMatchResultStr),
                        BetResult = betResult,
                        Odds = odds
                    };

                    previousDate = extDate;
                    newBets.Add(newBet);

                    if (fromDate != null && extDate < fromDate)
                        break;
                }

                if (fromDate != null && newBets.Last().Date < fromDate)
                {
                    newBets.Remove(newBets.Last());
                    break;
                }
            }

            OnInformationSending("Wczytano zakłady");

            return new BetsResponse
            {
                Tipster = tipster,
                Bets = newBets
            };
        }

        private static ExtendedTime ParseDate(string strDate, ExtendedTime previousDate, TimeZoneKind serverTimezone, ref int year)
        {
            var date = DateTime.ParseExact($"{strDate.RemoveHTMLSymbols().Trim()} 2000",
                "d. MMM HH:mm yyyy", new CultureInfo("en-GB"));
            var extDate = new DateTime(date.Month > previousDate.Rfc1123.Month
                    ? --year
                    : year, date.Month, date.Day, date.Hour, date.Minute, date.Second)
                .ToExtendedTime(serverTimezone)
                .ToUTC();
            return extDate;
        }

        public List<DbBet> ToDbBets()
        {
            return Bets.Select(b => b.ToDbBet()).ToList();
        }
    }
}
