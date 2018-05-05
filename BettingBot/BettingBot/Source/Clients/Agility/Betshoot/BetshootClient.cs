using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Agility.Betshoot.Responses;

namespace BettingBot.Source.Clients.Agility.Betshoot
{
    public class BetshootClient : AgilityClient
    {
        public BetshootClient() 
            : base(
                    "https://www.betshoot.com/competition/users/", 
                    TimeZoneKind.GreenwichStandardTime) { }

        public TipsterAddressResponse TipsterAddress(string tipsterName)
        {
            return Get(null, (html, arm) => new TipsterAddressResponse()
                .ReceiveInfoWith<TipsterAddressResponse>(Response_InformationSent)
                .Parse(html, tipsterName, _address));
        }
        
        public TipsterResponse Tipster(string tipsterAddress)
        {
            var relativeAddress = tipsterAddress.Remove(_address);
            return Get(relativeAddress, (html, arm) => new TipsterResponse()
                .ReceiveInfoWith<TipsterResponse>(Response_InformationSent)
                .Parse(html, tipsterAddress));
        }

        public BetsResponse Tips(TipsterResponse tipster, ExtendedTime fromDate = null)
        {
            return Get(tipster.Name, (html, arm) => new BetsResponse()
                .ReceiveInfoWith<BetsResponse>(Response_InformationSent)
                .Parse(html, arm, tipster, fromDate, _timeZone));
        }
    }
}
