using System;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;

namespace BettingBot.Source.Clients.Selenium.Hintwise.Responses
{
    public class HintwiseResponse : ResponseBase
    {
        public string Error { get; set; }
        public ExtendedTime Time { get; set; }

        public HintwiseResponse RawParse(SeleniumDriverManager sdm)
        {
            Time = DateTime.Now.ToExtendedTime(TimeZoneKind.CurrentLocal).ToUTC();
            return this;
        }

        public void HandleErrors(SeleniumDriverManager sdm)
        {
            RawParse(sdm);
            if (Error != null)
                throw new HintwiseException(Error);
        }
    }
}
