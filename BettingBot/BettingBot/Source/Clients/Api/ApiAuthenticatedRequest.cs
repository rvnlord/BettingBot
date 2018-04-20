using RestSharp;

namespace BettingBot.Source.Clients.Api
{
    public abstract class AuthenticatedRequest : RestRequest
    {
        protected static long _lastNonce;

        protected AuthenticatedRequest(Method method) : base(method) { }
    }
}
