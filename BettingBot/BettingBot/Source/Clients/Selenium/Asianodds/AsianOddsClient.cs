using System.Linq;
using BettingBot.Source.Clients.Selenium.Asianodds.Requests;
using BettingBot.Source.Clients.Selenium.Asianodds.Responses;
using BettingBot.Source.Clients.Selenium.Hintwise;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using BettingBot.Source.ViewModels;
using OpenQA.Selenium;

namespace BettingBot.Source.Clients.Selenium.Asianodds
{
    public class AsianoddsClient : SeleniumClient, ISeleniumAuthenticable
    {
        private readonly AsianoddsSeleniumDriverManager _aosdm;

        public AsianoddsClient(string login, string password, bool headlessMode)
            : base(
                "https://www.asianodds88.com/",
                TimeZoneKind.CentralEuropeStandardTime,
                login, password, headlessMode)
        {
            _sdm = new AsianoddsSeleniumDriverManager();
            _aosdm = _sdm.ToAosdm();
        }

        public BetResponse MakeBet(BetRequest betRequest)
        {
            return GetPrivate(sdm => new BetResponse()
                .ReceiveInfoWith<BetResponse>(Response_InformationSent)
                .Parse(sdm.ToAosdm(), betRequest, _timeZone, this));
        }

        public BetsResponse HistoricalBets(ExtendedTime fromDate)
        {
            return GetPrivate(sdm => new BetsResponse()
                .ReceiveInfoWith<BetsResponse>(Response_InformationSent)
                .Parse(sdm.ToAosdm(), fromDate, _timeZone));
        }

        protected T GetPrivate<T>(NavigateToResponse<T> navigator) where T : ResponseBase
        {
            return Get(QueryType.Private, null, navigator);
        }

        protected override T Get<T>(QueryType queryType, string action, NavigateToResponse<T> navigator)
        {
            OnInformationSending("Łączenie z Asianodds...");

            if (queryType == QueryType.Public)
                throw new AsianoddsException("Asianodds nie wspiera publicznych zapytań");
            if (action != null)
                throw new AsianoddsException("Podanie argumentu 'action' nie ma wpływu na zapytanie");
            
            _aosdm.OpenOrReuseDriver(_headlessMode);
            
            if (queryType == QueryType.Private)
                EnsureLogin();

            return navigator(_aosdm);
        }

        public void EnsureLogin()
        {
            if (IsLogged()) return;
            Login();
            if (!IsLogged()) throw new AsianoddsException("Nie można zalogować");
        }

        public void Login()
        {
            OnInformationSending("Logowanie do Asianodds...");

            var loginUrl = _address + "/Login.aspx";
            _aosdm.NavigateTo(loginUrl);
            loginUrl = _aosdm.Url; // dokładny adres logowania,

            void login()
            {
                var txtUsername = _aosdm.FindElementByXPath("//*[@id='ctl00_txtUserID']");
                var txtPassword = _aosdm.FindElementByXPath("//*[@id='ctl00_txtPass']");
                var btnLogin = _aosdm.FindElementByXPath("//*[@id='btnDoLogin']");

                txtUsername.SendKeys(_login);
                txtPassword.SendKeys(_password);
                btnLogin.Click();
            }

            var loginIncorrect = false;
            bool isLoginIncorrect()
            {
                _aosdm.WithoutWaitingForElements(() => // jeśli jest błąd to element error jest dostępny od razu, jeśli nie ma to nie możemy czekac n sekund na timeout, tylko przejść do warunku Correct od razu i czekać tylko na nowy Url
                    loginIncorrect = _aosdm.FindElementsByXPath("//*[@id='lblInfo']")?.Select(div => div.Text).Any(text => text.Trim()
                        .EqIgnoreCase("Invalid userid or password. Please check your login credentials and try again")) == true);
                return loginIncorrect;
            }
            bool isLoginCorrect()
            {
                var loginCorrect = false;
                _aosdm.WithoutWaitingForElements(() => loginCorrect = _aosdm.Url != loginUrl
                    && _aosdm.FindElementsByXPath("//*[@id='selAdvSearchSportType']")?.SingleOrDefault()?.Displayed == true);
                return loginCorrect;
            }

            bool waitForLoginTimedOut;
            do
            {
                try
                {
                    login();
                    _aosdm.Wait.Until(d => isLoginIncorrect() || isLoginCorrect());
                    waitForLoginTimedOut = false;
                }
                catch (WebDriverTimeoutException)
                {
                    waitForLoginTimedOut = true;
                }
            } while (waitForLoginTimedOut);
            
            if (loginIncorrect)
                throw new AsianoddsException("Niepoprawne dane logowania");
            
            OnInformationSending("Zalogowano do Asianodds");
        }

        public bool IsLogged()
        {
            _aosdm.DisableWaitingForElements();

            var isDivAccountSummaryPresent = _aosdm.FindElementsByXPath("//*[@id='divAccountSummaryHeader']")?.Any() == true;
            var isDivKickoutDialogDisplayed = _aosdm.FindElementsByXPath("//*[@id='kickoutDialog']")?.Any(div => div.Displayed) == true;
            var xPathSelAdvSearchSportType = "//*[@id='selAdvSearchSportType']";
            var IsDdlDisciplineTypeFilled = _aosdm.FindElementsByXPath(xPathSelAdvSearchSportType)?.SingleOrDefault()?.FindElements(By.TagName("option"))?.Any() == true;          
            var isDdlDisciplineTypeDisplayed = _aosdm.FindElementsByXPath(xPathSelAdvSearchSportType)?.SingleOrDefault()?.Displayed == true;

            var isLogged = isDivAccountSummaryPresent && !isDivKickoutDialogDisplayed && IsDdlDisciplineTypeFilled && isDdlDisciplineTypeDisplayed;

            _aosdm.EnableWaitingForElements();
            return isLogged;
        }
    }
}
