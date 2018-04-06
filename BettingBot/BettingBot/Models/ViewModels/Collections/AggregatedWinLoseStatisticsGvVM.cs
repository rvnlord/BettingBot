using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common.UtilityClasses;
using MoreLinq;

namespace BettingBot.Models.ViewModels.Collections
{
    public class AggregatedWinLoseStatisticsGvVM : CustomList<AggregatedWinLoseStatisticGvVM>
    {
        public AggregatedWinLoseStatisticsGvVM(IReadOnlyCollection<RepCounter<int>> losesCounter, IReadOnlyCollection<RepCounter<int>> winsCounter, bool isReadOnly = false)
            : base(isReadOnly)
        {
            if (losesCounter.Count == 0 && winsCounter.Count == 0)
            {
                _customList = new List<AggregatedWinLoseStatisticGvVM>();
                return;
            }
            
            var maxLosesCounter = losesCounter.Any() ? losesCounter.MaxBy(c => c.Value).Value : 0;
            var maxWinsCounter = winsCounter.Any() ? winsCounter.MaxBy(c => c.Value).Value : 0;
            var maxCounter = Math.Max(maxLosesCounter, maxWinsCounter);
            for (var i = maxCounter - 1; i >= 0; i--)
            {
                _customList.Add(new AggregatedWinLoseStatisticGvVM(
                    i + 1,
                    losesCounter.Any(lc => lc.Value == i + 1) ? losesCounter.Single(lc => lc.Value == i + 1).Counter : 0,
                    winsCounter.Any(lc => lc.Value == i + 1) ? winsCounter.Single(lc => lc.Value == i + 1).Counter : 0));
            }
            _customList = _customList.OrderByDescending(s => s.Count).ToList();
        }
    }

}
