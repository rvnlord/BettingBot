using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Selenium.Asianodds.Requests;
using BettingBot.Source.Converters;
using MoreLinq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace BettingBot.Source.Clients.Selenium.Asianodds.Responses
{
    public class BetResponse : AsianoddsResponse
    {
        private readonly PickChoice[] _supportedPickChoices = { PickChoice.Home, PickChoice.Away, PickChoice.Draw,
            PickChoice.Over, PickChoice.Under,
            PickChoice.HomeAsianHandicapAdd, PickChoice.HomeAsianHandicapSubtract,
            PickChoice.AwayAsianHandicapAdd, PickChoice.AwayAsianHandicapSubtract };
        
        public ExtendedTime Date { get; set; }
        public DisciplineType? Discipline { get; set; }
        public string LeagueName { get; set; }
        public string MatchHomeName { get; set; }
        public string MatchAwayName { get; set; }
        public PickChoice PickChoice { get; set; }
        public double? PickValue { get; set; }
        public BetResult BetResult { get; set; }
        public double Stake { get; set; }
        public int? MatchId { get; set; }

        public BetResponse Parse(AsianoddsSeleniumDriverManager sdm, BetRequest betRequest, TimeZoneKind tz, AsianoddsClient ao)
        {
            OnInformationSending("Wyszukiwanie zakładu...");

            HandleErrors(sdm);
            
            if (!betRequest.PickChoice?.In(_supportedPickChoices) == true)
                throw new AsianoddsException("Typ zakładu nie jest wspierany przez Asianodds");
            
            var ddlDisciplineType = sdm.FindElementByXPath("//*[@id='selAdvSearchSportType']");
            var optionDisciplines = ddlDisciplineType.FindElements(By.TagName("option")).ToArray();
            var avalDisciplines = optionDisciplines.Select(o => o.Text.ToEnum<DisciplineType>());
            var disciplinesToSearch = (betRequest.Discipline?.ToEnumerable() ?? avalDisciplines).ToArray();
            var searchTerms = $"{betRequest.MatchHomeName} {betRequest.MatchAwayName}".Trim().Split(" ")
                .Select(w => w.Trim().ToLower()).Where(w => w.Length > 2 && !w.ContainsAny("(", ")")).Distinct().ToArray();
            var resultsNum = 0;
            var candidateBets = new List<BetRequest>();
            const string xpathDivSearchInfo = ".//div[@id='gamecontainer']//div[@class='divNoEvent']";
            var txtSearchMatch = sdm.FindElementByXPath("//*[@id='txtFilterGamesAndLeauges']");

            bool timedOut;
            do
            {
                timedOut = sdm.WaitUntilOrTimeout(d => optionDisciplines.All(o => o.Text.Length > 0)); // Poczekaj na załadowanie ddl dyscyplin
                if (timedOut)
                    ao.EnsureLogin();
            } while (timedOut);
            
            bool IsSearchInProgress()
            {
                bool? searchInProgress = null;
                sdm.TryUntilElementAttachedToPage(() =>
                    searchInProgress = sdm.FindElementsByXPath(xpathDivSearchInfo).SingleOrDefault()?.Text
                        .ContainsAny("searching for") == true, true);
                return searchInProgress.ToBool();
            }

            bool areNoResultsFound()
            {
                bool? noResultsFound = null;
                sdm.TryUntilElementAttachedToPage(() => noResultsFound = sdm.FindElementsByXPath(xpathDivSearchInfo).SingleOrDefault()?.Text
                    .ContainsAny("too many", "no result") == true, true);
                return noResultsFound.ToBool();
            }

            IWebElement[] divLeagues = null;
            bool areLeaguesFound()
            {
                divLeagues = null;
                sdm.TryUntilElementAttachedToPage(() =>
                    divLeagues = sdm.FindElementsByXPath(".//div[@id='gamecontainer']//div[@class='ileaugegroup']")
                        .Where(div => !div.FindElement(By.ClassName("leauge")).GetAttribute("value")
                            .ContainsAny("NO. OF", "FANTASY MATCH", "- TO KICK OF", "SCORED")).ToArray(), true);
                return divLeagues?.Any() == true;
            }

            foreach (var discipline in disciplinesToSearch)
            {
                var strDisciplineLocalized = DisciplineConverter.DisciplineTypeToLocalizedString(discipline);
                OnInformationSending($"Przeszukiwanie dyscypliny: {strDisciplineLocalized}...");

                var strD = discipline.EnumToString();
                
                var optionDiscipline = optionDisciplines.Single(o => o.Text.EqIgnoreCase(strD));
                
                ddlDisciplineType.Click();
                optionDiscipline.Click();

                foreach (var searchTerm in searchTerms)
                {
                    OnInformationSending($"Przeszukiwanie dyscypliny: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\"...");

                    var divTimePeriods = (discipline == DisciplineType.Basketball 
                        ? sdm.FindElementsByXPath("//div[@id='lbl_running_basketball' or @id='lbl_today_basketball' or @id='lbl_early_basketball']")
                        : sdm.FindElementsByXPath("//div[@id='lbl_running' or @id='lbl_today' or @id='lbl_early']")).ToArray();
                    if (divTimePeriods.Length != 3)
                        throw new AsianoddsException("Pobrano niepoprawne menu przedziałów czasowych");

                    foreach (var divTimePeriod in divTimePeriods)
                    {
                        var divTimePeriodId = divTimePeriod.GetId();
                        var localizedTimePeriod = divTimePeriodId.ContainsAny("running")
                            ? "na żywo"
                            : divTimePeriodId.ContainsAny("today")
                                ? "dziś"
                                : divTimePeriodId.ContainsAny("early")
                                    ? "późniejsze wydarzenia" : "nieznany przedział czasowy";
                        OnInformationSending($"Przeszukiwanie dyscypliny: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"...");
                        
                        sdm.HideElement(By.XPath("//*[@id='footer']"));
                        divTimePeriod.Click();
                        
                        txtSearchMatch.Clear();
                        txtSearchMatch.SendKeys(searchTerm);
                        
                        OnInformationSending($"Oczekiwanie na rozpoczęcie wyszukiwania: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"...");
                        sdm.Wait.Until(d => IsSearchInProgress());
                        OnInformationSending($"Oczekiwanie na wyniki: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"...");
                        sdm.Wait.Until(d => areNoResultsFound() || areLeaguesFound());
                        var areResultsFound = divLeagues?.Any() == true;

                        if (areResultsFound)
                        {
                            OnInformationSending($"Znaleziono wyniki (ligi: {divLeagues.Length}): {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"");
                            
                            foreach (var divLeague in divLeagues)
                            {
                                var tblGameRows = divLeague.FindElements(By.CssSelector("table.game_panel_tmp.igametable")).ToArray();

                                foreach (var tblGameRow in tblGameRows)
                                {
                                    try
                                    {
                                        string league = null;
                                        sdm.TryUntilElementAttachedToPage(() => league = tblGameRow.FindElement(By.XPath(".//td[1]/input[@class='leauge']")).GetAttribute("value").Trim());
                                        string strTime = null;
                                        sdm.TryUntilElementAttachedToPage(() => strTime = tblGameRow.FindElement(By.XPath(".//td[1]/input[@class='hidKickofTime']")).GetAttribute("value").Trim());
                                        IWebElement tdTeamNames = null;
                                        sdm.TryUntilElementAttachedToPage(() => tdTeamNames = tblGameRow.FindElement(By.XPath(".//td[2]")));

                                        var time = strTime.ToExtendedTime("MM/dd/yyyy hh:mm:ss.fff tt", tz); // 04/28/2018 06:30:00.000 PM
                                        string homeName = null;
                                        sdm.TryUntilElementAttachedToPage(() => homeName = tdTeamNames.FindElement(By.XPath(".//span[contains(@class, 'Home') and contains(@class, 'samehidegroup1')]")).GetAttribute("innerText").Trim());
                                        string awayName = null;
                                        sdm.TryUntilElementAttachedToPage(() => awayName = tdTeamNames.FindElement(By.XPath(".//span[contains(@class, 'Away') and contains(@class, 'samehidegroup1')]")).GetAttribute("innerText").Trim());

                                        bool correctWordCondition(string w) => !w.ContainsAny("(", ")");
                                        var homeSameWords = homeName.SameWords(betRequest.MatchHomeName, 3, correctWordCondition);
                                        var awaySameWords = awayName.SameWords(betRequest.MatchHomeName, 3, correctWordCondition);
                                        var timeDifference = (time - betRequest.Date).Abs();

                                        IWebElement[] spanBetItems = null;
                                        sdm.TryUntilElementAttachedToPage(() => spanBetItems = tblGameRow.FindElements(By.XPath(".//span[contains(@class, 'BetItem')]"))
                                            .Where(bi => !string.IsNullOrWhiteSpace(bi.Text)).ToArray());

                                        foreach (var spanBetItem in spanBetItems)
                                        {
                                            OnInformationSending($"Budowa ścieżki dla elementu nr {++resultsNum}: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"...");
                                            var candidateBetReq = new BetRequest
                                            {
                                                Discipline = discipline,
                                                Date = time,
                                                MatchHomeName = homeName,
                                                MatchAwayName = awayName,
                                                LeagueName = league,

                                                MatchId = betRequest.MatchId,

                                                Keyword = searchTerm,
                                                HomeCommonWords = homeSameWords.Length,
                                                AwayCommonWords = awaySameWords.Length,
                                                TimeDifference = timeDifference,
                                                XPath = spanBetItem.XPath()
                                            };

                                            OnInformationSending($"Aktualizacja elementu nr {++resultsNum}: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"...");

                                            var spanBetItemClasses = spanBetItem.GetClasses();

                                            if (spanBetItemClasses.Contains("FTHomeOdd"))
                                            {
                                                candidateBetReq.PickChoice = PickChoice.Home;
                                                candidateBetReq.PickValue = null;
                                            }
                                            else if (spanBetItemClasses.Contains("FTAwayOdd"))
                                            {
                                                candidateBetReq.PickChoice = PickChoice.Away;
                                                candidateBetReq.PickValue = null;
                                            }
                                            else if (spanBetItemClasses.Contains("FTDrawOdd"))
                                            {
                                                candidateBetReq.PickChoice = PickChoice.Draw;
                                                candidateBetReq.PickValue = null;
                                            }
                                            else if (spanBetItemClasses.ContainsAny("FTHDPHomeOdd", "FTHDPAwayOdd"))
                                            {
                                                double? handicapHome = null;
                                                sdm.TryUntilElementAttachedToPage(() => handicapHome = tblGameRow.FindElement(By.XPath(".//span[contains(@class, 'FTHDPHome') and not(contains(@class, 'BetItem'))]")).Text.Trim().ToDoubleN());
                                                double? handicapAway = null;
                                                sdm.TryUntilElementAttachedToPage(() => handicapAway = tblGameRow.FindElement(By.XPath(".//span[contains(@class, 'FTHDPAway') and not(contains(@class, 'BetItem'))]")).Text.Trim().ToDoubleN());
                                                if (handicapHome == null && handicapAway == null)
                                                    throw new AsianoddsException("Nie można pobrac wartości handicapu dla zakładu");

                                                if (spanBetItemClasses.ContainsAny("FTHDPHomeOdd"))
                                                {
                                                    if (handicapHome != null)
                                                    {
                                                        candidateBetReq.PickChoice = PickChoice.HomeAsianHandicapSubtract;
                                                        candidateBetReq.PickValue = -handicapHome;
                                                    }
                                                    else
                                                    {
                                                        candidateBetReq.PickChoice = PickChoice.HomeAsianHandicapAdd;
                                                        candidateBetReq.PickValue = handicapAway;
                                                    }
                                                }
                                                else if (spanBetItemClasses.ContainsAny("FTHDPAwayOdd"))
                                                {
                                                    if (handicapAway != null)
                                                    {
                                                        candidateBetReq.PickChoice = PickChoice.AwayAsianHandicapSubtract;
                                                        candidateBetReq.PickValue = -handicapAway;
                                                    }
                                                    else
                                                    {
                                                        candidateBetReq.PickChoice = PickChoice.AwayAsianHandicapAdd;
                                                        candidateBetReq.PickValue = handicapHome;
                                                    }
                                                }

                                            }
                                            else if (spanBetItemClasses.ContainsAny("FTOver", "FTUnder"))
                                            {
                                                string[] goalsThresholdSplit = null;
                                                sdm.TryUntilElementAttachedToPage(() => goalsThresholdSplit = tblGameRow.FindElement(By.XPath(".//span[contains(@class, 'FTGoal') and not(contains(@class, 'BetItem'))]")).Text.Trim()
                                                    .Split("-"));
                                                double goals;
                                                if (goalsThresholdSplit.Length == 1)
                                                {
                                                    goals = goalsThresholdSplit.Single().ToDouble();
                                                }
                                                else if (goalsThresholdSplit.Length == 2)
                                                {
                                                    var goalsFromTo = goalsThresholdSplit.Select(x => x.ToDouble()).ToArray();
                                                    goals = goalsFromTo[1] + (goalsFromTo[1] - goalsFromTo[0]) / 2;
                                                }
                                                else
                                                    throw new AsianoddsException("Nie można pobrać progu punktów dla zakładu");

                                                candidateBetReq.PickChoice = spanBetItemClasses.ContainsAny("FTOver") ? PickChoice.Over : PickChoice.Under;
                                                candidateBetReq.PickValue = goals.Round(2);
                                            }
                                            else
                                                continue;

                                            candidateBets.Add(candidateBetReq);

                                            OnInformationSending($"Zaaktualizowano element nr {++resultsNum}: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"...");
                                        }

                                    }
                                    catch (StaleElementReferenceException)
                                    {
                                        OnInformationSending($"Błąd elementu nr {++resultsNum} (element przestał być aktualny): {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"");
                                        // wiersz został usunięty, bo zakład jest np nieaktualny, dotyczy zwłaszcza LIVE
                                    }
                                }
                            }

                            break;
                        }

                        OnInformationSending($"Brak wyników: {strDisciplineLocalized}, słowo kluczowe: \"{searchTerm}\", przedział czasowy: \"{localizedTimePeriod}\"");
                    }
                }
            }

            if (!candidateBets.Any())
                throw new AsianoddsException("Brak zakładów dla wybranego meczu");
            
            var candidateBetsDistinct = candidateBets.Distinct().OrderBy(b => b.MatchHomeName)
                .ThenBy(b => b.PickChoice.EnumToString()).ThenBy(b => b.PickValue).ToList();

            OnInformationSending($"Filtrowanie potencjalnych zakładów {candidateBetsDistinct.Count}...");

            var candidateBetsWithLowestTimeDifference = candidateBetsDistinct.GroupBy(b => b.TimeDifference).Min().ToList();
            var candidateBetsWithSamePick = candidateBetsWithLowestTimeDifference
                .Where(b => b.PickChoice == betRequest.PickChoice && b.PickValue.Eq(betRequest.PickValue)).ToList();
            var finalBet = candidateBetsWithSamePick
                .Where(b => b.HomeCommonWords + b.AwayCommonWords >= 1)
                .MaxBy(b => b.HomeCommonWords + b.AwayCommonWords);

            OnInformationSending("Znaleziono zakład");

            var stake = (betRequest.Stake / 4).Round(); // TODO: użyć API do konwersji walut

            OnInformationSending("Zawieranie zakładu...");

            txtSearchMatch.Clear();
            txtSearchMatch.SendKeys(finalBet.Keyword);
            sdm.Wait.Until(d => IsSearchInProgress());
            sdm.Wait.Until(d => areNoResultsFound() || areLeaguesFound());
            var spanBet = sdm.FindElementByXPath(finalBet.XPath);
            spanBet.Click();

            var txtStake = sdm.FindElementByXPath("//*[@id='betdlg_USR_AMOUNT']");
            var btnPlaceBet = sdm.FindElementByXPath("//*[@id='btnDlgPlaceBetSubmit']");
            txtStake.SendKeys($"{stake:0}");
            sdm.Wait.Until(d => btnPlaceBet.Enabled);
            btnPlaceBet.Click();

            OnInformationSending("Wczytywanie informacji o zakładzie...");

            // załaduj stronę outstanding
            // wczytaj z tabeli postawiony zakład do poniższych właściwości

            Date = finalBet.Date; // TODO: status zakładu z OUTSTANDING
            Discipline = finalBet.Discipline;
            LeagueName = finalBet.LeagueName;
            MatchHomeName = finalBet.MatchHomeName;
            MatchAwayName = finalBet.MatchAwayName;
            PickChoice = (PickChoice) finalBet.PickChoice;
            PickValue = finalBet.PickValue;
            BetResult = BetResult.Pending;
            Stake = stake;
            MatchId = finalBet.MatchId;

            OnInformationSending("Zakład został poprawnie zawarty...");

            return this;
        }

        public override string ToString()
        {
            return $"[{Date.Rfc1123:dd-MM-yyyy HH:mm}] {MatchHomeName} - {MatchAwayName}: {PickChoice.EnumToString()} {PickValue:0.##} (s: {Stake:0.00})";
        }
    }
}
