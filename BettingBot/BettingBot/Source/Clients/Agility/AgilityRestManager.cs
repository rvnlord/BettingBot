using System;
using RestSharp;

namespace BettingBot.Source.Clients.Agility
{
    public class AgilityRestManager
    {
        public string GetHtml(string url)
        {
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var rawResponse = new RestClient(url).Execute(request);
            if (string.IsNullOrEmpty(rawResponse.Content))
                throw new Exception("Serwer zwrócił pustą wiadomość");
            return rawResponse.Content;
        }
    }
}
