using System;
using BettingBot.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source.DbContext.Models
{
    [Serializable]
    public class DbBet
    {
        public int Id { get; set; }
        public double Odds { get; set; }
        public int? BetResult { get; set; }

        public DateTime OriginalDate { get; set; }
        public int OriginalBetResult { get; set; }
        public string OriginalHomeName { get; set; }
        public string OriginalAwayName { get; set; }
        public string OriginalMatchResultString { get; set; }
        public string OriginalPickString { get; set; }
        public int? OriginalDiscipline { get; set; }
        public string OriginalLeagueName { get; set; }
        public double? OriginalStake { get; set; }

        public int? TriedAssociateWithMatch { get; set; }

        public int TipsterId { get; set; }
        public int? MatchId { get; set; }
        public int PickId { get; set; }

        public virtual DbTipster Tipster { get; set; }
        public virtual DbMatch Match { get; set; }
        public virtual DbPick Pick { get; set; }
        
        public override string ToString()
        {
            return $"{Id} - {OriginalDate}, {OriginalHomeName} {OriginalAwayName}, ({TipsterId}) {Tipster?.Name ?? "(Brak)"}";
        }

        public BetToDisplayGvVM ToBetToDisplayGvVM()
        {
            return BetConverter.ToBetToDisplayGvVM(this);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbBet)) return false;
            var b = (DbBet) obj;

            return OriginalDate == b.OriginalDate 
                && OriginalHomeName.EqIgnoreCase(b.OriginalHomeName)
                && OriginalAwayName.EqIgnoreCase(b.OriginalAwayName) 
                && TipsterId == b.TipsterId
                && PickId == b.PickId
                && OriginalDiscipline == b.OriginalDiscipline;
        }

        public bool EqualsWoOriginalDate(DbBet b)
        {
            return OriginalHomeName.EqIgnoreCase(b.OriginalHomeName)
                   && OriginalAwayName.EqIgnoreCase(b.OriginalAwayName)
                   && TipsterId == b.TipsterId
                   && PickId == b.PickId
                   && OriginalDiscipline == b.OriginalDiscipline;
        }

        public override int GetHashCode()
        {
            return OriginalDate.GetHashCode() ^ 7 
                * OriginalHomeName.GetHashCode() ^ 11
                * OriginalAwayName.GetHashCode() ^ 17
                * TipsterId.GetHashCode() ^ 19 
                * PickId.GetHashCode() ^ 23
                * OriginalDiscipline.GetHashCode() ^ 29;
        }

        public DbBet CopyWithoutNavigationProperties()
        {
            return BetConverter.CopyWithoutNavigationProperties(this);
        }

        public BetToAssociateGvVM ToBetToAssociateGvVM()
        {
            return BetConverter.ToBetToAssociateGvVM(this);
        }
    }
}
