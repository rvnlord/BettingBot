using System;
using BettingBot.Common;

namespace BettingBot.Source
{
    public abstract class StakingManager
    {
        public double BaseStake { get; protected set; }
        public double PreviousStake { get; protected set; }
        public double CurrentStake => Apply();
        public bool IsLost { get; protected set; }
        public bool ResetStake { get; protected set; }
        public double FlatStake => ResetStake ? BaseStake : Math.Max(PreviousStake, BaseStake);
        protected double Value { get; set; }
        protected double InARow { get; set; }
        protected double GroupSize { get; set; }
        protected double LostMoney { get; set; }
        protected double TotalOdds { get; set; }
        protected double Budget { get; set; }

        protected StakingManager(bool isLost, double baseStake, double previousStake, bool resetStake) // stałe stawki
        {
            IsLost = isLost;
            BaseStake = baseStake;
            PreviousStake = previousStake;
            ResetStake = resetStake;
        }
        
        public virtual double Apply() => FlatStake;

        protected double Add() => ResetStake 
            ? Math.Max(BaseStake + Value * InARow / GroupSize, BaseStake)
            : Math.Max(PreviousStake + Value / GroupSize, BaseStake); // BaseStake + Value * InARow

        protected double Subtract() => Math.Max(PreviousStake - Value / GroupSize, BaseStake);

        protected double Multiply() => ResetStake 
            ? Math.Max(BaseStake * Math.Pow(Value, InARow) / GroupSize, BaseStake)
            : Math.Max(PreviousStake * Value / GroupSize, BaseStake); // BaseStake * Math.Pow(Value, InARow)

        protected double Divide() => Math.Max(PreviousStake / Value / GroupSize, BaseStake);

        protected double CoverPercentage()
        {
            if (TotalOdds.Eq(1)) // jeśli z jakiegoś powodu mecz ma kurs 1.000 to żadna stawka nie pokryje strat
                return BaseStake;
            var stake = LostMoney / (TotalOdds - GroupSize);
            return Math.Max(stake * Value / 100, BaseStake); // groupOdds * stake = stake * groupSize + lostMoney
        }

        protected double UsePercentOfBudget() => Math.Max(Budget * Value / 100 / GroupSize, BaseStake);
    }


