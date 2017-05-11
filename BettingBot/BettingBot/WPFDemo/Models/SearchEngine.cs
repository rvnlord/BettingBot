using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFDemo.Models.SiteManagers;

namespace WPFDemo.Models
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

        public List<BetToSendVM> FindBet(BetToDisplayVM betTdVM)
        {
            var newBets = SiteManagers.Select(sm => sm.FindBet(betTdVM)).ToList();
            FoundBets.AddRange(newBets);
            return newBets;
        }
    }
}
