using System;
using BettingBot.Models.ViewModels.Abstracts;

namespace BettingBot.Models.ViewModels
{
    public class ProfitByPeriodStatisticRgvVM : BaseVM
    {
        private string _period;
        private double _profit;
        private int _count;
        private int _periodId;

        public string Period { get => _period; set => SetPropertyAndNotify(ref _period, value, nameof(Period)); }
        public double Profit { get => _profit; set => SetPropertyAndNotify(ref _profit, value, nameof(Profit)); }
        public int Count { get => _count; set => SetPropertyAndNotify(ref _count, value, nameof(Count)); }
        public int PeriodId { get => _periodId; set => SetPropertyAndNotify(ref _periodId, value, nameof(PeriodId)); }

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

        public ProfitByPeriodStatisticRgvVM(int periodId, string period, double profit, int count)
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
}
