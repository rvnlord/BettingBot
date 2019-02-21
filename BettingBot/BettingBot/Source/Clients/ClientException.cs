using System;
using System.Runtime.Serialization;
using BettingBot.Source.Common;

namespace BettingBot.Source.Clients
{
    [Serializable]
    public class ClientException : Exception
    {
        public ClientException() { }
        public ClientException(string message) : base(message) { }
        public ClientException(string message, Exception innerException) : base(message, innerException) { }
        protected ClientException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        protected static string CreateMessage(string exClassName, string originalMessage)
        {
            return $"({exClassName.BeforeFirst("Exception")}) {originalMessage}";
        }
    }
}
