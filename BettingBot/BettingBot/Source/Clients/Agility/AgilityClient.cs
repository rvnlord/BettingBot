using System;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Agility.Betshoot;
using RestSharp;

namespace BettingBot.Source.Clients.Agility
{
    public abstract class AgilityClient : Client
    {
        protected AgilityRestManager _arm;

        protected AgilityClient(string address, TimeZoneKind timeZone) : base(address, timeZone)
        {
            _arm = new AgilityRestManager();
        }

        protected virtual T Get<T>(string action, DeserializeAgilityResponse<T> agilityDeserializer) where T : ResponseBase
        {
            var url = _address + action;
            if (!url.EndsWith("/")) url += "/";

            var html = _arm.GetHtml(url);
            return agilityDeserializer(html, _arm);
        }

        public delegate T DeserializeAgilityResponse<out T>(string html, AgilityRestManager arm) where T : ResponseBase;
    }
}
