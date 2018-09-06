using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Source.ViewModels.Collections
{
    public class ProfitByPeriodStatisticsGvVM : CustomList<ProfitByPeriodStatisticGvVM>
    {
        public ProfitByPeriodStatisticsGvVM(IEnumerable<BetToDisplayGvVM> bets, Period period, bool isReadOnly = false) : base(isReadOnly)
        {
            var listBets = bets.ToList();
            if (!listBets.Any())
            {
                _customList = new List<ProfitByPeriodStatisticGvVM>();
                return;
            }

            var pbpLs = LocalizationManager.GetProfitByPeriodStatisticsLocalizedStrings();
            var months = pbpLs.Take(12).ToArray();
            var week = pbpLs.Skip(12).Take(1).Single();
            var total = pbpLs.Skip(13).Take(1).Single();

            Func<BetToDisplayGvVM, string> groupBySt;
            if (period == Period.Month)
                groupBySt = b => $"{months[b.LocalTimestamp.Rfc1123.Month - 1]} {b.LocalTimestamp.Rfc1123.Year}";
            else if (period == Period.Week)
                groupBySt = b =>
                {
                    const int i = 7;
                    var p = b.LocalTimestamp.Rfc1123.Period(i);
                    return $"{Math.Floor(p.DayOfYear / (double) i) + 1} {week} {p.Year}";
                };
            else if (period == Period.Day)
                groupBySt = b => $"{b.LocalTimestamp.Rfc1123:dd-MM-yyyy}";
            else throw new Exception("Niepoprawne grupowanie");

            _customList = listBets.GroupBy(groupBySt)
                .Select((g, i) => new ProfitByPeriodStatisticGvVM(
                    i,
                    g.Key,
                    g.Last().Budget - g.First().Budget + g.First().Profit,
                    g.Count()))
                .ToList();
            _customList.Add(new ProfitByPeriodStatisticGvVM(
                _customList.Select(pbp => pbp.PeriodId).Max() + 1,
                total, 
                _customList.Select(pbp => pbp.Profit).Sum(),
                _customList.Select(pbp => pbp.Count).Sum()));
        }

        public ProfitByPeriodStatisticsGvVM(IEnumerable<ProfitByPeriodStatisticGvVM> statistics, bool isReadOnly = false) : base(isReadOnly)
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
