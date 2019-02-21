using BettingBot.Source.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using Newtonsoft.Json.Linq;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class TeamResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int? SquadMarketValue { get; set; }
        public string CrestUrl { get; set; }

        public TeamResponse Parse(JObject jTeam)
        {
            Id = jTeam["id"].ToInt();
            Name = jTeam["name"].ToStringN();
            ShortName = jTeam["shortName"].ToStringN();
            SquadMarketValue = jTeam["squadMarketValue"].ToIntN();
            CrestUrl = jTeam["crestUrl"].ToStringN();

            return this;
        }

        public DbTeam ToDbTeam()
        {
            return TeamConverter.ToDbTeam(this);
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
