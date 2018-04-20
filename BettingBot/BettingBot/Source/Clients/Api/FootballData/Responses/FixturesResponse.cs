using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class FixturesResponse : FootballDataResponse
    {
        public ExtendedTime From { get; set; }
        public ExtendedTime To { get; set; }
        public int Count { get; set; }
        public List<FixtureResponse> Fixtures { get; set; } = new List<FixtureResponse>();

        public FixturesResponse Parse(string json)
        {
            HandleErrors(json);

            const string dateFormat = "yyyy-MM-dd";
            var jFixturesResponse = json.ToJObject();
            Fixtures = jFixturesResponse["fixtures"].ToJArray().Select(f => new FixtureResponse().Parse(f.ToJObject())).ToList();
            Count = jFixturesResponse["count"].ToInt();
            From = jFixturesResponse.VorN("timeFrameStart").ToExtendedTimeN(dateFormat);
            To = jFixturesResponse.VorN("timeFrameEnd").ToExtendedTimeN(dateFormat);

            return this;
        }

        public List<DbMatch> ToDbMatches()
        {
            return Fixtures.Select(f => f.ToDbMatch()).ToList();
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