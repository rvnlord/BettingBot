using System;
using System.Threading;
using BettingBot.Common.UtilityClasses;
using RestSharp;
using WebSocketSharp;

namespace BettingBot.Source.Clients.Api
{
    public abstract class ApiClient : Client
    {
        protected static readonly object _lock = new object();
        protected TimeSpan _rateLimit;
        protected WebSocket _socket;

        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public DateTime LastApiCallTimestamp { get; set; }

        protected ApiClient(string address, TimeZoneKind timeZone, string apiKey, string apiSecret, TimeSpan? rateLimit = null, int version = 2) 
            : base(address, timeZone)
        {
            _rateLimit = rateLimit ?? TimeSpan.FromSeconds(5);
            ApiKey = apiKey;
            ApiSecret = apiSecret;
        }
        
        protected void RateLimit()
        {
            lock (_lock)
            {
                var elapsedSpan = DateTime.Now - LastApiCallTimestamp;
                if (elapsedSpan < _rateLimit)
                    Thread.Sleep(_rateLimit - elapsedSpan);
                LastApiCallTimestamp = DateTime.Now;
            }
        }

        protected abstract T Query<T>(QueryType queryType, Method method, string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase;

        private T QueryPrivate<T>(Method method, string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase
        {
            return Query(QueryType.Private, method, action, parameters, deserializer, flags);
        }

        private T QueryPublic<T>(Method method, string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase
        {
            return Query(QueryType.Public, method, action, parameters, deserializer, flags);
        }

        protected T GetPrivate<T>(string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase
        {
            return QueryPrivate(Method.GET, action, parameters, deserializer, flags);
        }

        protected T PostPrivate<T>(string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase
        {
            return QueryPrivate(Method.POST, action, parameters, deserializer, flags);
        }

        protected T GetPublic<T>(string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase
        {
            return QueryPublic(Method.GET, action, parameters, deserializer, flags);
        }

        protected T PostPublic<T>(string action, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags flags = null) where T : ResponseBase
        {
            return QueryPublic(Method.POST, action, parameters, deserializer, flags);
        }

        protected abstract T QuerySocket<T>(QueryType queryType, string action, string actionEvent, string subActionEvent, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags apiFlags = null) where T : ResponseBase;

        protected T QuerySocketPrivate<T>(string action, string actionEvent, string subActionEvent, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags apiFlags = null) where T : ResponseBase
        {
            return QuerySocket(QueryType.Private, action, actionEvent, subActionEvent, parameters, deserializer, apiFlags);
        }

        protected T QuerySocketPublic<T>(string action, string actionEvent, string subActionEvent, Parameters parameters = null, DeserializeResponse<T> deserializer = null, ApiFlags apiFlags = null) where T : ResponseBase
        {
            return QuerySocket(QueryType.Public, action, actionEvent, subActionEvent, parameters, deserializer, apiFlags);
        }

        protected void InitSocket()
        {
            _socket = new WebSocket(_address);
            _socket.OnOpen += Socket_Open;
            _socket.OnError += Socket_Error;
            _socket.OnClose += Socket_Closed;
            _socket.OnMessage += Socket_Message;
            _socket.Connect();
        }

        protected abstract void Socket_Message(object sender, MessageEventArgs e);

        protected virtual void Socket_Closed(object sender, CloseEventArgs e)
        {
            if (e.Code != 1005) // 1005 = poprawny powód zamknięcia gniazda
                return;
            InitSocket();
        }

        protected virtual void Socket_Error(object sender, ErrorEventArgs e)
        {
            throw e.Exception;
        }

        protected virtual void Socket_Open(object sender, EventArgs eventArgs) { }
    }
}
