namespace BettingBot.Source.ViewModels
{
    public class AggregatedWinLoseStatisticGvVM : BaseVM
    {
        private int _count;
        private int _loses;
        private int _wins;

        public int Count { get => _count; set => SetPropertyAndNotify(ref _count, value, nameof(Count)); }
        public int Loses { get => _loses; set => SetPropertyAndNotify(ref _loses, value, nameof(Loses)); }
        public int Wins { get => _wins; set => SetPropertyAndNotify(ref _wins, value, nameof(Wins)); }

        public AggregatedWinLoseStatisticGvVM(int count, int loses, int wins)
        {
            Count = count;
            Loses = loses;
            Wins = wins;
        }
    }
}
