using System;
using System.Text;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Converters;

namespace BettingBot.Source.ViewModels
{
    public class SentBetGvVM : BaseVM
    {
        private int _id;
        private ExtendedTime _timestamp;
        private BetResult _betResult;
        private double _odds;
        private double _stake;
        private PickChoice _pickChoice;
        private double? _pickValue;
        private string _originalHomeName;
        private string _originalAwayName;
        private string _originalLeagueName;
        private int _nr;
        private DisciplineType? _discipline;
        private double _profit;
        private double _budget;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public ExtendedTime LocalTimestamp { get => _timestamp; set => SetPropertyAndNotify(ref _timestamp, value, nameof(LocalTimestamp)); }
        public BetResult BetResult { get => _betResult; set => SetPropertyAndNotify(ref _betResult, value, nameof(BetResult)); }
        public double Odds { get => _odds; set => SetPropertyAndNotify(ref _odds, value, nameof(Odds)); }
        public double Stake { get => _stake; set => SetPropertyAndNotify(ref _stake, value, nameof(Stake)); }
        public PickChoice PickChoice { get => _pickChoice; set => SetPropertyAndNotify(ref _pickChoice, value, nameof(PickChoice)); }
        public double? PickValue { get => _pickValue; set => SetPropertyAndNotify(ref _pickValue, value, nameof(PickValue)); }
        public string HomeName { get => _originalHomeName; set => SetPropertyAndNotify(ref _originalHomeName, value, nameof(HomeName)); }
        public string AwayName { get => _originalAwayName; set => SetPropertyAndNotify(ref _originalAwayName, value, nameof(AwayName)); }
        public string LeagueName { get => _originalLeagueName; set => SetPropertyAndNotify(ref _originalLeagueName, value, nameof(LeagueName)); }
        public DisciplineType? Discipline { get => _discipline; set => SetPropertyAndNotify(ref _discipline, value, nameof(Discipline)); }
        public double Profit { get => _profit; set => SetPropertyAndNotify(ref _profit, value, nameof(Profit)); }
        public double Budget { get => _budget; set => SetPropertyAndNotify(ref _budget, value, nameof(Budget)); }

        public int Nr { get => _nr; set => SetPropertyAndNotify(ref _nr, value, nameof(Nr)); }

        public int? MatchId { get; set; }

        public string DisciplineString => DisciplineConverter.DisciplineTypeToLocalizedString(Discipline);
        public string PickString
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(PickConverter.PickToString(PickChoice, PickValue));
                if (PickChoice == PickChoice.Other)
                    throw new InvalidCastException("Typ wysłanego zakładu nie może nie być zparsowany");
                return sb.ToString();
            }
        }
        public string BetResultString => BetConverter.BetResultToLocalizedString(BetResult);
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
        public string OddsString => Odds <= 0 ? "" : $"{Odds:0.000}";
    }
}
