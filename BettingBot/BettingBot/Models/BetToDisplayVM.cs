using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models
{
    public class BetToDisplayVM
    {
        public int Id { get; set; }
        public int Nr { get; set; }
        public DateTime Date { get; set; }
        public string Match { get; set; }
        public Result BetResult { get; set; }
        public string MatchResult { get; set; }

        public int TipsterId { get; set; }
        public int PickId { get; set; }

        public virtual Tipster Tipster { get; set; }
        public virtual Pick Pick { get; set; }
        
        public double Odds { get; set; }
        public string PickOriginalString { get; set; }
        public double Stake { get; set; }
        public double Profit { get; set; }
        public double Budget { get; set; }
        public double BudgetBeforeResult { get; set; }
        
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
