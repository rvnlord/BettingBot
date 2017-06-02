using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BettingBot.Models.SiteManagers;

namespace BettingBot.Models
{
    public class BetToSendVM
    {
        public Bookmaker Bookmaker { get; set; }
        public string Match { get; set; }
        public double Odds { get; set; }

        public BetToSendVM(Bookmaker bookmaker, string match, double odds)
        {
            Bookmaker = bookmaker;
            Match = match;
            Odds = odds;
        }

        public override string ToString()
        {
            return $"{Enum.GetName(typeof(Bookmaker), Bookmaker) }, {Match} - {Odds}";
        }

        public string ToStringWoMatch()
        {
            return $"{Enum.GetName(typeof(Bookmaker), Bookmaker) } - {Odds}";
        }
    }
}
