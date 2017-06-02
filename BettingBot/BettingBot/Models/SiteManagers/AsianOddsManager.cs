using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models.SiteManagers
{
    public class AsianOddsManager : BettingSiteManager
    {
        public override BetToSendVM FindBet(BetToDisplayVM betTdVM)
        {
            Sdm.OpenOrReuseDriver();
            EnsureLogin();

            //

            var betTsVM = new BetToSendVM(Bookmaker.AsianOdds, betTdVM.Match, 0);
            FoundBets.Add(betTsVM);
            return betTsVM;
        }
        
        public override void Login()
        {
            
        }

        public override bool IsLogged()
        {
            return true;
        }
    }
}
