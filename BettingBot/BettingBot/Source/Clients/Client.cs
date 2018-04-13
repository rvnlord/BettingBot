using BettingBot.Common;

namespace BettingBot.Source.Clients
{
    public abstract class Client : InformationSender
    {
        protected string _address;

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