using System;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Source.Clients.Selenium.Asianodds.Responses
{
    public class AsianoddsResponse : ResponseBase
    {
        public string Error { get; set; }
        public ExtendedTime Time { get; set; }

        public AsianoddsResponse RawParse(SeleniumDriverManager sdm)
        {
            Time = DateTime.Now.ToExtendedTime(TimeZoneKind.CurrentLocal).ToUTC();
            return this;
        }

        public void HandleErrors(SeleniumDriverManager sdm)
        {
            RawParse(sdm);
            if (Error != null)
                throw new AsianoddsException(Error);
        }
    }
}
