using System;
using System.Runtime.Serialization;
using BettingBot.Source.Clients.Agility.Betshoot.Responses;

namespace BettingBot.Source.Clients.Agility.Betshoot
{
    [Serializable]
    public class BetshootException : ClientException
    {
        public BetshootResponse Response { get; }

        public BetshootException(string message, BetshootResponse response) : base(CreateMessage(nameof(BetshootException), message))
        {
            Response = response;
        }

        public BetshootException()
        {
        }

        public BetshootException(string message) : base(CreateMessage(nameof(BetshootException), message))
        {
        }

        public BetshootException(string message, Exception innerException) : base(CreateMessage(nameof(BetshootException), message), innerException)
        {
        }

        protected BetshootException(SerializationInfo info, StreamingContext context) : base(info, context)
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