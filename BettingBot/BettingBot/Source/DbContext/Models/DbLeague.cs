using System.Collections.Generic;

namespace BettingBot.Source.DbContext.Models
{
    public class DbLeague
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Season { get; set; }

        public int DisciplineId { get; set; }

        public virtual DbDiscipline Discipline { get; set; }
        public virtual IList<DbMatch> Matches { get; set; } = new List<DbMatch>();

        public override bool Equals(object obj)
        {
            if (!(obj is DbLeague)) return false;
            var l = (DbLeague)obj;

            return Name == l.Name
                && Season == l.Season
                && DisciplineId == l.DisciplineId;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ 19 
                * Season.GetHashCode() ^ 23
                * DisciplineId ^ 29;
        }
    }
}
