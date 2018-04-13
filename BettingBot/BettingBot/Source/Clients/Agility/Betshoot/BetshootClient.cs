using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Agility.Betshoot.Responses;

namespace BettingBot.Source.Clients.Agility.Betshoot
{
    public class BetshootClient : AgilityClient
    {
        private readonly TimeZoneKind _timeZone;

        public BetshootClient()
        {
            _address = "https://www.betshoot.com/competition/users/";
            _timeZone = TimeZoneKind.GreenwichStandardTime;
        }

        public TipsterAddressResponse TipsterAddress(string tipsterName)
        {
            return Get(null, html => new TipsterAddressResponse()
                .ReceiveInfoWith<TipsterAddressResponse>(Response_InformationSent)
                .Parse(html, tipsterName, _address));
        }
        
        public TipsterResponse Tipster(string tipsterAddress)
        {
            var relativeAddress = tipsterAddress.Remove(_address);
            return Get(relativeAddress, html => new TipsterResponse()
                .ReceiveInfoWith<TipsterResponse>(Response_InformationSent)
                .Parse(html, tipsterAddress));
        }

        public BetsResponse Tips(TipsterResponse tipster, ExtendedTime fromDate = null)
        {
            return Get(tipster.Name, html => new BetsResponse()
                .ReceiveInfoWith<BetsResponse>(Response_InformationSent)
                .Parse(html, tipster, fromDate, _timeZone));
        }
    }
}
