using BettingBot.Source.Converters;

namespace BettingBot.Source.DbContext.Models
{
    public class DbTeamAlternateName
    {
        public string AlternateName { get; set; }

        public int TeamId { get; set; }

        public virtual DbTeam Team { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is DbTeamAlternateName)) return false;
            var o = (DbTeamAlternateName)obj;

            return AlternateName == o.AlternateName;
        }

        public override int GetHashCode()
        {
            return AlternateName.GetHashCode() ^ 387;
        }

        public DbTeamAlternateName CopyWithoutNavigationProperties()
        {
            return TeamConverter.CopyWithoutNavigationProperties(this);
        }
    }
}
