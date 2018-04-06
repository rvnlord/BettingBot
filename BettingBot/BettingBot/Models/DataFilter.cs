using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using BettingBot.Common;
using BettingBot.Models.ViewModels;

namespace BettingBot.Models
{
    public abstract class DataFilter
    {
        public abstract FilterType FilterType { get; }
        public abstract List<Bet> Apply(List<Bet> bets);
    }

    public class LowestHighestSumOddsByPeriodFilter : DataFilter
    {
        public LowestHighestSumOddsByPeriodFilterChoice Choice { get; }
        public int Period { get; }
        public override FilterType FilterType => FilterType.LowestHighestSumOddsByPeriod;

        public LowestHighestSumOddsByPeriodFilter(LowestHighestSumOddsByPeriodFilterChoice choice, int period)
        {
            Choice = choice;
            Period = period;
        }

        public override List<Bet> Apply(List<Bet> bets)
        {
            var betsGroups = bets.GroupBy(b => b.Date.Period(Period));

            return Choice == LowestHighestSumOddsByPeriodFilterChoice.HighestByPeriod
                ? betsGroups.Select(g => g.MaxBy(b => b.Odds)).ToList()
                : Choice == LowestHighestSumOddsByPeriodFilterChoice.LowestByPeriod
                    ? betsGroups.Select(g => g.MinBy(b => b.Odds)).ToList()
                    : bets;
            //: betsGroups.Select(g => new Bet { Odds = g.Sum(b => b.Odds) }).ToList();
        }
    }

    public class OddsLesserGreaterThanFilter : DataFilter
    {
        public OddsLesserGreaterThanFilterChoice Choice { get; }
        public double Reference { get; }
        public override FilterType FilterType => FilterType.OddsLesserGreaterThan;

        public OddsLesserGreaterThanFilter(OddsLesserGreaterThanFilterChoice choice, double reference)
        {
            Choice = choice;
            Reference = reference;
        }

        public override List<Bet> Apply(List<Bet> bets)
        {
            return Choice == OddsLesserGreaterThanFilterChoice.GreaterThan
                ? bets.Where(b => b.Odds >= Reference).ToList()
                : bets.Where(b => b.Odds <= Reference).ToList();
        }
    }

    public class SelectionFilter : DataFilter
    {
        public SelectionFilterChoice Choice { get; }
        public List<BetToDisplayGvVM> SelectedBetsVM { get; }
        public List<BetToDisplayGvVM> VisibleBetsVM { get; }
        public override FilterType FilterType => FilterType.Selection;

        public SelectionFilter(SelectionFilterChoice choice, List<BetToDisplayGvVM> selectedBetsVM, List<BetToDisplayGvVM> visibleBetsVM)
        {
            Choice = choice;
            SelectedBetsVM = selectedBetsVM;
            VisibleBetsVM = visibleBetsVM;
        }

        public override List<Bet> Apply(List<Bet> bets)
        {
            var selectedBets = bets.Where(b => SelectedBetsVM.Any(sb => Equals(new { b.Date, b.Match, b.TipsterId }, new { sb.Date, sb.Match, sb.TipsterId }))).ToList();
            var visibleBets = bets.Where(b => VisibleBetsVM.Any(vb => Equals(new { b.Date, b.Match, b.TipsterId }, new { vb.Date, vb.Match, vb.TipsterId }))).ToList();
            return Choice == SelectionFilterChoice.Selected
                ? selectedBets
                : Choice == SelectionFilterChoice.Unselected
                    ? visibleBets.Except(selectedBets).ToList()
                    : visibleBets;
        }
    }

    public class TipsterFilter : DataFilter
    {
        public IEnumerable<Tipster> Tipsters { get; set; }
        public override FilterType FilterType => FilterType.Tipster;

        public TipsterFilter(IEnumerable<Tipster> tipsters)
        {
            Tipsters = tipsters;
        }
        
        public override List<Bet> Apply(List<Bet> bets)
        {
            return bets.WhereByMany(b => b.TipsterId, Tipsters.Select(t => t.Id)).ToList();
        }
    }

    public class DateFilter : DataFilter
    {
        public DateTime? ToDate { get; set; }
        public DateTime? FromDate { get; set; }

        public override FilterType FilterType => FilterType.Date;

        public DateFilter(DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate != null && toDate != null && fromDate > toDate)
                throw new Exception("Data rozpoczęcia jest większa niż data zakończenia");

            FromDate = fromDate;
            ToDate = toDate;
        }

        public override List<Bet> Apply(List<Bet> bets)
        {
            if (FromDate != null)
                bets = bets.Where(b => b.Date >= ((DateTime)FromDate).ToDMY()).ToList();
            if (ToDate != null)
                bets = bets.Where(b => b.Date <= ((DateTime)ToDate).ToDMY().AddDays(1).AddTicks(-1)).ToList();
            return bets;
        }
    }

    public class PickFilter : DataFilter
    {
        public IEnumerable<PickChoice> Picks { get; set; }

        public override FilterType FilterType => FilterType.Pick;

        public PickFilter(IEnumerable<PickChoice> picks)
        {
            Picks = picks;
        }

        public PickFilter(IEnumerable<int> picks)
        {
            Picks = picks.Cast<PickChoice>();
        }

        public override List<Bet> Apply(List<Bet> bets)
        {
            return bets.WhereByMany(b => b.Pick.Choice, Picks).ToList();
        }
    }

    public class NotesFilter : DataFilter
    {
        public string Notes { get; set; }

        public override FilterType FilterType => FilterType.WithoutNotes;

        public NotesFilter(string notes)
        {
            Notes = notes;
        }

        public override List<Bet> Apply(List<Bet> bets)
        {
            if (Notes == null) return bets;
            var matches = Notes.Split("\n");
            return bets.Except(bets.WhereByMany(b => $"{b.Match} - {b.Pick}" , matches)).ToList();
        }
    }

    public enum LowestHighestSumOddsByPeriodFilterChoice
    {
        LowestByPeriod,
        HighestByPeriod,
        SumByPeriod
    }

    public enum OddsLesserGreaterThanFilterChoice
    {
        GreaterThan = -1,
        LesserThan
    }

    public enum SelectionFilterChoice
    {
        Selected = -1,
        Unselected,
        Visible
    }

    public enum FilterType
    {
        OddsLesserGreaterThan,
        LowestHighestSumOddsByPeriod,
        Selection,
        Tipster,
        Date,
        Pick,
        WithoutNotes
    }
}
