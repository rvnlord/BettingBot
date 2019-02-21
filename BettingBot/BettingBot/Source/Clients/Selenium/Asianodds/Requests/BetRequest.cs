using System;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using BettingBot.Source.Converters;

namespace BettingBot.Source.Clients.Selenium.Asianodds.Requests
{
    public class BetRequest
    {
        public ExtendedTime Date { get; set; }
        public DisciplineType? Discipline { get; set; }
        public string LeagueName { get; set; }
        public string MatchHomeName { get; set; }
        public string MatchAwayName { get; set; }
        public PickChoice? PickChoice { get; set; }
        public double? PickValue { get; set; }
        public double Stake { get; set; }

        public int? MatchId { get; set; }

        public string Keyword { get; set; }
        public int HomeCommonWords { get; set; }
        public int AwayCommonWords { get; set; }
        public TimeSpan TimeDifference { get; set; }
        public string XPath { get; set; }
        public string TimePeriodXPath { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is BetRequest)) return false;
            var oBetResponse = (BetRequest)obj;

            return Date == oBetResponse.Date
                && Discipline == oBetResponse.Discipline
                && LeagueName.EqIgnoreCase(oBetResponse.LeagueName)
                && MatchHomeName.EqIgnoreCase(oBetResponse.MatchHomeName)
                && MatchAwayName.EqIgnoreCase(oBetResponse.MatchAwayName)
                && PickChoice == oBetResponse.PickChoice
                && PickValue.Eq(oBetResponse.PickValue);
        }

        public override int GetHashCode()
        {
            return Date.GetHashCode() ^ 7
                * Discipline.GetHashCode() ^ 11
                * LeagueName.GetHashCode() ^ 17
                * MatchHomeName.GetHashCode() ^ 19
                * MatchAwayName.GetHashCode() ^ 23
                * PickChoice.GetHashCode() ^ 29
                * PickValue.GetHashCode() ^ 31;
        }

        public override string ToString()
        {
            return $"[{Date.Rfc1123:dd-MM-yyyy HH:mm}] {MatchHomeName} - {MatchAwayName}: {PickChoice.EnumToString()} {PickValue:0.##} (s: {Stake:0.00})";
        }
    }
}
