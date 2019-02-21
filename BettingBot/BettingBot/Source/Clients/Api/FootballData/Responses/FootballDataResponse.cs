using System;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BettingBot.Source.Clients.Api.FootballData.Responses
{
    public class FootballDataResponse : ResponseBase
    {
        public string Error { get; set; }
        public ExtendedTime Time { get; set; }

        public FootballDataResponse RawParse(string json)
        {
            try
            {
                var jtRawGenericResponse = json.ToJToken(); // tylko dla sprawdzenia czy parser zwróci błąd
                if (jtRawGenericResponse is JObject joRawGenericResponse)
                    Error = joRawGenericResponse.VorN("error")?.ToString();
                
                Time = DateTime.UtcNow.ToExtendedTime();
            }
            catch (JsonReaderException) { Error = "Football-Data APi zwróciło dane w niepoprawnym formacie (innym niż json)"; }
            catch (JsonSerializationException) { } 
            return this;
        }

        public void HandleErrors(string json)
        {
            RawParse(json);
            if (Error != null)
                throw new FootballDataException(Error);
        }
    }
}
