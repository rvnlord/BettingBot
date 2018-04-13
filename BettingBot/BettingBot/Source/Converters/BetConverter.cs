using System;
using BettingBot.Common;
using BettingBot.Source.Clients.Agility.Betshoot.Responses;
using BettingBot.Source.Clients.Selenium.Hintwise.Responses;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using BetshootBetResponse = BettingBot.Source.Clients.Agility.Betshoot.Responses.BetResponse;
using HintwiseBetResponse = BettingBot.Source.Clients.Selenium.Hintwise.Responses.BetResponse;

namespace BettingBot.Source.Converters
{
    public static class BetConverter
    {
        public static BetResult ParseBetshootResultStringToBetResult(string mResult)
        {
            BetResult betResult;
            if (mResult.Equals("draw", StringComparison.OrdinalIgnoreCase))
                betResult = BetResult.Canceled;
            else if (mResult.Equals("won", StringComparison.OrdinalIgnoreCase))
                betResult = BetResult.Win;
            else if (mResult.Equals("lost", StringComparison.OrdinalIgnoreCase))
                betResult = BetResult.Lose;
            else if (mResult.Equals("half lost", StringComparison.OrdinalIgnoreCase))
                betResult = BetResult.HalfLost;
            else if (mResult.Equals("won 1/2", StringComparison.OrdinalIgnoreCase))
                betResult = BetResult.HalfWon;
            else if (mResult.Equals("pending", StringComparison.OrdinalIgnoreCase))
                betResult = BetResult.Pending;
            else throw new Exception("Błąd Parsowania");
            return betResult;
        }

        public static DbBet ToDbBet(BetshootBetResponse betResponse)
        {
            return new DbBet
            {
                OriginalDate = betResponse.Date.Rfc1123,
                OriginalHomeName = betResponse.HomeName,
                OriginalAwayName = betResponse.AwayName,
                OriginalPickString = betResponse.Pick.PickOriginalString,
                OriginalMatchResultString = betResponse.MatchResult.ToString().Remove(" ").Trim(),
                Pick = betResponse.Pick.ToDbPick(),
                OriginalBetResult = betResponse.BetResult.ToInt(),
                Odds = betResponse.Odds
            };
        }

        public static DbBet ToDbBet(HintwiseBetResponse betResponse)
        {
            return new DbBet
            {
                OriginalDate = betResponse.Date.Rfc1123,
                OriginalHomeName = betResponse.HomeName,
                OriginalAwayName = betResponse.AwayName,
                OriginalPickString = betResponse.Pick.PickOriginalString,
                OriginalMatchResultString = betResponse.MatchResult.ToString().Remove(" ").Trim(),
                Pick = betResponse.Pick.ToDbPick(),
                OriginalBetResult = betResponse.BetResult.ToInt(),
                Odds = betResponse.Odds
            };
        }

        public static BetToDisplayGvVM ToBetToDisplayGvVM(DbBet dbBet)
        {
            var betToDisplayVM = new BetToDisplayGvVM();
            var matchResult = MatchResultConverter.ParseToMatchResultResponse(dbBet.OriginalMatchResultString);

            betToDisplayVM.IsAssociatedWithArbitraryData = dbBet.MatchId != null;

            betToDisplayVM.Id = dbBet.Id;
            betToDisplayVM.BetResult = (dbBet.BetResult ?? dbBet.OriginalBetResult).ToEnum<BetResult>();
            betToDisplayVM.IsBetResultOriginal = dbBet.BetResult == null;
            betToDisplayVM.Odds = dbBet.Odds;

            betToDisplayVM.TipsterName = dbBet.Tipster?.Name;
            betToDisplayVM.TipsterWebsite = dbBet.Tipster?.Link?.UrlToDomain();

            betToDisplayVM.MatchHomeName = dbBet.Match?.Home?.Name ?? dbBet.OriginalHomeName;
            betToDisplayVM.IsMatchHomeNameOriginal = dbBet.Match?.Home?.Name == null;
            betToDisplayVM.MatchAwayName = dbBet.Match?.Away?.Name ?? dbBet.OriginalAwayName;
            betToDisplayVM.IsMatchAwayNameOriginal = dbBet.Match?.Away?.Name == null;
            betToDisplayVM.SetUnparsedMatchString(dbBet.OriginalHomeName, dbBet.OriginalAwayName);
            betToDisplayVM.MatchHomeScore = dbBet.Match?.HomeScore ?? matchResult.HomeScore;
            betToDisplayVM.IsMatchHomeScoreOriginal = dbBet.Match?.HomeScore == null;
            betToDisplayVM.MatchAwayScore = dbBet.Match?.AwayScore ?? matchResult.AwayScore;
            betToDisplayVM.IsMatchAwayScoreOriginal = dbBet.Match?.AwayScore == null;
            betToDisplayVM.LocalTimestamp = (dbBet.Match?.Date ?? dbBet.OriginalDate).ToExtendedTime().ToLocal();
            betToDisplayVM.IsLocalTimestampOriginal = dbBet.Match?.Date == null;

            betToDisplayVM.PickChoice = dbBet.Pick.Choice.ToEnum<PickChoice>();
            betToDisplayVM.PickValue = dbBet.Pick.Value;
            betToDisplayVM.SetUnparsedPickString(dbBet.OriginalPickString);
            
            return betToDisplayVM;
        }

