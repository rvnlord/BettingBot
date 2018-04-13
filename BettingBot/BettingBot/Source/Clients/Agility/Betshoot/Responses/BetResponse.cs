using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Responses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.Models;

namespace BettingBot.Source.Clients.Agility.Betshoot.Responses
{
    public class BetResponse
    {
        public ExtendedTime Date { get; set; }
        public string HomeName { get; set; }
        public string AwayName { get; set; }
        public PickResponse Pick { get; set; }
        public MatchResult MatchResult { get; set; } // przy dziedziczeniu nadpisac tą właściwość na dzieidzeczącą z niej
        public BetResult BetResult { get; set; }
        public double Odds { get; set; }

        public DbBet ToDbBet() => BetConverter.ToDbBet(this); // data jest już w utc przed konwersją
    }
}
