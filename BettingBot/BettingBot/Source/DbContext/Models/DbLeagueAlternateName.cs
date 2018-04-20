using BettingBot.Source.Converters;

namespace BettingBot.Source.DbContext.Models
{
    public class DbLeagueAlternateName
    {
        public string AlternateName { get; set; }

        public int LeagueId { get; set; }

        public virtual DbLeague League { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is DbLeagueAlternateName)) return false;
            var o = (DbLeagueAlternateName)obj;

            return AlternateName == o.AlternateName;
        }

        public override int GetHashCode()
        {
            return AlternateName.GetHashCode() ^ 387;
        }

        public DbLeagueAlternateName CopyWithoutNavigationProperties()
        {
            return LeagueConverter.CopyWithoutNavigationProperties(this);
        }
    }
}
