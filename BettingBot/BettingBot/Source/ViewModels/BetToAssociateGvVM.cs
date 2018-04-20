using BettingBot.Common.UtilityClasses;

namespace BettingBot.Source.ViewModels
{
    public class BetToAssociateGvVM : BaseVM
    {
        public int _id;
        private ExtendedTime _timestamp;
        private string _tipsterName;
        private string _leagueName;
        private string _matchHomeName;
        private string _matchAwayName;

        private int? _matchId;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public ExtendedTime LocalTimestamp { get => _timestamp; set => SetPropertyAndNotify(ref _timestamp, value, nameof(LocalTimestamp)); }
        public string TipsterName { get => _tipsterName; set => SetPropertyAndNotify(ref _tipsterName, value, nameof(TipsterName)); }
        public string LeagueName { get => _leagueName; set => SetPropertyAndNotify(ref _leagueName, value, nameof(LeagueName)); }
        public string MatchHomeName { get => _matchHomeName; set => SetPropertyAndNotify(ref _matchHomeName, value, nameof(MatchHomeName)); }
        public string MatchAwayName { get => _matchAwayName; set => SetPropertyAndNotify(ref _matchAwayName, value, nameof(MatchAwayName)); }

        public int? MatchId { get => _matchId; set => SetPropertyAndNotify(ref _matchId, value, nameof(MatchId)); }

        public string DateString => LocalTimestamp.Rfc1123.ToString("dd-MM-yyyy HH:mm");
    }
}
