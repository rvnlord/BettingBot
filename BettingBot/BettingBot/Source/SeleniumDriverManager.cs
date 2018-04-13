using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using BettingBot.Common;
using BettingBot.Source.Clients.Selenium.Hintwise;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace BettingBot.Source
{
    public class SeleniumDriverManager
    {
        private static string _previousPage;

        private static readonly List<ChromeDriver> _drivers = new List<ChromeDriver>();
        protected ChromeDriver _driver;

        public WebDriverWait Wait;
        public string Url => _driver.Url;

        public void OpenOrReuseDriver(bool headlessMode = false, bool reuse = true)
        {
            if (_driver.IsClosed())
            {
                if (reuse && _drivers.Any() && _drivers.Last().IsOpen())
                    _driver = _drivers.Last();
                else
                {
                    var chromeService = ChromeDriverService.CreateDefaultService($@"{AppDomain.CurrentDomain.BaseDirectory}");
                    var chromeOptions = new ChromeOptions();
                    if (headlessMode)
                    {
                        chromeOptions.AddArguments(new List<string>
                        {
                            "--silent-launch",
                            "--no-startup-window",
                            "no-sandbox",
                            "headless"
                        });
                        chromeService.HideCommandPromptWindow = true;
                    }
                    
                    _driver = new ChromeDriver(chromeService, chromeOptions);
                    var size = new Size(1240, 720);
                    _driver.Manage().Window.Size = size;
                    _driver.Manage().Window.Position = PointUtils.CenteredWindowTopLeft(size).ToDrawingPoint();
                    
                    Wait = new WebDriverWait(_driver, new TimeSpan(0, 0, 10));
                    _drivers.Add(_driver);
                    _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                    _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
                }
            }
        }

        public void DisableWaitingForElements()
        {
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
        }

        public void EnableWaitingForElements()
        {
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
        }

        public void NavigateTo(string url)
        {
            _previousPage = _driver.Url;
            _driver.Navigate().GoToUrl(url);
        }

        public void NavigateTo(Uri url) => NavigateTo(url.ToString());

        public void NavigateToAndStopWaitingForUrlAfter(string url, int dontWaitAfter) //, Action actionBeforeLoaded = null
        {
            _previousPage = _driver.Url;
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(dontWaitAfter);
            try
            {
                _driver.Navigate().GoToUrl(url);
            }
            catch (WebDriverTimeoutException)
            { }

            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            Wait.Until(d => d.Url != _previousPage); // czeka na Url automatycznie
        }

        public void NavigateToAndStopWaitingForUrlAfter(Uri uri, int dontWaitAfter) => NavigateToAndStopWaitingForUrlAfter(uri.ToString(), dontWaitAfter);

        public void ClickAndWaitForUrl(IWebElement webElement)
        {
            _previousPage = _driver.Url;
            webElement.Click();
            Wait.Until(d => d.Url != _previousPage);
        }

        public void CloseDriver()
        {
            if (_driver?.SessionId != null)
            {
                _drivers.Remove(_driver);
                _driver.Quit();
                _driver = null;
            }
        }

        public static void CloseAllDrivers()
        {
            try
            {
                foreach (var d in _drivers)
                    if (d?.SessionId != null)
                        d.Quit();
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                _drivers.Clear();
            }
        }

        public ReadOnlyCollection<IWebElement> FindElementsByXPath(string xPath)
        {
            return _driver.FindElementsByXPath(xPath);
        }

        public IWebElement FindElementByXPath(string xPath)
        {
            return _driver.FindElementByXPath(xPath);
        }

        public IWebElement FindElementByName(string name)
        {
            return _driver.FindElementByName(name);
        }

        public HintwiseSeleniumDriverManager ToHsdm()
        {
            return (HintwiseSeleniumDriverManager) this;
        }
    }
}
