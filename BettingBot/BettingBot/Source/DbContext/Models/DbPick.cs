using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using BettingBot.Source.Common;
using BettingBot.Source.Converters;
using MoreLinq;

namespace BettingBot.Source.DbContext.Models
{
    [Table("tblPicks")]
    [Serializable]
    public class DbPick
    {
        public int Id { get; set; }
        public PickChoice Choice { get; set; }
        public double? Value { get; set; }

        public virtual IList<DbBet> Bets { get; set; } = new List<DbBet>();

        public DbPick()
        {
        }

        public DbPick(int id, PickChoice choice, double? value)
        {
            Id = id;
            Choice = choice;
            Value = value;
        }

        public override string ToString()
        {
            return PickConverter.PickToString(Choice, Value);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbPick))
                return false;

            var otherPick = (DbPick)obj;
            return Choice == otherPick.Choice 
                && Value.Eq(otherPick.Value);
        }

        public override int GetHashCode()
        {
            return Choice.GetHashCode() * 397 ^ Value.GetHashCode() * 397;
        }
    }
}
