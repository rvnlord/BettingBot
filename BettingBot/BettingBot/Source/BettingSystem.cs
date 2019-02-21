using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using BettingBot.Source.WIndows;

namespace BettingBot.Source
{
    public class BettingSystem
    {
        public double InitialStake { get; set; }
        public double Budget { get; set; }
        public StakingTypeOnLose StakingTypeOnLose { get; set; }
        public StakingTypeOnWin StakingTypeOnWin { get; set; }
        public List<DataFilter> Filters { get; set; }
        public List<BetToDisplayGvVM> InitialBets { get; }
        public List<BetToDisplayGvVM> FilteredBets { get; private set; }
        public List<BetToDisplayGvVM> Bets { get; set; }
        public double BudgetIncreaseReference { get; set; }
        public double StakeIncrease { get; set; }
        public double BudgetDecreaseReference { get; set; }
        public double StakeDecrease { get; set; }
        public double LoseCoefficient { get; set; }
        public double WinCoefficient { get; set; }
        public LoseCondition LoseCondition { get; set; }
        public double MaxStake { get; set; }
        public List<RepCounter<int>> LosesCounter { get; private set; } = new List<RepCounter<int>>();
        public List<RepCounter<int>> WinsCounter { get; private set; } = new List<RepCounter<int>>();
        public bool ResetStake { get; set; }

        public bool IncreaseBudgetWithStake => (int)BudgetIncreaseReference != 0 && (int)StakeIncrease != 0;
        public bool DecreaseBudgetWithStake => (int)BudgetDecreaseReference != 0 && (int)StakeDecrease != 0;

        public BettingSystem(double initialStake, double budget, StakingTypeOnLose stakingTypeOnLose, StakingTypeOnWin stakingTypeOnWin,
            List<BetToDisplayGvVM> bets, List<DataFilter> filters, double budgetIncreaseReference, double stakeIncrease, 
            double budgetDecreaseReference, double stakeDecrease, double loseCoefficient, double winCoefficient, 
            LoseCondition loseCondition, double maxStake, bool resetStake)
        {
            InitialStake = initialStake;
            Budget = budget;
            StakingTypeOnLose = stakingTypeOnLose;
            StakingTypeOnWin = stakingTypeOnWin;
            InitialBets = bets;
            FilteredBets = new List<BetToDisplayGvVM>();
            Filters = filters;
            BudgetIncreaseReference = budgetIncreaseReference;
            StakeIncrease = stakeIncrease;
            BudgetDecreaseReference = budgetDecreaseReference;
            StakeDecrease = stakeDecrease;
            LoseCoefficient = loseCoefficient;
            WinCoefficient = winCoefficient;
            LoseCondition = loseCondition;
            MaxStake = maxStake;
            ResetStake = resetStake;
        }

        private double CalculateStakeChangedWithBudget(double currentBudget, double previousBudget, double maxBudget)
        {
            double multiplierForMax = 0;
            double multiplierForCurr = 0;
            maxBudget = Math.Max(currentBudget, maxBudget);

            var maxInitBudgetDiff = maxBudget - Budget;
            if (maxInitBudgetDiff >= BudgetIncreaseReference && IncreaseBudgetWithStake)
                multiplierForMax = (int) Math.Floor(maxInitBudgetDiff / BudgetIncreaseReference);
            var currInitBudgetDiff = currentBudget - Budget;
            if (currInitBudgetDiff >= BudgetIncreaseReference && DecreaseBudgetWithStake)
                multiplierForCurr = (int) Math.Floor(currInitBudgetDiff / BudgetIncreaseReference);

            var multiplierForDecrease = multiplierForMax - multiplierForCurr;
            
            return Math.Max(InitialStake + StakeIncrease * multiplierForMax - StakeDecrease * multiplierForDecrease, InitialStake);
        }

        public void ApplyFilters()
        {
            var rawFilteredBets = Filters
                .Aggregate(InitialBets, (bets, filter) => filter.Apply(bets));
            MaintainOrder(rawFilteredBets);
            FilteredBets = rawFilteredBets;
        }

        private void MaintainOrder(List<BetToDisplayGvVM> bets)
        {
            var concludedBets = bets.Where(b => b.BetResult != BetResult.Pending).OrderBy(b => b.LocalTimestamp)
                .ThenBy(b => b.MatchHomeName).ToList();
            var pendingBets = bets.Where(b => b.BetResult == BetResult.Pending).OrderBy(b => b.LocalTimestamp)
                .ThenBy(b => b.MatchHomeName).ToList();
            bets.ReplaceAll(concludedBets.Concat(pendingBets));
        }

        private bool IsLost(double budget, double previousBudget, double maxBudget)
        {
            if (LoseCondition == LoseCondition.BudgetLowerThanMax)
                return budget < maxBudget;
            if (LoseCondition == LoseCondition.PreviousPeriodLost)
                return budget < previousBudget;
            throw new Exception("Niepoprawny warunek porażki");
        }

