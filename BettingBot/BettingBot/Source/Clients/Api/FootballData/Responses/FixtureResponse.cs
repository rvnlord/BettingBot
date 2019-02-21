using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using Newtonsoft.Json.Linq;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class FixtureResponse
    {
        public int Id { get; set; }
        public int CompetitionId { get; set; }
        public ExtendedTime Date { get; set; }
        public int Matchday { get; set; }
        public string HomeTeamName { get; set; }
        public int HomeTeamId { get; set; }
        public string AwayTeamName { get; set; }
        public int AwayTeamId { get; set; }
        public int? HomeTeamScore { get; set; }
        public int? AwayTeamScore { get; set; }
        public int? HomeTeamScoreHalf { get; set; }
        public int? AwayTeamScoreHalf { get; set; }
        public MatchStatus Status { get; set; }
        
        public FixtureResponse Parse(JObject jFixture)
        {
            var jFullTimeResult = jFixture.VorN("result").ToJObject();
            var jHalfTimeResult = jFullTimeResult.VorN("halfTime").ToJObject();

            Id = jFixture["id"].ToInt();
            CompetitionId = jFixture["competitionId"].ToInt();
            Date = jFixture["date"].ToExtendedTimeN("yyyy-MM-dd'T'HH:mm:ss'Z'");
            Matchday = jFixture["matchday"].ToInt();
            HomeTeamName = jFixture["homeTeamName"].ToStringN();
            HomeTeamId = jFixture["homeTeamId"].ToInt();
            AwayTeamName = jFixture["awayTeamName"].ToStringN();
            AwayTeamId = jFixture["awayTeamId"].ToInt();
            HomeTeamScore = jFullTimeResult.VorN("goalsHomeTeam").ToIntN();
            AwayTeamScore = jFullTimeResult.VorN("goalsAwayTeam").ToIntN();
            HomeTeamScoreHalf = jHalfTimeResult.VorN("goalsHomeTeam").ToIntN();
            AwayTeamScoreHalf = jHalfTimeResult.VorN("goalsAwayTeam").ToIntN();
            Status = jFixture["status"].ToMatchStatus();

            return this;
        }

        public DbMatch ToDbMatch()
        {
            return MatchConverter.ToDbMatch(this);
        }
    }
}

//{
//  "count": 5,
//  "fixtures": [
//    {
//      "id": 164921,
//      "competitionId": 466,
//      "date": "2018-04-13T09:50:00Z",
//      "status": "FINISHED",
//      "matchday": 27,
//      "homeTeamName": "Central Coast Mariners FC",
//      "homeTeamId": 1830,
//      "awayTeamName": "Newcastle Jets FC",
//      "awayTeamId": 1829,
//      "result": {
//        "goalsHomeTeam": 2,
//        "goalsAwayTeam": 8,
//        "halfTime": {
//          "goalsHomeTeam": 1,
//          "goalsAwayTeam": 3
//        }
//      },
//      "odds": null
//    },
//    ( ... )
//  ]
//}
