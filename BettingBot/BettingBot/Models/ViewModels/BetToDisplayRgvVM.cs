using System;
using System.Text;
using BettingBot.Models.ViewModels.Abstracts;

namespace BettingBot.Models.ViewModels
{
    public class BetToDisplayRgvVM : BaseVM
    {
        private int _id;
        private int _nr;
        private DateTime _date;
        private string _match;
        private Result _betResult;
        private string _matchResult;
        private double _odds;
        private string _pickOriginalString;
        private double _stake;
        private double _profit;
        private double _budget;
        private double _budgetBeforeResult;
        private int _tipsterId;
        private int _pickId;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public int Nr { get => _nr; set => SetPropertyAndNotify(ref _nr, value, nameof(Nr)); }
        public DateTime Date { get => _date; set => SetPropertyAndNotify(ref _date, value, nameof(Date)); }
        public string Match { get => _match; set => SetPropertyAndNotify(ref _match, value, nameof(Match)); }
        public Result BetResult { get => _betResult; set => SetPropertyAndNotify(ref _betResult, value, nameof(BetResult)); }
        public string MatchResult { get => _matchResult; set => SetPropertyAndNotify(ref _matchResult, value, nameof(MatchResult)); }
        public double Odds { get => _odds; set => SetPropertyAndNotify(ref _odds, value, nameof(Odds)); }
        public string PickOriginalString { get => _pickOriginalString; set => SetPropertyAndNotify(ref _pickOriginalString, value, nameof(PickOriginalString)); }
        public double Stake { get => _stake; set => SetPropertyAndNotify(ref _stake, value, nameof(Stake)); }
        public double Profit { get => _profit; set => SetPropertyAndNotify(ref _profit, value, nameof(Profit)); }
        public double Budget { get => _budget; set => SetPropertyAndNotify(ref _budget, value, nameof(Budget)); }
        public double BudgetBeforeResult { get => _budgetBeforeResult; set => SetPropertyAndNotify(ref _budgetBeforeResult, value, nameof(BudgetBeforeResult)); }
        public int TipsterId { get => _tipsterId; set => SetPropertyAndNotify(ref _tipsterId, value, nameof(TipsterId)); }
        public int PickId { get => _pickId; set => SetPropertyAndNotify(ref _pickId, value, nameof(PickId)); }

        public virtual Tipster Tipster { get; set; }
        public virtual Pick Pick { get; set; }

        public string OddsString => $"{Odds:0.00}";
        public string StakeString => (Stake < 0 ? "- " + $"{Stake:0.##}".Substring(1) : $"{Stake:0.##}") + " zł";
        public string ProfitString => (Profit < 0 ? "- " + $"{Profit:0.##}".Substring(1) : "+ " + $"{Profit:0.##}") + " zł";
        public string BudgetString => (Budget < 0 ? "- " + $"{Budget:0.##}".Substring(1) : $"{Budget:0.##}") + " zł";
        public string DateString => Date.ToString("dd-MM-yyyy HH:mm");
        public string BetResultString
        {
            get
            {
                switch (BetResult)
                {
                    case Result.Win:
                        return "Wygrana";
                    case Result.Lose:
                        return "Przegrana";
                    case Result.Canceled:
                        return "Anulowano";
                    case Result.HalfLost:
                        return "- 1/2";
                    case Result.HalfWon:
                        return "+ 1/2";
                    case Result.Pending:
                        return "Oczekuje";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public string PickString
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(Pick);
                if (Pick.Choice == PickChoice.Other)
                    sb.Append($" {PickOriginalString}");
                return sb.ToString();
            }
        }

        public void CalculateProfit(double currStake, ref double budget)
        {
            BudgetBeforeResult = budget - currStake;

            if (BetResult == Result.Lose)
                Profit = -currStake;
            else if (BetResult == Result.Canceled || BetResult == Result.Pending)
                Profit = 0;
            else if (BetResult == Result.HalfLost)
                Profit = -currStake / 2;
            else if (BetResult == Result.HalfWon)
                Profit = (currStake * Odds - currStake) / 2;
            else
                Profit = currStake * Odds - currStake;

            budget += Profit;
            Budget = budget;
        }

        public override string ToString()
        {
            return $"{Id} - {Date}, {Match}, {Tipster.Name}";
        }
    }

    public enum Result
    {
        Lose = 0,
        Win = 1,
        Canceled = 2,
        HalfLost = 3,
        HalfWon = 4,
        Pending = 5
    }
}
