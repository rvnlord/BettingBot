using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using MoreLinq;

namespace BettingBot.Source
{
    public abstract class DataFilter
    {
        public abstract FilterType FilterType { get; }
        public abstract List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets);
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

        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
        {
            var betsGroups = bets.GroupBy(b => b.LocalTimestamp.Rfc1123.Period(Period));

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

        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
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

        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
        {
            return Choice == SelectionFilterChoice.Selected
                ? SelectedBetsVM
                : Choice == SelectionFilterChoice.Unselected
                    ? VisibleBetsVM.Except(SelectedBetsVM).ToList()
                    : VisibleBetsVM;
        }
    }

    public class TipsterFilter : DataFilter
    {
        public IEnumerable<TipsterGvVM> Tipsters { get; set; }
        public override FilterType FilterType => FilterType.Tipster;

        public TipsterFilter(IEnumerable<TipsterGvVM> tipsters)
        {
            Tipsters = tipsters;
        }
        
        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
        {
            return bets.WhereByMany(b => b.TipsterName + b.TipsterWebsite?.ToLower(), 
                Tipsters.Select(t => t.Name + t.WebsiteAddress?.ToLower() )).ToList();
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

        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
        {
            if (FromDate != null)
                bets = bets.Where(b => b.LocalTimestamp.Rfc1123 >= ((DateTime)FromDate).ToDMY()).ToList();
            if (ToDate != null)
                bets = bets.Where(b => b.LocalTimestamp.Rfc1123 <= ((DateTime)ToDate).ToDMY().AddDays(1).AddTicks(-1)).ToList();
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

        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
        {
            return bets.WhereByMany(b => b.PickChoice, Picks).ToList();
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

        public override List<BetToDisplayGvVM> Apply(List<BetToDisplayGvVM> bets)
        {
            if (Notes == null) return bets;
            var matches = Notes.Split("\n");
            
            return bets.Except(bets.WhereByMany(b => $"{b.MatchHomeName} - {b.MatchAwayName}: {b.PickString}" , matches)).ToList();
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
