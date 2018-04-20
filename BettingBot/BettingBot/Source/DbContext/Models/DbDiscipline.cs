using System.Collections.Generic;
using BettingBot.Source.Converters;

namespace BettingBot.Source.DbContext.Models
{
    public class DbDiscipline
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual IList<DbLeague> Leagues { get; set; } = new List<DbLeague>();

        public override bool Equals(object obj)
        {
            if (!(obj is DbDiscipline)) return false;
            var d = (DbDiscipline)obj;

            return Name == d.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ 387;
        }

        public DbDiscipline CopyWithoutNavigationProperties()
        {
            return DisciplineConverter.CopyWithoutNavigationProperties(this);
        }
    }
}
