using System;
using System.Runtime.Serialization;
using BettingBot.Source.Clients.Api.FootballData.Responses;

namespace BettingBot.Source.Clients.Api.FootballData
{
    [Serializable]
    public class FootballDataException : ClientException
    {
        public FootballDataResponse Response { get; }

        public FootballDataException(string message, FootballDataResponse response) : base(CreateMessage(nameof(FootballDataException), message))
        {
            Response = response;
        }

        public FootballDataException()
        {
        }

        public FootballDataException(string message) : base(CreateMessage(nameof(FootballDataException), message))
        {
        }

        public FootballDataException(string message, Exception innerException) : base(CreateMessage(nameof(FootballDataException), message), innerException)
        {
        }

        protected FootballDataException(SerializationInfo info, StreamingContext context) : base(info, context)
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