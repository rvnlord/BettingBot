using BettingBot.Common;
using BettingBot.Source.Clients.Agility.Betshoot;
using RestSharp;

namespace BettingBot.Source.Clients.Agility
{
    public abstract class AgilityClient : Client
    {
        protected AgilityClient()
        {

        }

        protected virtual T Get<T>(string action, DeserializeResponse<T> deserializer) where T : ResponseBase
        {
            var url = _address + action;
            if (!url.EndsWith("/"))
                url += "/";
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var rawResponse = new RestClient(url).Execute(request);
            if (rawResponse == null || rawResponse.ContentLength == 0)
                throw new BetshootException("Serwer zwrócił pustą wiadomość");
            return deserializer(rawResponse.Content);
        }
    }
}
