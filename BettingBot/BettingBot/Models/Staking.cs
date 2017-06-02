using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models
{
    public abstract class Staking
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

        protected Staking(bool isLost, double baseStake, double previousStake, bool resetStake) // stałe stawki
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
            var stake = LostMoney / (TotalOdds - GroupSize);
            return Math.Max(stake * Value / 100, BaseStake); // groupOdds * stake = stake * groupSize + lostMoney
        }

        protected double UsePercentOfBudget() => Math.Max(Budget * Value / 100 / GroupSize, BaseStake);
    }


    public class FlatFlatStaking : Staking
    {
        public FlatFlatStaking(bool isLost, double baseStake, double previousStake, bool resetStake) 
            : base(isLost, baseStake, previousStake, resetStake) { }
    }

    public class AddFlatStaking : Staking
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

    public class SubtractFlatStaking : Staking
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

    public class MultiplyFlatStaking : Staking
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

    public class DivideFlatStaking : Staking
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

    public class CoverPercentOfLosesFlatStaking : Staking
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

    public class PercentOfBudgetFlatStaking : Staking
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


    public class FlatAddStaking : Staking
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

    public class AddAddStaking : Staking
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

    public class SubtractAddStaking : Staking
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

    public class MultiplyAddStaking : Staking
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

    public class DivideAddStaking : Staking
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

    public class CoverPercentOfLosesAddStaking : Staking
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

    public class PercentOfBudgetAddStaking : Staking
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


    public class FlatSubtractStaking : Staking
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

    public class AddSubtractStaking : Staking
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

    public class SubtractSubtractStaking : Staking
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

    public class MultiplySubtractStaking : Staking
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

    public class DivideSubtractStaking : Staking
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

    public class CoverPercentOfLosesSubtractStaking : Staking
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

    public class PercentOfBudgetSubtractStaking : Staking
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


    public class FlatMultiplyStaking : Staking
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

    public class AddMultiplyStaking : Staking
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

    public class SubtractMultiplyStaking : Staking
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

    public class MultiplyMultiplyStaking : Staking
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

    public class DivideMultiplyStaking : Staking
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

    public class CoverPercentOfLosesMultiplyStaking : Staking
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

    public class PercentOfBudgetMultiplyStaking : Staking
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


    public class FlatDivideStaking : Staking
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

    public class AddDivideStaking : Staking
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

    public class SubtractDivideStaking : Staking
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

    public class MultiplyDivideStaking : Staking
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

    public class DivideDivideStaking : Staking
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

    public class CoverPercentOfLosesDivideStaking : Staking
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

    public class PercentOfBudgetDivideStaking : Staking
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


    public class FlatPercentOfBudgetStaking : Staking
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

    public class AddPercentOfBudgetStaking : Staking
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

    public class SubtractPercentOfBudgetStaking : Staking
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

    public class MultiplyPercentOfBudgetStaking : Staking
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

    public class DividePercentOfBudgetStaking : Staking
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

    public class CoverPercentOfLosesPercentOfBudgetStaking : Staking
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

    public class PercentOfBudgetPercentOfBudgetStaking : Staking
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
