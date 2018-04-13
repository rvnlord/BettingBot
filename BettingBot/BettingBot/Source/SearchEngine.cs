using System.Collections.Generic;
using System.Linq;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source
{
    //public class SearchEngine
    //{
    //    public List<BetToSendVM> FoundBets { get; set; }
    //    public List<BettingSiteClient> SiteManagers { get; }

    //    public SearchEngine()
    //    {
    //        FoundBets = new List<BetToSendVM>();
    //        SiteManagers = new List<BettingSiteClient>
    //        {
    //            new AsianOddsClient(),
    //            new CloudBetClient()
    //        };
    //    }

    //    public List<BetToSendVM> FindBet(BetToDisplayGvVM betTdVM)
    //    {
    //        var newBets = SiteManagers.Select(sm => sm.FindBet(betTdVM)).ToList();
    //        FoundBets.AddRange(newBets);
    //        return newBets;
    //    }
    //}
}
