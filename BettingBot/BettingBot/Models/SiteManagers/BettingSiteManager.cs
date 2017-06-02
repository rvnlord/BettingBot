using System;
using System.Collections.Generic;
using BettingBot.Models.Interfaces;

namespace BettingBot.Models.SiteManagers
{
    public abstract class BettingSiteManager : ISeleniumConnectable
    {
        public SeleniumDriverManager Sdm { get; set; } = new SeleniumDriverManager();
        public List<BetToSendVM> FoundBets { get; set; } = new List<BetToSendVM>();

        public abstract BetToSendVM FindBet(BetToDisplayVM BetTdVM);
        public abstract void Login();
        public abstract bool IsLogged();

        public virtual void EnsureLogin()
        {
            if (IsLogged()) return;
            Sdm.OpenOrReuseDriver();
            Login();
            if (!IsLogged()) throw new Exception("Niepoprawne dane logowania do strony");
        }
    }

    public enum Bookmaker
    {
        AsianOdds,
        CloudBet
    }
}
