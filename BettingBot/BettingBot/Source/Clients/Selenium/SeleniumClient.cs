using BettingBot.Source.Common.UtilityClasses;

namespace BettingBot.Source.Clients.Selenium
{
    public abstract class SeleniumClient : Client
    {
        protected string _login;
        protected string _password;
        protected bool _headlessMode;
        protected SeleniumDriverManager _sdm;

        protected SeleniumClient(string address, TimeZoneKind timeZone, string login, string password, bool headlessMode) : base(address, timeZone)
        {
            _login = login;
            _password = password;
            _headlessMode = headlessMode;
            _sdm = new SeleniumDriverManager();
        }

        protected abstract T Get<T>(QueryType queryType, string action, NavigateToResponse<T> navigator) where T : ResponseBase;

        protected virtual T GetPrivate<T>(string action, NavigateToResponse<T> navigator) where T : ResponseBase
        {
            return Get(QueryType.Private, action, navigator);
        }

        protected virtual T GetPublic<T>(string action, NavigateToResponse<T> navigator) where T : ResponseBase
        {
            return Get(QueryType.Public, action, navigator);
        }

        public delegate T NavigateToResponse<out T>(SeleniumDriverManager sdm) where T : ResponseBase;
    }
}
