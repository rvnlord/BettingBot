using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Models;

namespace BettingBot.Models
{
    public class ProfitByPeriodStatistic
    {
        public string Period { get; set; }
        public double Profit { get; set; }
        public int Count { get; set; }
        public int PeriodId { get; set; }

        public string ProfitStr
        {
            get
            {
                var profitStr = $"{Profit:0.00} zł";
                if (profitStr.Contains("-"))
                    profitStr = profitStr.Insert(profitStr.IndexOf("-", StringComparison.Ordinal) + 1, " ");
                return profitStr;
            }
        }

        public ProfitByPeriodStatistic(int periodId, string period, double profit, int count)
        {
            PeriodId = periodId;
            Period = period;
            Profit = profit;
            Count = count;
        }
        public override string ToString()
        {
            return $"{Period}, {ProfitStr}";
        }
    }

    public class ProfitByPeriodStatistics : CustomList<ProfitByPeriodStatistic>
    {
        public ProfitByPeriodStatistics(IEnumerable<BetToDisplayVM> bets, Period period, bool isReadOnly = false) : base(isReadOnly)
        {
            Func<BetToDisplayVM, string> groupBySt;
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

            _customList = bets.GroupBy(groupBySt)
                .Select((g, i) => new ProfitByPeriodStatistic(
                    i,
                    g.Key,
                    g.Last().Budget - g.First().Budget + g.First().Profit,
                    g.Count()))
                .ToList();
            _customList.Add(new ProfitByPeriodStatistic(
                _customList.Select(pbp => pbp.PeriodId).Max() + 1, 
                "Razem:", 
                _customList.Select(pbp => pbp.Profit).Sum(),
                _customList.Select(pbp => pbp.Count).Sum()));
        }

        public ProfitByPeriodStatistics(IEnumerable<ProfitByPeriodStatistic> statistics, bool isReadOnly = false) : base(isReadOnly)
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
