using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using BettingBot.Common.UtilityClasses;
using BettingBot.Models;

namespace BettingBot.Models
{
    public class AggregatedWinLoseStatistic
    {
        public int Count { get; set; }
        public int Loses { get; set; }
        public int Wins { get; set; }

        public AggregatedWinLoseStatistic(int count, int loses, int wins)
        {
            Count = count;
            Loses = loses;
            Wins = wins;
        }
    }

    public class AggregatedWinLoseStatistics : CustomList<AggregatedWinLoseStatistic>
    {
        public AggregatedWinLoseStatistics(IReadOnlyCollection<RepCounter<int>> losesCounter, IReadOnlyCollection<RepCounter<int>> winsCounter, bool isReadOnly = false)
            : base(isReadOnly)
        {
            var maxLosesCounter = losesCounter.Any() ? losesCounter.MaxBy(c => c.Value).Value : 0;
            var maxWinsCounter = winsCounter.Any() ? winsCounter.MaxBy(c => c.Value).Value : 0;
            var maxCounter = Math.Max(maxLosesCounter, maxWinsCounter);
            for (var i = maxCounter - 1; i >= 0; i--)
            {
                _customList.Add(new AggregatedWinLoseStatistic(
                    i + 1,
                    losesCounter.Any(lc => lc.Value == i + 1) ? losesCounter.Single(lc => lc.Value == i + 1).Counter : 0,
                    winsCounter.Any(lc => lc.Value == i + 1) ? winsCounter.Single(lc => lc.Value == i + 1).Counter : 0));
            }
            _customList = _customList.OrderByDescending(s => s.Count).ToList();
        }
    }

}
