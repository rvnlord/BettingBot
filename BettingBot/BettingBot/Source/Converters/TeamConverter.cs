using System;
using System.Collections.Generic;
using BettingBot.Source.Clients.Api.FootballData.Responses;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Converters
{
    public class TeamConverter
    {
        public static DbTeam ToDbTeam(TeamResponse teamResponse)
        {
            var teamAlternateNames = new List<DbTeamAlternateName>();
            if (!string.IsNullOrWhiteSpace(teamResponse.ShortName))
            {
                teamAlternateNames.Add(new DbTeamAlternateName
                {
                    TeamId = teamResponse.Id,
                    AlternateName = teamResponse.ShortName
                });
            } // fix: dla przypisania wartości null jako AltName w bd, wtedy DataManager używając Selectmany miałby puste wartości, co spowodowałoby NullReferenceException przy próbach pobrania właściwości

            return new DbTeam
            {
                Id = teamResponse.Id,
                Name = teamResponse.Name,
                TeamAlternateNames = teamAlternateNames
            };
        }
        
        public static DbTeam CopyWithoutNavigationProperties(DbTeam dbTeam)
        {
            return new DbTeam
            {
                Id = dbTeam.Id,
                Name = dbTeam.Name
            };
        }

        public static DbTeamAlternateName CopyWithoutNavigationProperties(DbTeamAlternateName dbTeamAlternateName)
        {
            return new DbTeamAlternateName
            {
                AlternateName = dbTeamAlternateName.AlternateName,
                TeamId = dbTeamAlternateName.TeamId
            };
        }
    }
}
