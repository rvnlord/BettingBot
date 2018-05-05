using BettingBot.Common;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Source.Clients
{
    public abstract class Client : InformationSender
    {
        protected string _address;
        protected readonly TimeZoneKind _timeZone;

        protected string Address
        {
            set
            {
                if (!value.StartsWithAny("http://", "https://"))
                {
                    if (!value.StartsWith("www"))
                        value = "www" + value;
                    value = "http://" + value;
                }

                _address = value;
            }
            get => _address;
        }

        protected Client(string address, TimeZoneKind timeZone)
        {
            Address = address;
            _timeZone = timeZone;
        }
        
        public delegate T DeserializeResponse<out T>(string content) where T : ResponseBase;

        protected virtual void Response_InformationSent(object sender, InformationSentEventArgs e)
        {
            OnInformationSending(e.Information);
        }
    }

    public enum QueryType
    {
        Private,
        Public
    }
}