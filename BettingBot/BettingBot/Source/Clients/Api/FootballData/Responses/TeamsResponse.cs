using System.Collections.Generic;
using System.Linq;
using BettingBot.Source.Common;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class TeamsResponse : FootballDataResponse
    {
        public int Count { get; set; }
        public List<TeamResponse> Teams { get; set; } = new List<TeamResponse>();

        public TeamsResponse Parse(string json)
        {
            HandleErrors(json);

            var jTeamsResponse = json.ToJObject();
            Teams = jTeamsResponse["teams"].ToJArray().Select(c => new TeamResponse().Parse(c.ToJObject())).ToList();
            Count = jTeamsResponse["count"].ToInt();
            return this;
        }

        public List<DbTeam> ToDbTeams()
        {
            return Teams.Select(t => t.ToDbTeam()).ToList();
        }
    }
}

//{
//  "count": 10,
//  "teams": [
//    {
//      "id": 1828,
//      "name": "Melbourne City",
//      "shortName": null,
//      "squadMarketValue": null,
//      "crestUrl": null
//    },
//    ( ... )
//  ]
//}
