using BettingBot.Common;
using BettingBot.Source.Models;

namespace BettingBot.Source.Converters
{
    public static class MatchResultConverter
    {
        public static MatchResult ParseToMatchResultResponse(string matchResult)
        {
            var matchResultStr = matchResult.RemoveHTMLSymbols().Remove(" ");
            if (matchResultStr.Contains("-"))
            {
                var homeScore = matchResultStr.BeforeFirst("-").ToIntN();
                var awayScore = matchResultStr.AfterLast("-").ToIntN();
                if (homeScore != null && awayScore != null)
                    return new MatchResult(homeScore.ToInt(), awayScore.ToInt());
            }
            return MatchResult.Inconclusive();
        }
    }
}
