using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source.Clients.Api.FootballData.Responses;
using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using WebSocketSharp;

namespace BettingBot.Source.Clients.Api.FootballData
{
    public class FootballDataClient : ApiClient
    {
        private readonly string _addressWithVersion;

        public FootballDataClient(string apiKey, TimeSpan? rateLimit = null, int version = 1) 
            : base(
                "http://api.football-data.org/", 
                TimeZoneKind.UTC,
                apiKey, 
                null, 
                rateLimit ?? TimeSpan.FromMilliseconds(1200))
        {
            _addressWithVersion = $"{_address}v{version}/";
        }

        public CompetitionsResponse Competitions(int? year = null)
        {
            var parameters = new Dictionary<string, string>();
            parameters.AddIfNotNull("season", year?.ToString());

            return GetPrivate("competitions", parameters.ToParameters(), new CompetitionsResponse().Parse);
        }

        public TeamsResponse Teams(int competitionId)
        {
            return GetPrivate($"competitions/{competitionId}/teams", null, new TeamsResponse().Parse);
        }

        public FixturesResponse Fixtures(int competitionId, int? periodInDays = null, FixturesPeriodType? periodType = null, int? matchDay = null)
        {
            var parameters = new Dictionary<string, string>();
            parameters.AddIf(periodInDays != null && periodType != null, "timeFrame", $"{periodType?.GetDescription()}{periodInDays}");
            parameters.AddIfNotNull("matchday", matchDay?.ToString());

            return GetPrivate($"competitions/{competitionId}/fixtures", parameters.ToParameters(), new FixturesResponse().Parse);
        }

        public FixturesResponse Fixtures()
        {
            return GetPrivate("fixtures", null, new FixturesResponse().Parse);
        }

        public FixturesResponse Fixtures(int periodInDays, FixturesPeriodType periodType)
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("timeFrame", $"{periodType.GetDescription()}{periodInDays}");

            return GetPrivate("fixtures", parameters.ToParameters(), new FixturesResponse().Parse);
        }

        public FixturesResponse Fixtures(ExtendedTime timeFrameStart, ExtendedTime timeFrameEnd)
        {
            var parameters = new Dictionary<string, string>();
            parameters.Add("timeFrameStart", $"{timeFrameStart.ToUTC().Rfc1123:yyyy-MM-dd}");
            parameters.Add("timeFrameEnd", $"{timeFrameEnd.ToUTC().Rfc1123:yyyy-MM-dd}");

            return GetPrivate("fixtures", parameters.ToParameters(), new FixturesResponse().Parse);
        }

        protected override T Query<T>(QueryType queryType, Method method, string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null)
        {
            RateLimit();
            var omitVersion = flags?.V(ApiFlagType.OmitVersion) == true;
            var uri = $"{(omitVersion ? _address : _addressWithVersion)}{action}";

            if (!uri.EndsWith("/")) uri += "/";
            var request = queryType == QueryType.Private
                ? new FootballDataAuthenticatedRequest(method, ApiKey)
                : new RestRequest(method);

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("X-Response-Control", "minified");
            parameters?.ForEach(p => request.AddParameter(p.Name, p.Value)); // querystring dla get

            var catchNum = 0;
            const int maxCatchNum = 3;
            FootballDataException fdEx = null;
            while (catchNum < maxCatchNum)
            {
                try
                {
                    var rawResponse = new RestClient(uri).Execute(request);
                    if (!rawResponse.StatusCode.ToInt().Between(200, 299) && !rawResponse.ContentType.Split(";").Any(m => m.EqIgnoreCase("application/json"))) // jeśli kod wskazuje błąd i json nie opisuje tego błędu to zwróć ogólny
                        throw new FootballDataException($"{rawResponse.StatusCode.ToInt()}: {rawResponse.StatusDescription}");
                    if (string.IsNullOrEmpty(rawResponse.Content))
                        throw new FootballDataException("Serwer zwrócił pustą zawartość, prawdopodobnie ochrona przed spamem");

                    var response = deserializer == null
                        ? JsonConvert.DeserializeObject<T>(rawResponse.Content)
                        : deserializer(rawResponse.Content);
                    return response;
                }
                catch (FootballDataException ex)
                {
                    catchNum++;
                    fdEx = ex;

                    OnInformationSending($"Zapytanie zwrociło błąd, próba {catchNum} z {maxCatchNum}");
                    Thread.Sleep(5000);
                }
            }

            throw fdEx;
        }

        protected override T QuerySocket<T>(QueryType queryType, string action, string actionEvent, string subActionEvent, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags apiFlags = null)
        {
            throw new FootballDataException("Brak wsparcia dla gniazd");
        }

        protected override void Socket_Message(object sender, MessageEventArgs e)
        {
            throw new FootballDataException("Brak wsparcia dla gniazd");
        }
    }

    public enum FixturesPeriodType
    {
        [Description("p")] Past,
        [Description("n")] Future
    }
}
