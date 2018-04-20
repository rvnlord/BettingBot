using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class CompetitionsResponse : FootballDataResponse
    {
        public List<CompetitionResponse> Competitions { get; set; } = new List<CompetitionResponse>();

        public CompetitionsResponse Parse(string json)
        {
            HandleErrors(json);

            var jCompetitions = json.ToJArray();
            Competitions = jCompetitions.Select(c => new CompetitionResponse().Parse(c.ToJObject())).ToList();
            return this;
        }
    }
}
