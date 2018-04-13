using System;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Source.Clients.Agility.Betshoot.Responses
{
    public class BetshootResponse : ResponseBase
    {
        public string Error { get; set; }
        public ExtendedTime Time { get; set; }

        public BetshootResponse RawParse(string html)
        {
            Time = DateTime.Now.ToExtendedTime(TimeZoneKind.CurrentLocal).ToUTC();
            return this;
        }

        public void HandleErrors(string html)
        {
            RawParse(html);
            if (Error != null)
                throw new BetshootException(Error);
        }
    }
}
