using System.Linq;
using BettingBot.Source.Clients.Selenium.Hintwise.Responses;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;

namespace BettingBot.Source.Clients.Selenium.Hintwise
{
    public class HintwiseClient : SeleniumClient, ISeleniumAuthenticable
    {
        private readonly HintwiseSeleniumDriverManager _hsdm;

        public HintwiseClient(string login, string password, bool headlessMode)
            : base(
                "https://hintwise.com/",
                TimeZoneKind.GreenwichStandardTime, // .GMTStandardTime ze zmianą czasu na letni/zimowy w wielkiej brytanii, ale wygląda na to, że na hintwise znów jest innas trefa, WTF?
                login, password, headlessMode)
        {
            _sdm = new HintwiseSeleniumDriverManager();
            _hsdm = _sdm.ToHsdm();
        }

        public TipsterAddressResponse TipsterAddress(string tipsterName)
        {
            return GetPublic("tipsters", sdm => new TipsterAddressResponse()
                .ReceiveInfoWith<TipsterAddressResponse>(Response_InformationSent)
                .Parse(sdm.ToHsdm(), tipsterName, _address));
        }

        public TipsterResponse Tipster(string tipsterAddress)
        {
            var relativeAddress = tipsterAddress.Remove(_address);
            return GetPublic(relativeAddress, sdm => new TipsterResponse()
                .ReceiveInfoWith<TipsterResponse>(Response_InformationSent)
                .Parse(sdm.ToHsdm()));
        }

        public BetsResponse Tips(TipsterResponse tipster, ExtendedTime fromDate)
        {
            var relativeAddress = tipster.Address.Remove(_address);
            return GetPrivate(relativeAddress, sdm => new BetsResponse()
                .ReceiveInfoWith<BetsResponse>(Response_InformationSent)
                .Parse(sdm.ToHsdm(), tipster, fromDate, _timeZone));
        }

        protected override T Get<T>(QueryType queryType, string action, NavigateToResponse<T> navigator)
        {
            OnInformationSending("Łączenie z Hintwise...");

            var url = (_address + action).EnsureSuffix("/");

            _hsdm.OpenOrReuseDriver(_headlessMode);

            _hsdm.NavigateTo(url); // przed sprawdzeniem loginu, bo inaczej przeglądarka nie ma adresu do sprawdzenia
            if (queryType == QueryType.Private)
                EnsureLogin(); // nawiguje z powrotem
            
            _hsdm.ClosePopups();

            return navigator(_hsdm);
        }
        
        public void EnsureLogin()
        {
            if (IsLogged()) return;
            Login();
            if (!IsLogged()) throw new HintwiseException("Nie można zalogować");
        }

        public void Login()
        {
            OnInformationSending("Logowanie...");

            var loginUrl = $"{_address}login";
            var urlFromBeforeLogin = _hsdm.Url;
            _hsdm.NavigateToAndStopWaitingForUrlAfter(loginUrl, 5);
            loginUrl = _hsdm.Url; // dokładny adres logowania, żeby póxniej sprawdzić czy adres się zmienił, jeśli zostawimy domyślny, np  z http a po zalogowaniu przekieruje na ten sam adres z https to sprawdzenie czy d.Url != _prevUrl od razu zwróci true i przejdzie dalej nie oczekując w Waicie

            var userNameField = _hsdm.FindElementByName("LoginForm[username]");
            var userPasswordField = _hsdm.FindElementByName("LoginForm[password]");
            var loginButton = _hsdm.FindElementByXPath("//input[@value='Login']");

            userNameField.SendKeys(_login);
            userPasswordField.SendKeys(_password);

            _hsdm.ClosePopups();

            loginButton.Click();

            var loginIncorrect = false;
            bool isLoginIncorrect()
            {
                _hsdm.WithoutWaitingForElements(() =>
                {
                    loginIncorrect = _hsdm.FindElementsByXPath(".//div[@class='errorMessage']")?.Select(div => div.Text).Any(text => text.EqIgnoreCase("Password incorrect!")) == true;
                }); // jeśli jest błąd to element error jest dostępny od razu, jeśli nie ma to nie możemy czekac n sekund na timeout, tylko przejść do warunku Correct od razu i czekać tylko na nowy Url
                
                return loginIncorrect;
            }
            bool isLoginCorrect() => _hsdm.Url != loginUrl;
            
            _hsdm.Wait.Until(d => isLoginIncorrect() || isLoginCorrect());

            if (loginIncorrect)
                throw new HintwiseException("Niepoprawne dane logowania");

            _hsdm.NavigateTo(urlFromBeforeLogin);

            OnInformationSending("Zalogowano");
        }

        public bool IsLogged()
        {
            var isLogged = false;
            _sdm.WithoutWaitingForElements(() => isLogged = !_sdm.FindElementsByXPath(".//a").Any(e => e.Text.ContainsAny("login")));
            return isLogged;
        }
    }
}
