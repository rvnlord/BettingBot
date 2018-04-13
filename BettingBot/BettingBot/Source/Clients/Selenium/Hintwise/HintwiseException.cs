using System;
using System.Runtime.Serialization;
using BettingBot.Source.Clients.Selenium.Hintwise.Responses;

namespace BettingBot.Source.Clients.Selenium.Hintwise
{
    [Serializable]
    public class HintwiseException : ClientException
    {
        public HintwiseResponse Response { get; }

        public HintwiseException(string message, HintwiseResponse response) : base(CreateMessage(nameof(HintwiseException), message))
        {
            Response = response;
        }

        public HintwiseException()
        {
        }

        public HintwiseException(string message) : base(CreateMessage(nameof(HintwiseException), message))
        {
        }

        public HintwiseException(string message, Exception innerException) : base(CreateMessage(nameof(HintwiseException), message), innerException)
        {
        }

        protected HintwiseException(SerializationInfo info, StreamingContext context) : base(info, context)
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