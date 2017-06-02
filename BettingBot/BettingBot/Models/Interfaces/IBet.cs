using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models.Interfaces
{
    public interface IBet
    {
        int TipsterId { get; set; }
        DateTime Date { get; set; }
        string Match { get; set; }
        Pick Pick { get; set; }
        int BetResult { get; set; }
        string MatchResult { get; set; }
        double Odds { get; set; }

        IBet DeepClone();
    }
}
