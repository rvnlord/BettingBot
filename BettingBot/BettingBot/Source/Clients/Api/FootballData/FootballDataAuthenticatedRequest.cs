using System;
using BettingBot.Source.Common;
using RestSharp;

namespace BettingBot.Source.Clients.Api.FootballData
{
    public class FootballDataAuthenticatedRequest : AuthenticatedRequest
    {
        public FootballDataAuthenticatedRequest(Method method, string apiKey) : base(method)
        {
            var nonce = DateTime.UtcNow.ToUnixTimestamp().ToLong();
            if (nonce <= _lastNonce) nonce = ++_lastNonce;
            AddHeader("X-Auth-Token", apiKey);
            _lastNonce = nonce;
        }
    }
}
