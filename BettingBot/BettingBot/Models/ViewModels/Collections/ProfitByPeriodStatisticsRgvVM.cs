using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Models.ViewModels.Collections
{
    public class ProfitByPeriodStatisticsRgvVM : CustomList<ProfitByPeriodStatisticRgvVM>
    {
        public ProfitByPeriodStatisticsRgvVM(IEnumerable<BetToDisplayRgvVM> bets, Period period, bool isReadOnly = false) : base(isReadOnly)
        {
            var listBets = bets.ToList();
            if (!listBets.Any())
            {
                _customList = new List<ProfitByPeriodStatisticRgvVM>();
                return;
            }
               

            Func<BetToDisplayRgvVM, string> groupBySt;
            if (period == Period.Month)
                groupBySt = b => $"{b.Date.MonthName()} {b.Date.Year}";
            else if (period == Period.Week)
                groupBySt = b =>
                {
                    const int i = 7;
                    var p = b.Date.Period(i);
                    return $"{Math.Floor(p.DayOfYear / (double) i) + 1} tydzień {p.Year}";
                };
            else if (period == Period.Day)
                groupBySt = b => $"{b.Date:dd-MM-yyyy}";
            else throw new Exception("Niepoprawne grupowanie");

            _customList = listBets.GroupBy(groupBySt)
                .Select((g, i) => new ProfitByPeriodStatisticRgvVM(
                    i,
                    g.Key,
                    g.Last().Budget - g.First().Budget + g.First().Profit,
                    g.Count()))
                .ToList();
            _customList.Add(new ProfitByPeriodStatisticRgvVM(
                _customList.Select(pbp => pbp.PeriodId).Max() + 1, 
                "Razem:", 
                _customList.Select(pbp => pbp.Profit).Sum(),
                _customList.Select(pbp => pbp.Count).Sum()));
        }

        public ProfitByPeriodStatisticsRgvVM(IEnumerable<ProfitByPeriodStatisticRgvVM> statistics, bool isReadOnly = false) : base(isReadOnly)
        {
            _customList = statistics.ToList();
        }
    }

    public enum Period
    {
        Month = -1,
        Week,
        Day
    }
}