    public class FlatFlatStaking : StakingManager
    {
        public FlatFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake) 
            : base(isLost, baseStake, previousStake, resetStake) { }
    }

    public class AddFlatStaking : StakingManager
    {
        public AddFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAdd, double lostInARow, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = valueToAdd;
            InARow = lostInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Add() : FlatStake;
    }

    public class SubtractFlatStaking : StakingManager
    {
        public SubtractFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToSubtractIfLost, double lostInARow, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = valueToSubtractIfLost;
            InARow = lostInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Subtract() : FlatStake;
    }

    public class MultiplyFlatStaking : StakingManager
    {
        public MultiplyFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double multiplierIfLost, double lostInARow, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = multiplierIfLost;
            InARow = lostInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Multiply() : FlatStake;
    }

    public class DivideFlatStaking : StakingManager
    {
        public DivideFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double dividerIfLost, int lostInARow, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = dividerIfLost;
            InARow = lostInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Divide() : FlatStake;
    }

    public class CoverPercentOfLosesFlatStaking : StakingManager
    {
        public CoverPercentOfLosesFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentCoveredIfLost, double lostMoney, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = percentCoveredIfLost;
            LostMoney = lostMoney;
            GroupSize = groupSize;
            TotalOdds = totalOdds;
        }

        public override double Apply() => IsLost ? CoverPercentage() : FlatStake;
    }

    public class PercentOfBudgetFlatStaking : StakingManager
    {
        public PercentOfBudgetFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudget, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            Value = percentageOfBudget;
        }

        public override double Apply() => IsLost ? UsePercentOfBudget() : FlatStake;
    }


    public class FlatAddStaking : StakingManager
    {
        public FlatAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfWon, double wonInARow, int groupSize) // jedna stała, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = valueToAddIfWon;
            InARow = wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? FlatStake : Add();
    }

    public class AddAddStaking : StakingManager
    {
        public AddAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToAddIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => Add();
    }

    public class SubtractAddStaking : StakingManager
    {
        public SubtractAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToSubtractIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToSubtractIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Subtract() : Add();
    }

    public class MultiplyAddStaking : StakingManager
    {
        public MultiplyAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double multiplierIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? multiplierIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Multiply() : Add();
    }

    public class DivideAddStaking : StakingManager
    {
        public DivideAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double dividerIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? dividerIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Divide() : Add();
    }

    public class CoverPercentOfLosesAddStaking : StakingManager
    {
        public CoverPercentOfLosesAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageCoveredIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, double lostMoney, double totalOdds, int groupSize) // jedna procentowa, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? percentageCoveredIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            LostMoney = lostMoney;
            TotalOdds = totalOdds;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? CoverPercentage() : Add();
    }

    public class PercentOfBudgetAddStaking : StakingManager
    {
        public PercentOfBudgetAddStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudgetIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? percentageOfBudgetIfLost : valueToAddIfWon;
        }

        public override double Apply() => IsLost ? UsePercentOfBudget() : Add();
    }


    public class FlatSubtractStaking : StakingManager
    {
        public FlatSubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfWon, double wonInARow, int groupSize) // jedna stała, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = valueToAddIfWon;
            InARow = wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? FlatStake : Subtract();
    }

    public class AddSubtractStaking : StakingManager
    {
        public AddSubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToAddIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Add() : Subtract();
    }

    public class SubtractSubtractStaking : StakingManager
    {
        public SubtractSubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToSubtractIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToSubtractIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => Subtract();
    }

    public class MultiplySubtractStaking : StakingManager
    {
        public MultiplySubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double multiplierIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? multiplierIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Multiply() : Subtract();
    }

    public class DivideSubtractStaking : StakingManager
    {
        public DivideSubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double dividerIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? dividerIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Divide() : Subtract();
    }

    public class CoverPercentOfLosesSubtractStaking : StakingManager
    {
        public CoverPercentOfLosesSubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageCoveredIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, double lostMoney, double totalOdds, int groupSize) // jedna procentowa, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? percentageCoveredIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            LostMoney = lostMoney;
            TotalOdds = totalOdds;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? CoverPercentage() : Subtract();
    }

    public class PercentOfBudgetSubtractStaking : StakingManager
    {
        public PercentOfBudgetSubtractStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudgetIfLost, double valueToSubtractIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? percentageOfBudgetIfLost : valueToSubtractIfWon;
        }

        public override double Apply() => IsLost ? UsePercentOfBudget() : Subtract();
    }


    public class FlatMultiplyStaking : StakingManager
    {
        public FlatMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfWon, double wonInARow, int groupSize) // jedna stała, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = valueToAddIfWon;
            InARow = wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? FlatStake : Multiply();
    }

    public class AddMultiplyStaking : StakingManager
    {
        public AddMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToAddIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Add() : Multiply();
    }

    public class SubtractMultiplyStaking : StakingManager
    {
        public SubtractMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToSubtractIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToSubtractIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Subtract() : Multiply();
    }

    public class MultiplyMultiplyStaking : StakingManager
    {
        public MultiplyMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double multiplierIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? multiplierIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => Multiply();
    }

    public class DivideMultiplyStaking : StakingManager
    {
        public DivideMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double dividerIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? dividerIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Divide() : Multiply();
    }

    public class CoverPercentOfLosesMultiplyStaking : StakingManager
    {
        public CoverPercentOfLosesMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageCoveredIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, double lostMoney, double totalOdds, int groupSize) // jedna procentowa, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? percentageCoveredIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            LostMoney = lostMoney;
            TotalOdds = totalOdds;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? CoverPercentage() : Multiply();
    }

    public class PercentOfBudgetMultiplyStaking : StakingManager
    {
        public PercentOfBudgetMultiplyStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudgetIfLost, double valueToMultiplyIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? percentageOfBudgetIfLost : valueToMultiplyIfWon;
        }

        public override double Apply() => IsLost ? UsePercentOfBudget() : Multiply();
    }


    public class FlatDivideStaking : StakingManager
    {
        public FlatDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfWon, double wonInARow, int groupSize) // jedna stała, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = valueToAddIfWon;
            InARow = wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? FlatStake : Divide();
    }

    public class AddDivideStaking : StakingManager
    {
        public AddDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToAddIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Add() : Divide();
    }

    public class SubtractDivideStaking : StakingManager
    {
        public SubtractDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToSubtractIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? valueToSubtractIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Subtract() : Divide();
    }

    public class MultiplyDivideStaking : StakingManager
    {
        public MultiplyDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double multiplierIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? multiplierIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? Multiply() : Divide();
    }

    public class DivideDivideStaking : StakingManager
    {
        public DivideDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double dividerIfLost, double dividerIfWon, double lostInARow, double wonInARow, int groupSize) // dwie zmienne
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? dividerIfLost : dividerIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            GroupSize = groupSize;
        }

        public override double Apply() => Divide();
    }

    public class CoverPercentOfLosesDivideStaking : StakingManager
    {
        public CoverPercentOfLosesDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageCoveredIfLost, double valueToAddIfWon, double lostInARow, double wonInARow, double lostMoney, double totalOdds, int groupSize) // jedna procentowa, jedna zmienna
            : base(isLost, baseStake, previousStake, resetStake)
        {
            Value = isLost ? percentageCoveredIfLost : valueToAddIfWon;
            InARow = isLost ? lostInARow : wonInARow;
            LostMoney = lostMoney;
            TotalOdds = totalOdds;
            GroupSize = groupSize;
        }

        public override double Apply() => IsLost ? CoverPercentage() : Divide();
    }

    public class PercentOfBudgetDivideStaking : StakingManager
    {
        public PercentOfBudgetDivideStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudgetIfLost, double valueToDivideIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? percentageOfBudgetIfLost : valueToDivideIfWon;
        }

        public override double Apply() => IsLost ? UsePercentOfBudget() : Divide();
    }


    public class FlatPercentOfBudgetStaking : StakingManager
    {
        public FlatPercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudgetIfWon, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            if (!isLost)
                Value = percentageOfBudgetIfWon;
        }

        public override double Apply() => IsLost ? FlatStake : UsePercentOfBudget();
    }

    public class AddPercentOfBudgetStaking : StakingManager
    {
        public AddPercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToAddIfLost, double percentageOfBudgetIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? valueToAddIfLost : percentageOfBudgetIfWon;
        }

        public override double Apply() => IsLost ? Add() : UsePercentOfBudget();
    }

    public class SubtractPercentOfBudgetStaking : StakingManager
    {
        public SubtractPercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToSubtractIfLost, double percentageOfBudgetIfWon, double budget, double lostInARow, double wonInARow, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? valueToSubtractIfLost : percentageOfBudgetIfWon;
        }

        public override double Apply() => IsLost ? Subtract() : UsePercentOfBudget();
    }

    public class MultiplyPercentOfBudgetStaking : StakingManager
    {
        public MultiplyPercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToMultiplyIfLost, double percentageOfBudgetIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? valueToMultiplyIfLost : percentageOfBudgetIfWon;
        }

        public override double Apply() => IsLost ? Multiply() : UsePercentOfBudget();
    }

    public class DividePercentOfBudgetStaking : StakingManager
    {
        public DividePercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double valueToDivideIfLost, double percentageOfBudgetIfWon, double lostInARow, double wonInARow, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            InARow = isLost ? lostInARow : wonInARow;
            Value = isLost ? valueToDivideIfLost : percentageOfBudgetIfWon;
        }

        public override double Apply() => IsLost ? Divide() : UsePercentOfBudget();
    }

    public class CoverPercentOfLosesPercentOfBudgetStaking : StakingManager
    {
        public CoverPercentOfLosesPercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfLosesToCover, double percentageOfBudgetIfWon, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            Value = isLost ? percentageOfLosesToCover : percentageOfBudgetIfWon;
        }

        public override double Apply() => IsLost ? CoverPercentage() : UsePercentOfBudget();
    }

    public class PercentOfBudgetPercentOfBudgetStaking : StakingManager
    {
        public PercentOfBudgetPercentOfBudgetStaking(bool isLost, double baseStake, double previousStake, bool resetStake, double percentageOfBudgetIfLost, double percentageOfBudgetIfWon, double budget, double totalOdds, int groupSize)
            : base(isLost, baseStake, previousStake, resetStake)
        {
            GroupSize = groupSize;
            TotalOdds = totalOdds;
            Budget = budget;
            Value = isLost ? percentageOfBudgetIfLost : percentageOfBudgetIfWon;
        }

        public override double Apply() => UsePercentOfBudget();
    }
}
