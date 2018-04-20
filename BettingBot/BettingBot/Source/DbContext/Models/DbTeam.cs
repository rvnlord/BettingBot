using System.Collections.Generic;
using BettingBot.Source.Converters;

namespace BettingBot.Source.DbContext.Models
{
    public class DbTeam
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual IList<DbMatch> HomeMatches { get; set; } = new List<DbMatch>();
        public virtual IList<DbMatch> AwayMatches { get; set; } = new List<DbMatch>();
        public virtual IList<DbTeamAlternateName> TeamAlternateNames { get; set; } = new List<DbTeamAlternateName>();
        
        public override bool Equals(object obj)
        {
            if (!(obj is DbTeam)) return false;
            var o = (DbTeam)obj;

            return Name == o.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ 387;
        }

        public DbTeam CopyWithoutNavigationProperties()
        {
            return TeamConverter.CopyWithoutNavigationProperties(this);
        }
    }
}
