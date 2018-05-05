using System;
using BettingBot.Common;
using BettingBot.Source.Clients.Api.FootballData.Responses;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.Models;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source.Converters
{
    public static class MatchConverter
    {
        public static MatchResult ToMatchResultResponse(string matchResult)
        {
            if (matchResult == null)
                return MatchResult.Inconclusive();
            var matchResultStr = matchResult.RemoveHTMLSymbols().Remove(" ");
            if (matchResultStr.Contains("-"))
            {
                var homeScore = matchResultStr.BeforeFirst("-").ToIntN();
                var awayScore = matchResultStr.AfterLast("-").ToIntN();
                if (homeScore != null && awayScore != null)
                    return new MatchResult(homeScore.ToInt(), awayScore.ToInt());
            }
            return MatchResult.Inconclusive();
        }

        public static MatchStatus ToMatchStatus(string matchStatus)
        {
            if (matchStatus.EqIgnoreCase("SCHEDULED"))
                return MatchStatus.Scheduled;
            if (matchStatus.EqIgnoreCase("TIMED"))
                return MatchStatus.Timed;
            if (matchStatus.EqIgnoreCase("POSTPONED"))
                return MatchStatus.Postponed;
            if (matchStatus.EqAnyIgnoreCase("CANCELLED", "CANCELED"))
                return MatchStatus.Cancelled;
            if (matchStatus.EqAnyIgnoreCase("IN_PLAY", "INPLAY"))
                return MatchStatus.InPlay;
            if (matchStatus.EqIgnoreCase("FINISHED"))
                return MatchStatus.Finished;
            throw new InvalidCastException("Nie można przekonwertować wartości string na poprawną dyscyplinę");
        }

        public static DbMatch ToDbMatch(FixtureResponse fixtureResponse)
        {
            return new DbMatch
            {
                Id = fixtureResponse.Id,
                Date = fixtureResponse.Date.ToUTC().Rfc1123,
                Status = fixtureResponse.Status.ToInt(),
                MatchDay = fixtureResponse.Matchday,
                HomeScore = fixtureResponse.HomeTeamScore,
                AwayScore = fixtureResponse.AwayTeamScore,
                HomeScoreHalf = fixtureResponse.HomeTeamScoreHalf,
                AwayScoreHalf = fixtureResponse.AwayTeamScoreHalf,
                HomeId = fixtureResponse.HomeTeamId,
                AwayId = fixtureResponse.AwayTeamId,
                LeagueId = fixtureResponse.CompetitionId
            };
        }

        public static DbMatch CopyWithoutNavigationProperties(DbMatch dbMatch)
        {
            return new DbMatch
            {
                Id = dbMatch.Id,
                Date = dbMatch.Date,
                Status = dbMatch.Status,
                MatchDay = dbMatch.MatchDay,
                HomeScore = dbMatch.HomeScore,
                AwayScore = dbMatch.AwayScore,
                HomeScoreHalf = dbMatch.HomeScoreHalf,
                AwayScoreHalf = dbMatch.AwayScoreHalf,

                HomeId = dbMatch.HomeId,
                AwayId = dbMatch.AwayId,
                LeagueId = dbMatch.LeagueId
            };
        }

        public static MatchToAssociateGvVM ToMatchToAssociateGvVM(DbMatch dbMatch)
        {
            return new MatchToAssociateGvVM
            {
                Id = dbMatch.Id,
                LocalTimestamp = dbMatch.Date.ToExtendedTime().ToLocal(),
                LeagueName = dbMatch.League.Name,
                MatchHomeName = dbMatch.Home.Name,
                MatchAwayName = dbMatch.Away.Name,
            };
        }
    }

    public enum MatchStatus
    {
        Scheduled = 0,
        Timed = 1,
        Postponed = 2,
        Cancelled = 3,
        InPlay = 4,
        Finished = 5
    }
}
