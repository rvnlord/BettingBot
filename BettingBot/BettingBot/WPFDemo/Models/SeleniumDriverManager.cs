using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using WPFDemo.Common;

namespace WPFDemo.Models
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

        public void OpenOrReuseDriver(bool reuse = true)
        {
            if (Driver.IsClosed())
            {
                if (reuse && Drivers.Any() && Drivers.Last().IsOpen())
                    Driver = Drivers.Last();
                else
                {
                    Driver = new ChromeDriver($@"{AppDomain.CurrentDomain.BaseDirectory}");
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
            PreviousPage = Driver.Url;;
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
                var t = ex;
            }
            finally
            {
                Drivers.Clear();
            }
        }
    }
}
