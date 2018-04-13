using System;
using System.Collections.Generic;

namespace BettingBot.Source.DbContext.Models
{
    public class DbMatch
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int Status { get; set; }
        public int MatchDay { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public int HomeScoreHalf { get; set; }
        public int AwayScoreHalf { get; set; }

        public int HomeId { get; set; }
        public int AwayId { get; set; }
        public int LeagueId { get; set; }

        public virtual DbTeam Home { get; set; }
        public virtual DbTeam Away { get; set; }
        public virtual DbLeague League { get; set; }
        public virtual IList<DbBet> Bets { get; set; } = new List<DbBet>();

        public override bool Equals(object obj)
        {
            if (!(obj is DbMatch)) return false;
            var m = (DbMatch)obj;

            return Date == m.Date 
                && HomeId == m.HomeId 
                && AwayId == m.AwayId
                && LeagueId == m.LeagueId;
        }

        public override int GetHashCode()
        {
            return Date.GetHashCode() ^ 7
                * HomeId.GetHashCode() ^ 11
                * AwayId.GetHashCode() ^ 17
                * LeagueId.GetHashCode() ^ 23;
        }
    }
}
