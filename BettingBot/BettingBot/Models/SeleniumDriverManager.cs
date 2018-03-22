using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using BettingBot.Common;
using OpenQA.Selenium.PhantomJS;

namespace BettingBot.Models
{
    public class SeleniumDriverManager
    {
        public ChromeDriver Driver { get; set; }
        private static List<ChromeDriver> Drivers { get; } = new List<ChromeDriver>();
        private static WebDriverWait Wait { get; set; }
        private static string PreviousPage { get; set; }

        public SeleniumDriverManager()
        {
        }

        public void OpenOrReuseDriver(bool reuse = true, bool headlessMode = false)
        {
            if (Driver.IsClosed())
            {
                if (reuse && Drivers.Any() && Drivers.Last().IsOpen())
                    Driver = Drivers.Last();
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
                    
                    Driver = new ChromeDriver(chromeService, chromeOptions);
                    var size = new Size(1240, 720);
                    Driver.Manage().Window.Size = size;
                    Driver.Manage().Window.Position = PointUtils.CenteredWindowTopLeft(size).ToDrawingPoint();
                    Driver.EnableImplicitWait();
                    Wait = new WebDriverWait(Driver, new TimeSpan(0, 0, 10));
                    Drivers.Add(Driver);
                }
            }
        }

        public void NavigateTo(string url)
        {
            Driver.Navigate().GoToUrl(url);
        }

        public void NavigateAndWaitForUrl(string url, int forceCancelLoadAfter = 60) //, Action actionBeforeLoaded = null
        {
            PreviousPage = Driver.Url;
            Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(forceCancelLoadAfter);
            try
            {
                Driver.Navigate().GoToUrl(url);
            }
            catch (WebDriverTimeoutException)
            { }

            Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
            //Wait.Until(d => d.Url != PreviousPage);
        }

        public void NavigateAndWaitForUrl(Uri url) //, Action actionBeforeLoaded = null
        {
            NavigateAndWaitForUrl(url.ToString());
        }

        public void ClickAndWaitForUrl(IWebElement webElement)
        {
            PreviousPage = Driver.Url;
            webElement.Click();
            Wait.Until(d => d.Url != PreviousPage);
        }

        public void CloseDriver()
        {
            if (Driver?.SessionId != null)
            {
                Drivers.Remove(Driver);
                Driver.Quit();
                Driver = null;
            }
        }

        public static void CloseAllDrivers()
        {
            try
            {
                foreach (var d in Drivers)
                    if (d?.SessionId != null)
                        d.Quit();
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                Drivers.Clear();
            }
        }
    }
}
