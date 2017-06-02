using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models.SiteManagers
{
    public class CloudBetManager : BettingSiteManager
    {
        public override BetToSendVM FindBet(BetToDisplayVM betTdVM)
        {
            //Sdm.OpenOrReuseDriver();
            //EnsureLogin();

            //

            var betTsVM = new BetToSendVM(Bookmaker.CloudBet, betTdVM.Match, 0);
            return betTsVM;
        }

        public override void Login()
        {
            //
        }

        public override bool IsLogged()
        {
            return false;
        }
    }
}
