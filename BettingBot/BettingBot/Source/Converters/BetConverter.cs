using System;
using BettingBot.Common;
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
            if (mResult.Equals("draw"))
                betResult = BetResult.Canceled;
            else if (mResult.EqIgnoreCase("won"))
                betResult = BetResult.Win;
            else if (mResult.EqIgnoreCase("lost"))
                betResult = BetResult.Lose;
            else if (mResult.EqIgnoreCase("half lost"))
                betResult = BetResult.HalfLost;
            else if (mResult.EqIgnoreCase("won 1/2"))
                betResult = BetResult.HalfWon;
            else if (mResult.EqIgnoreCase("pending"))
                betResult = BetResult.Pending;
            else throw new Exception("Błąd Parsowania");
            return betResult;
        }

        public static DbBet ToDbBet(BetshootBetResponse betResponse)
        {
            return new DbBet
            {
                Pick = betResponse.Pick.ToDbPick(),
                Odds = betResponse.Odds,
                OriginalDate = betResponse.Date.Rfc1123,
                OriginalHomeName = betResponse.HomeName,
                OriginalAwayName = betResponse.AwayName,
                OriginalPickString = betResponse.Pick.PickOriginalString,
                OriginalMatchResultString = betResponse.MatchResult.ToString().Remove(" ").Trim(),
                OriginalBetResult = betResponse.BetResult.ToInt(),
                OriginalDiscipline = betResponse.Discipline?.ToInt(),
                OriginalLeagueName = betResponse.LeagueName
            };
        }

        public static DbBet ToDbBet(HintwiseBetResponse betResponse)
        {
            return new DbBet
            {
                Pick = betResponse.Pick.ToDbPick(),
                Odds = betResponse.Odds,
                OriginalDate = betResponse.Date.Rfc1123,
                OriginalHomeName = betResponse.HomeName,
                OriginalAwayName = betResponse.AwayName,
                OriginalPickString = betResponse.Pick.PickOriginalString,
                OriginalMatchResultString = betResponse.MatchResult.ToString().Remove(" ").Trim(),
                OriginalBetResult = betResponse.BetResult.ToInt(),
                OriginalDiscipline = betResponse.Discipline.ToInt()
            };
        }

        public static BetToDisplayGvVM ToBetToDisplayGvVM(DbBet dbBet)
        {
            var betToDisplayVM = new BetToDisplayGvVM();
            var matchResult = MatchConverter.ToMatchResultResponse(dbBet.OriginalMatchResultString);

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
            betToDisplayVM.Discipline = dbBet.Match?.League?.Discipline?.Name.ToEnum<DisciplineType>() ?? dbBet.OriginalDiscipline?.ToEnum<DisciplineType>();
            betToDisplayVM.IsDisciplineOriginal = dbBet.Match?.League?.Discipline == null;
            betToDisplayVM.LeagueName = dbBet.Match?.League?.Name ?? dbBet.OriginalLeagueName;
            betToDisplayVM.IsLeagueNameOriginal = dbBet.Match?.League == null;

            betToDisplayVM.PickChoice = dbBet.Pick.Choice.ToEnum<PickChoice>();
            betToDisplayVM.PickValue = dbBet.Pick.Value;
            betToDisplayVM.SetUnparsedPickString(dbBet.OriginalPickString);

            betToDisplayVM.TriedAssociateWithMatch = dbBet.TriedAssociateWithMatch != null && dbBet.TriedAssociateWithMatch >= 1;

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
            betToDisplayVM.Discipline = oldBetToDisplayGvVM.Discipline;
            betToDisplayVM.IsDisciplineOriginal = oldBetToDisplayGvVM.IsDisciplineOriginal;
            betToDisplayVM.LeagueName = oldBetToDisplayGvVM.LeagueName;
            betToDisplayVM.IsLeagueNameOriginal = oldBetToDisplayGvVM.IsLeagueNameOriginal;

            betToDisplayVM.PickChoice = oldBetToDisplayGvVM.PickChoice;
            betToDisplayVM.PickValue = oldBetToDisplayGvVM.PickValue;
            betToDisplayVM.SetUnparsedPickString(oldBetToDisplayGvVM.GetUnparsedPickString());

            betToDisplayVM.TriedAssociateWithMatch = oldBetToDisplayGvVM.TriedAssociateWithMatch;

            return betToDisplayVM;
        }

        public static string BetResultToLocalizedString(BetResult betResult)
        {
            switch (betResult)
            {
                case BetResult.Win:
                    return "Wygrana";
                case BetResult.Lose:
                    return "Przegrana";
                case BetResult.Canceled:
                    return "Anulowano";
                case BetResult.HalfLost:
                    return "-1/2";
                case BetResult.HalfWon:
                    return "+1/2";
                case BetResult.Pending:
                    return "Oczekuje";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static DbBet CopyWithoutNavigationProperties(DbBet dbBet)
        {
            return new DbBet
            {
                Id = dbBet.Id,
                Odds = dbBet.Odds,
                BetResult  = dbBet.BetResult,

                OriginalDate = dbBet.OriginalDate,
                OriginalBetResult = dbBet.OriginalBetResult,
                OriginalHomeName = dbBet.OriginalHomeName,
                OriginalAwayName = dbBet.OriginalAwayName,
                OriginalMatchResultString = dbBet.OriginalMatchResultString,
                OriginalPickString = dbBet.OriginalPickString,
                OriginalDiscipline = dbBet.OriginalDiscipline,
                OriginalLeagueName = dbBet.OriginalLeagueName,

                TriedAssociateWithMatch = dbBet.TriedAssociateWithMatch,
                TipsterId = dbBet.TipsterId,
                MatchId = dbBet.MatchId,
                PickId = dbBet.PickId
            };
        }

        public static BetToAssociateGvVM ToBetToAssociateGvVM(BetToDisplayGvVM betToDisplayGvVM)
        {
            return new BetToAssociateGvVM
            {
                Id = betToDisplayGvVM.Id,
                LocalTimestamp = betToDisplayGvVM.LocalTimestamp,
                TipsterName = betToDisplayGvVM.TipsterName,
                LeagueName = betToDisplayGvVM.LeagueName,
                MatchHomeName = betToDisplayGvVM.MatchHomeName,
                MatchAwayName = betToDisplayGvVM.MatchAwayName,
            };
        }

        public static BetToAssociateGvVM ToBetToAssociateGvVM(DbBet dbBet)
        {
            return new BetToAssociateGvVM
            {
                Id = dbBet.Id,
                LocalTimestamp = dbBet.OriginalDate.ToExtendedTime().ToLocal(),
                TipsterName = dbBet.Tipster.Name,
                LeagueName = dbBet.OriginalLeagueName,
                MatchHomeName = dbBet.OriginalHomeName,
                MatchAwayName = dbBet.OriginalAwayName,
                MatchId = dbBet.MatchId
            };
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