        private double GetBudgetForRef(double previousBudget, double maxBudget)
        {
            if (LoseCondition == LoseCondition.BudgetLowerThanMax)
                return maxBudget;
            if (LoseCondition == LoseCondition.PreviousPeriodLost)
                return previousBudget;
            throw new Exception("Niepoprawny warunek porażki");
        }
        
        public void ApplyStaking()
        {
            if (CheckAggregation())
                ApplyStakingForAggregation();
            else
                ApplyStakingForIndividual();
            MaintainOrder(Bets);
        }

        private void ApplyStakingForIndividual()
        {
            var betsVM = new List<BetToDisplayGvVM>();
            var currNr = 1;
            var currentBudget = Budget;
            var previousBudget = Budget;
            var maxBudget = Budget;
            var areLost = new List<bool>();
            BetToDisplayGvVM previousBet = null;

            foreach (var bet in FilteredBets)
            {
                var betVM = bet.Copy();
                var currStake = CalculateStakeChangedWithBudget(currentBudget, previousBudget, maxBudget);

                if (previousBet != null)
                {
                    var isPrevLost = IsLost(currentBudget, previousBudget, maxBudget);
                    areLost.Add(isPrevLost);
                    var maxOrPrevBudget = GetBudgetForRef(previousBudget, maxBudget);
                    var lostInARow = areLost.AsEnumerable().Reverse().TakeWhile(b => b).Count();
                    var wonInARow = areLost.AsEnumerable().Reverse().TakeWhile(b => !b).Count();
                    var lostMoney = maxOrPrevBudget - currentBudget;
                    
                    currStake = CalculateStake(currStake, isPrevLost, betsVM.LastOrNull(), lostInARow, wonInARow, lostMoney, betVM.Odds, 1, currentBudget);
                }

                previousBudget = currentBudget; // przed uzyskaniem profitu lub straty
                maxBudget = Math.Max(currentBudget, maxBudget);
                previousBet = betVM;
                betVM.Stake = currStake;
                betVM.Nr = currNr++;
                betVM.CalculateProfit(currStake, ref currentBudget);
                betsVM.Add(betVM);
            }

            LosesCounter = GetLosesCounter(areLost);
            WinsCounter = GetWinsCounter(areLost);
            Bets = betsVM;
        }
        
        private void ApplyStakingForAggregation()
        {
            var betsVM = new List<BetToDisplayGvVM>();
            var currNr = 1;
            var currentBudget = Budget;
            var previousBudget = Budget;
            var maxBudget = Budget;
            var areLost = new List<bool>();
            IGrouping<DateTime, BetToDisplayGvVM> previousGroup = null;

            var period = ((LowestHighestSumOddsByPeriodFilter) Filters.First(f => f.FilterType == FilterType.LowestHighestSumOddsByPeriod)).Period;
            var betsGroups = FilteredBets.GroupBy(b => b.LocalTimestamp.Rfc1123.Period(period)).ToList();

            foreach (var g in betsGroups)
            {
                var groupOdds = g.Select(b => b.Odds).Sum();
                var groupSize = g.Count();

                var currStake = CalculateStakeChangedWithBudget(currentBudget, previousBudget, maxBudget);

                if (previousGroup != null)
                {
                    var isPrevLost = IsLost(currentBudget, previousBudget, maxBudget);
                    areLost.Add(isPrevLost);
                    var maxOrPrevBudget = GetBudgetForRef(previousBudget, maxBudget);
                    var lostInARow = areLost.AsEnumerable().Reverse().TakeWhile(b => b).Count();
                    var wonInARow = areLost.AsEnumerable().Reverse().TakeWhile(b => !b).Count();
                    var lostMoney = maxOrPrevBudget - currentBudget;
                    
                    currStake = CalculateStake(currStake, isPrevLost, betsVM.LastOrNull(), lostInARow, wonInARow, lostMoney, groupOdds, groupSize, currentBudget); // betsVM.Last() - ostatni dodany należy do poprzedniej grupy, w której wszydtkie stawki są takie same
                }

                previousBudget = currentBudget; // przed uzyskaniem profitu lub straty dla dowolnego elementu grupy
                maxBudget = Math.Max(currentBudget, maxBudget);

                foreach (var bet in g)
                {
                    var betVM = bet.Copy();
                    
                    betVM.Stake = currStake;
                    betVM.Nr = currNr++;
                    betVM.CalculateProfit(currStake, ref currentBudget);
                    betsVM.Add(betVM);
                }

                previousGroup = g;
            }

            LosesCounter = GetLosesCounter(areLost);
            WinsCounter = GetWinsCounter(areLost);
            Bets = betsVM;
        }

        private double CalculateStake(double currStake, bool isLost, BetToDisplayGvVM previousBet, int lostInARow, int wonInARow, double lostMoney, double totalOdds, int groupSize, double budget)
        {
            if (previousBet == null)
                return currStake;

            if (StakingTypeOnLose == StakingTypeOnLose.Flat && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new FlatFlatStaking(isLost, currStake, previousBet.Stake, ResetStake).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Add && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new AddFlatStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, lostInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Subtract && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new SubtractFlatStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, lostInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Multiply && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new MultiplyFlatStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, lostInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Divide && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new DivideFlatStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, lostInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.CoverPercentOfLoses && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new CoverPercentOfLosesFlatStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, lostMoney, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.UsePercentOfBudget && StakingTypeOnWin == StakingTypeOnWin.Flat)
                currStake = new PercentOfBudgetFlatStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, budget, totalOdds, groupSize).Apply();

