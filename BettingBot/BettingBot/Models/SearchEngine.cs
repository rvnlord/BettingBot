using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BettingBot.Models.SiteManagers;
using BettingBot.Models.ViewModels;

namespace BettingBot.Models
{
    public class SearchEngine
    {
        public List<BetToSendVM> FoundBets { get; set; }
        public List<BettingSiteManager> SiteManagers { get; }

        public SearchEngine()
        {
            FoundBets = new List<BetToSendVM>();
            SiteManagers = new List<BettingSiteManager>
            {
                new AsianOddsManager(),
                new CloudBetManager()
            };
        }

        public List<BetToSendVM> FindBet(BetToDisplayGvVM betTdVM)
        {
            var newBets = SiteManagers.Select(sm => sm.FindBet(betTdVM)).ToList();
            FoundBets.AddRange(newBets);
            return newBets;
        }
    }
}
