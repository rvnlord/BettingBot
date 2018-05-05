using System;
using BettingBot.Common;
using BettingBot.Source.Clients.Selenium.Asianodds.Requests;
using BettingBot.Source.Clients.Selenium.Asianodds.Responses;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using BetshootBetResponse = BettingBot.Source.Clients.Agility.Betshoot.Responses.BetResponse;
using HintwiseBetResponse = BettingBot.Source.Clients.Selenium.Hintwise.Responses.BetResponse;

namespace BettingBot.Source.Converters
{
    public static class BetConverter
    {
        public static BetResult ParseBetshootResultStringToBetResult(string mResultClass, int stake, double odds, double? profit)
        {
            if (mResultClass == null) throw new ArgumentNullException(nameof(mResultClass));
            BetResult betResult;
            if (mResultClass.EqIgnoreCase("morange"))
                betResult = BetResult.Canceled;
            else if (mResultClass.EqIgnoreCase("mgreen"))
                betResult = (stake * odds - stake).Eq(profit.ToDouble()) ? BetResult.Win : BetResult.HalfWon;
            else if (mResultClass.EqIgnoreCase("mred"))
                betResult = (-profit.ToDouble()).Eq(stake) ? BetResult.Lose : BetResult.HalfLost;
            else if (mResultClass.EqIgnoreCase("munits2"))
                betResult = BetResult.Pending;
            else throw new Exception("Błąd Parsowania");
            return betResult;
                // fix: zmiana na betshoot - span o klasie m<sth> nie zawiera już informacji o statusie zakładu 
                // w elemencie imb / attr alt, trzeba parsować po klasie i stawce zakładu
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

            betToDisplayVM.MatchId = dbBet.MatchId;
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
            betToDisplayVM.MatchId = oldBetToDisplayGvVM.MatchId;

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
                OriginalStake = dbBet.OriginalStake,

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
                MatchId = betToDisplayGvVM.MatchId
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

        public static BetRequest ToBetRequest(BetToDisplayGvVM betToDisplayGvVm)
        {
            return new BetRequest
            {
                Date = betToDisplayGvVm.LocalTimestamp.ToUTC(),
                Discipline = betToDisplayGvVm.Discipline,
                LeagueName = betToDisplayGvVm.LeagueName,
                MatchHomeName = betToDisplayGvVm.MatchHomeName,
                MatchAwayName = betToDisplayGvVm.MatchAwayName,
                PickChoice = betToDisplayGvVm.PickChoice,
                PickValue = betToDisplayGvVm.PickValue,
                Stake = betToDisplayGvVm.Stake,
                MatchId = betToDisplayGvVm.MatchId
            };
        }

        public static DbBet ToDbBet(BetResponse betResponse)
        {
            return new DbBet
            {
                OriginalDate = betResponse.Date.ToUTC().Rfc1123,
                BetResult = betResponse.BetResult.ToInt(),
                MatchId = betResponse.MatchId,
                Odds = betResponse.Odds,
                OriginalStake = betResponse.Stake,
                Pick = new DbPick { Choice = betResponse.PickChoice, Value = betResponse.PickValue },
                OriginalHomeName = betResponse.MatchHomeName,
                OriginalAwayName = betResponse.MatchAwayName,
                OriginalLeagueName = betResponse.LeagueName,
                OriginalDiscipline = betResponse.Discipline.ToIntN()
            };
        }

        public static SentBetGvVM ToSentBetGvVM(DbBet dbBet)
        {
            return new SentBetGvVM
            {
                Id = dbBet.Id,
                LocalTimestamp = dbBet.OriginalDate.ToExtendedTime().ToLocal(),
                BetResult = dbBet.BetResult.ToEnum<BetResult>(),
                MatchId = dbBet.MatchId,
                Odds = dbBet.Odds,
                Stake = dbBet.OriginalStake.ToDouble() * 4, // TODO: z API do walut
                PickChoice = dbBet.Pick.Choice,
                PickValue = dbBet.Pick.Value,
                HomeName = dbBet.OriginalHomeName,
                AwayName = dbBet.OriginalAwayName,
                LeagueName = dbBet.OriginalLeagueName,
                Discipline = dbBet.OriginalDiscipline.ToEnum<DisciplineType>()
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