            else if (StakingTypeOnLose == StakingTypeOnLose.Flat && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new FlatAddStaking(isLost, currStake, previousBet.Stake, ResetStake, WinCoefficient, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Add && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new AddAddStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Subtract && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new SubtractAddStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Multiply && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new MultiplyAddStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Divide && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new DivideAddStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.CoverPercentOfLoses && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new CoverPercentOfLosesAddStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, lostMoney, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.UsePercentOfBudget && StakingTypeOnWin == StakingTypeOnWin.Add)
                currStake = new PercentOfBudgetAddStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();

            else if (StakingTypeOnLose == StakingTypeOnLose.Flat && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new FlatSubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, WinCoefficient, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Add && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new AddSubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Subtract && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new SubtractSubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Multiply && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new MultiplySubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Divide && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new DivideSubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.CoverPercentOfLoses && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new CoverPercentOfLosesSubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, lostMoney, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.UsePercentOfBudget && StakingTypeOnWin == StakingTypeOnWin.Subtract)
                currStake = new PercentOfBudgetSubtractStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();
            
            else if (StakingTypeOnLose == StakingTypeOnLose.Flat && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new FlatMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, WinCoefficient, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Add && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new AddMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Subtract && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new SubtractMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Multiply && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new MultiplyMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Divide && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new DivideMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.CoverPercentOfLoses && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new CoverPercentOfLosesMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, lostMoney, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.UsePercentOfBudget && StakingTypeOnWin == StakingTypeOnWin.Multiply)
                currStake = new PercentOfBudgetMultiplyStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();
            
            else if (StakingTypeOnLose == StakingTypeOnLose.Flat && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new FlatDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, WinCoefficient, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Add && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new AddDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Subtract && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new SubtractDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Multiply && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new MultiplyDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Divide && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new DivideDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.CoverPercentOfLoses && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new CoverPercentOfLosesDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, lostMoney, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.UsePercentOfBudget && StakingTypeOnWin == StakingTypeOnWin.Divide)
                currStake = new PercentOfBudgetDivideStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();

            else if (StakingTypeOnLose == StakingTypeOnLose.Flat && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new FlatPercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, WinCoefficient, budget, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Add && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new AddPercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Subtract && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new SubtractPercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Multiply && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new MultiplyPercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.Divide && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new DividePercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, lostInARow, wonInARow, budget, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.CoverPercentOfLoses && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new CoverPercentOfLosesPercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, budget, totalOdds, groupSize).Apply();
            else if (StakingTypeOnLose == StakingTypeOnLose.UsePercentOfBudget && StakingTypeOnWin == StakingTypeOnWin.UsePercentOfBudget)
                currStake = new PercentOfBudgetPercentOfBudgetStaking(isLost, currStake, previousBet.Stake, ResetStake, LoseCoefficient, WinCoefficient, budget, totalOdds, groupSize).Apply();
            
            else
                throw new Exception("Niezaimplementowane");

            return MaxStake.Eq(0.0) ? currStake : Math.Min(currStake, MaxStake);
        }

        private bool CheckAggregation()
        {
            LowestHighestSumOddsByPeriodFilter filterLHSByP = null;
            foreach (var f in Filters)
            {
                filterLHSByP = f as LowestHighestSumOddsByPeriodFilter;
                if (filterLHSByP != null)
                    break;
            }

            return filterLHSByP != null && filterLHSByP.Choice == LowestHighestSumOddsByPeriodFilterChoice.SumByPeriod;
        }

        private static List<RepCounter<int>> GetLosesCounter(IEnumerable<bool> areLost)
        {
            return GetWinsOrLosesCounter(areLost, false);
        }

        private static List<RepCounter<int>> GetWinsCounter(IEnumerable<bool> areLost)
        {
            return GetWinsOrLosesCounter(areLost, true);
        }

        private static List<RepCounter<int>> GetWinsOrLosesCounter(IEnumerable<bool> areLost, bool wins)
        {
            var losesInRow = new List<RepCounter<int>>();
            var c = 0;
            foreach (var l in areLost)
            {
                if ((wins && !l) || (!wins && l))
                    c++;
                else
                {
                    if (losesInRow.Any(i => i.Value == c))
                        losesInRow.Single(i => i.Value == c).Counter++;
                    else
                        losesInRow.Add(new RepCounter<int>(c, 1));
                    c = 0;
                }
            }
            return losesInRow.OrderByDescending(l => l.Value).ThenByDescending(l => l.Counter).ToList();
        }
    }
}
