using System;
using System.Collections.Generic;
using BettingBot.Source.Clients.Api.FootballData.Responses;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Converters
{
    public class LeagueConverter
    {
        public static DbLeague ToDbLeague(CompetitionResponse competitionResponse)
        {
            return new DbLeague
            {
                Id = competitionResponse.Id,
                Name = competitionResponse.Name,
                Season = competitionResponse.Year,
                LeagueAlternateNames = new List<DbLeagueAlternateName>
                {
                    new DbLeagueAlternateName
                    {
                        LeagueId = competitionResponse.Id,
                        AlternateName = competitionResponse.ShortName
                    }
                },
            };
        }
        
        public static DbLeague CopyWithoutNavigationProperties(DbLeague dbLeague)
        {
            return new DbLeague
            {
                Id = dbLeague.Id,
                Name = dbLeague.Name,
                Season = dbLeague.Season,
                DisciplineId = dbLeague.DisciplineId
            };
        }

        public static DbLeagueAlternateName CopyWithoutNavigationProperties(DbLeagueAlternateName dbLeagueAlternateName)
        {
            return new DbLeagueAlternateName
            {
                AlternateName = dbLeagueAlternateName.AlternateName,
                LeagueId = dbLeagueAlternateName.LeagueId
            };
        }
    }
}
