using System;
using System.Text;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Converters;

namespace BettingBot.Source.ViewModels
{
    public class BetToDisplayGvVM : BaseVM
    {
        private int _id;
        private double _odds;
        private BetResult _betResult;

        private string _tipsterName;
        private string _tipsterWebsite;
        private string _matchHomeName;
        private string _matchAwayName;
        private int? _matchHomeScore;
        private int? _matchAwayScore;
        private ExtendedTime _timestamp;
        private PickChoice _pickChoice;
        private double? _pickValue;
        private DisciplineType? _discipline;
        private string _leagueName;

        private string _unparsedPickString;
        private string _unparsedMatchString;
        
        private int _nr;
        private double _stake;
        private double _profit;
        private double _budget;
        private double _budgetBeforeResult;
        
        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public double Odds { get => _odds; set => SetPropertyAndNotify(ref _odds, value, nameof(Odds)); }
        public BetResult BetResult { get => _betResult; set => SetPropertyAndNotify(ref _betResult, value, nameof(BetResult)); }

        public string TipsterName { get => _tipsterName; set => SetPropertyAndNotify(ref _tipsterName, value, nameof(TipsterName)); }
        public string TipsterWebsite { get => _tipsterWebsite; set => SetPropertyAndNotify(ref _tipsterWebsite, value, nameof(TipsterName)); }
        public string MatchHomeName { get => _matchHomeName; set => SetPropertyAndNotify(ref _matchHomeName, value, nameof(MatchHomeName)); }
        public string MatchAwayName { get => _matchAwayName; set => SetPropertyAndNotify(ref _matchAwayName, value, nameof(MatchAwayName)); }
        public int? MatchHomeScore { get => _matchHomeScore; set => SetPropertyAndNotify(ref _matchHomeScore, value, nameof(MatchHomeScore)); }
        public int? MatchAwayScore { get => _matchAwayScore; set => SetPropertyAndNotify(ref _matchAwayScore, value, nameof(MatchAwayScore)); }
        public ExtendedTime LocalTimestamp { get => _timestamp; set => SetPropertyAndNotify(ref _timestamp, value, nameof(LocalTimestamp)); }
        public PickChoice PickChoice { get => _pickChoice; set => SetPropertyAndNotify(ref _pickChoice, value, nameof(PickChoice)); }
        public double? PickValue { get => _pickValue; set => SetPropertyAndNotify(ref _pickValue, value, nameof(PickValue)); }
        public DisciplineType? Discipline { get => _discipline; set => SetPropertyAndNotify(ref _discipline, value, nameof(Discipline)); }
        public string LeagueName { get => _leagueName; set => SetPropertyAndNotify(ref _leagueName, value, nameof(LeagueName)); }

        public int Nr { get => _nr; set => SetPropertyAndNotify(ref _nr, value, nameof(Nr)); }
        
        public double Stake { get => _stake; set => SetPropertyAndNotify(ref _stake, value, nameof(Stake)); }
        public double Profit { get => _profit; set => SetPropertyAndNotify(ref _profit, value, nameof(Profit)); }
        public double Budget { get => _budget; set => SetPropertyAndNotify(ref _budget, value, nameof(Budget)); }
        public double BudgetBeforeResult { get => _budgetBeforeResult; set => SetPropertyAndNotify(ref _budgetBeforeResult, value, nameof(BudgetBeforeResult)); }
        
        public string TipsterString => $"{_tipsterName} ({_tipsterWebsite.Take(1)})";
        public string OddsString => Odds <= 0 ? "" : $"{Odds:0.00}";
        public string StakeString => (Stake < 0 ? "-" + $"{Stake:0.##}".Substring(1) : $"{Stake:0.##}") + " zł";
        public string ProfitString => BetResult == BetResult.Pending 
            ? "" 
            : Profit < 0 
                ? ("-" + $"{Profit:0.##}".Substring(1) + " zł") 
                : Profit > 0 
                    ? ("+" + $"{Profit:0.##}" + " zł")
                    : "-";
        public string BudgetString => BetResult == BetResult.Pending 
            ? "" 
            : Profit.Eq(0)
                ? "-"
                : (Budget < 0 ? "-" + $"{Budget:0.##}".Substring(1) : $"{Budget:0.##}") + " zł";
        public string DateString => LocalTimestamp.Rfc1123.ToString("dd-MM-yyyy HH:mm");

        public string MatchResultString
        {
            get
            {
                if (BetResult == BetResult.Pending)
                    return "";
                if (BetResult == BetResult.Canceled && _matchHomeScore == null && _matchAwayScore == null)
                    return "-";

                return $"{_matchHomeScore} - {_matchAwayScore}";
            }
        }

        public string BetResultString => BetConverter.BetResultToLocalizedString(BetResult);

        public string DisciplineString => DisciplineConverter.DisciplineTypeToLocalizedString(Discipline);

        public string PickString
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(PickConverter.PickToString(PickChoice, PickValue));
                if (PickChoice == PickChoice.Other)
                    sb.Append($" {_unparsedPickString}");
                return sb.ToString();
            }
        }

        public string IsAssociatedString => IsAssociatedWithArbitraryData 
            ? "+" 
            : TriedAssociateWithMatch
                ? "-"
                : "";

        public bool TriedAssociateWithMatch { get; set; }

        public bool IsBetResultOriginal { get; set; }
        public bool IsMatchHomeNameOriginal { get; set; }
        public bool IsMatchAwayNameOriginal { get; set; }
        public bool IsMatchHomeScoreOriginal { get; set; }
        public bool IsMatchAwayScoreOriginal { get; set; }
        public bool IsLocalTimestampOriginal { get; set; }
        public bool IsAssociatedWithArbitraryData { get; set; }
        public bool IsDisciplineOriginal { get; set; }
        public bool IsLeagueNameOriginal { get; set; }

        public void SetUnparsedPickString(string unparsedPickString) => _unparsedPickString = unparsedPickString;
        public string GetUnparsedPickString() => _unparsedPickString;

        public void SetUnparsedMatchString(string dbBetOriginalHomeName, string dbBetOriginalAwayName) => _unparsedMatchString = $"{dbBetOriginalHomeName} - {dbBetOriginalAwayName}";
        public string GetUnparsedMatchString() => _unparsedMatchString;
        
        public void CalculateProfit(double currStake, ref double budget)
        {
            BudgetBeforeResult = budget - currStake;

            if (BetResult == BetResult.Lose)
                Profit = -currStake;
            else if (BetResult == BetResult.Canceled || BetResult == BetResult.Pending)
                Profit = 0;
            else if (BetResult == BetResult.HalfLost)
                Profit = -currStake / 2;
            else if (BetResult == BetResult.HalfWon)
                Profit = (currStake * Odds - currStake) / 2;
            else
                Profit = currStake * Odds - currStake;

            budget += Profit;
            Budget = budget;
        }

        public BetToDisplayGvVM Copy()
        {
            return BetConverter.CopyBetToDisplayGvVM(this);
        }

        public override string ToString() => $"{Id} - {DateString}, {MatchHomeName} - {MatchAwayName}, {TipsterName}";

        public BetToAssociateGvVM ToBetToAssociateGvVM()
        {
            return BetConverter.ToBetToAssociateGvVM(this);
        }
    }
}
