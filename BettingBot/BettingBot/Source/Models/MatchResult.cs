namespace BettingBot.Source.Models
{
    public class MatchResult
    {
        public int? HomeScore { get; }
        public int? AwayScore { get; }
        public MatchResultType Result { get; }

        public MatchResult(int homeScore, int awayScore)
        {
            HomeScore = homeScore;
            AwayScore = awayScore;
            Result = homeScore > awayScore
                ? MatchResultType.HomeWin
                : homeScore < awayScore
                    ? MatchResultType.AwayWin
                    : MatchResultType.Draw;
        }

        private MatchResult() => Result = MatchResultType.None;
        public static MatchResult Inconclusive() => new MatchResult();
        public override string ToString() => HomeScore == null && AwayScore == null ? "" : $"{HomeScore} - {AwayScore}";
    }

    public enum MatchResultType
    {
        None,
        HomeWin,
        AwayWin,
        Draw
    }
}