        public static BetToDisplayGvVM CopyBetToDisplayGvVM(BetToDisplayGvVM oldBetToDisplayGvVM)
        {
            var betToDisplayVM = new BetToDisplayGvVM();

            betToDisplayVM.IsAssociatedWithArbitraryData = oldBetToDisplayGvVM.IsAssociatedWithArbitraryData;

            betToDisplayVM.Id = oldBetToDisplayGvVM.Id;
            betToDisplayVM.BetResult = oldBetToDisplayGvVM.BetResult;
            betToDisplayVM.IsBetResultOriginal = oldBetToDisplayGvVM.IsBetResultOriginal;
            betToDisplayVM.Odds = oldBetToDisplayGvVM.Odds;
            betToDisplayVM.TipsterName = oldBetToDisplayGvVM.TipsterName;
            betToDisplayVM.TipsterWebsite = oldBetToDisplayGvVM.TipsterWebsite;
            betToDisplayVM.MatchHomeName = oldBetToDisplayGvVM.MatchHomeName;
            betToDisplayVM.IsMatchHomeNameOriginal = oldBetToDisplayGvVM.IsMatchHomeNameOriginal;
            betToDisplayVM.MatchAwayName = oldBetToDisplayGvVM.MatchAwayName;
            betToDisplayVM.IsMatchAwayNameOriginal = oldBetToDisplayGvVM.IsMatchAwayNameOriginal;
            var teamSeparator = " - ";
            var unparsedMatchString = oldBetToDisplayGvVM.GetUnparsedMatchString();
            betToDisplayVM.SetUnparsedMatchString(unparsedMatchString.BeforeFirst(teamSeparator), unparsedMatchString.AfterFirst(teamSeparator));
            betToDisplayVM.MatchHomeScore = oldBetToDisplayGvVM.MatchHomeScore;
            betToDisplayVM.IsMatchHomeScoreOriginal = oldBetToDisplayGvVM.IsMatchHomeScoreOriginal;
            betToDisplayVM.MatchAwayScore = oldBetToDisplayGvVM.MatchAwayScore;
            betToDisplayVM.IsMatchAwayScoreOriginal = oldBetToDisplayGvVM.IsMatchAwayScoreOriginal;
            betToDisplayVM.LocalTimestamp = oldBetToDisplayGvVM.LocalTimestamp;
            betToDisplayVM.IsLocalTimestampOriginal = oldBetToDisplayGvVM.IsLocalTimestampOriginal;

            betToDisplayVM.PickChoice = oldBetToDisplayGvVM.PickChoice;
            betToDisplayVM.PickValue = oldBetToDisplayGvVM.PickValue;
            betToDisplayVM.SetUnparsedPickString(oldBetToDisplayGvVM.GetUnparsedPickString());

            return betToDisplayVM;
        }
    }

    public enum BetResult
    {
        Lose = 0,
        Win = 1,
        Canceled = 2,
        HalfLost = 3,
        HalfWon = 4,
        Pending = 5
    }
}
