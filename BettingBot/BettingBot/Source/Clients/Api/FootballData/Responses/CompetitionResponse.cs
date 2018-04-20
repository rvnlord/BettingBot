using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using Newtonsoft.Json.Linq;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class CompetitionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int Year { get; set; }
        public int CurrentMatchday { get; set; }
        public int TotalMatchdays { get; set; }
        public int TeamsNumber { get; set; }
        public int GamesNumber { get; set; }
        public ExtendedTime LastUpdated { get; set; }

        public CompetitionResponse Parse(JObject jCompetition)
        {
            Id = jCompetition["id"].ToInt();
            Name = jCompetition["caption"].ToString();
            ShortName = jCompetition["league"].ToString();
            Year = jCompetition["year"].ToInt();
            CurrentMatchday = jCompetition["currentMatchday"].ToInt();
            TotalMatchdays = jCompetition["numberOfMatchdays"].ToInt();
            TeamsNumber = jCompetition["numberOfTeams"].ToInt();
            GamesNumber = jCompetition["numberOfGames"].ToInt();
            LastUpdated = jCompetition["lastUpdated"].ToExtendedTimeN("yyyy-MM-dd'T'HH:mm:ss'Z'");

            return this;
        }

        public DbLeague ToDbLeague()
        {
            return LeagueConverter.ToDbLeague(this);
        }
    }
}

//{
//  "id": 458,
//  "caption": "DFB-Pokal 2017/18",
//  "league": "DFB",
//  "year": "2017",
//  "currentMatchday": 5,
//  "numberOfMatchdays": 6,
//  "numberOfTeams": 64,
//  "numberOfGames": 62,
//  "lastUpdated": "2018-04-03T06:26:04Z"
//},
