using System;
using System.Runtime.Serialization;
using BettingBot.Source.Clients.Selenium.Asianodds.Responses;

namespace BettingBot.Source.Clients.Selenium.Asianodds
{
    [Serializable]
    public class AsianoddsException : ClientException
    {
        public AsianoddsResponse Response { get; }

        public AsianoddsException(string message, AsianoddsResponse response) : base(CreateMessage(nameof(AsianoddsException), message))
        {
            Response = response;
        }

        public AsianoddsException()
        {
        }

        public AsianoddsException(string message) : base(CreateMessage(nameof(AsianoddsException), message))
        {
        }

        public AsianoddsException(string message, Exception innerException) : base(CreateMessage(nameof(AsianoddsException), message), innerException)
        {
        }

        protected AsianoddsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("Response", Response);
            base.GetObjectData(info, context);
        }
    }
}