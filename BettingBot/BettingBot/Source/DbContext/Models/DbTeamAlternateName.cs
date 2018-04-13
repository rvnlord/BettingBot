namespace BettingBot.Source.DbContext.Models
{
    public class DbTeamAlternateName
    {
        public string AternateName { get; set; }

        public int TeamId { get; set; }

        public virtual DbTeam Team { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is DbTeamAlternateName)) return false;
            var o = (DbTeamAlternateName)obj;

            return AternateName == o.AternateName;
        }

        public override int GetHashCode()
        {
            return AternateName.GetHashCode() ^ 387;
        }
    }
}
