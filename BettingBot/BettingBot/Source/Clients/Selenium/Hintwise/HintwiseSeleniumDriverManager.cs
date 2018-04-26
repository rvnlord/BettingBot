using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using OpenQA.Selenium;

namespace BettingBot.Source.Clients.Selenium.Hintwise
{
    public class HintwiseSeleniumDriverManager : SeleniumDriverManager
    {
        public Dictionary<string, string> PagerLastPageQueries(IWebElement divGridView)
        {
            var url = new Uri(Url);
            DisableWaitingForElements();
            var aLastPage = divGridView.FindElements(By.XPath(".//li[@class='last'][last()]//a")).SingleOrDefault(); // znajdź element z linkiem do ostatniej strony
            EnableWaitingForElements();

            Dictionary<string, string> queries = null;
            if (aLastPage != null)
            {
                var lastPageRelativeLink = aLastPage.GetAttribute("href").RemoveMany("https://", "www.", "hintwise.com");
                var lastPageUri = new Uri($"{url.Scheme}://{url.Host}{lastPageRelativeLink}");
                queries = lastPageUri.Query.QueryStringToDictionary();
            }
            return queries;
        }

        public int PagerLastPage(Dictionary<string, string> lastPageQueries)
        {
            if (lastPageQueries == null)
                return 1;
            return lastPageQueries.Single(kvp => kvp.Key.EndsWith("page")).Value.ToInt();
        }

        public void PagerNavigateToCurrentPage(int currPage, Dictionary<string, string> lastPageQueries, string addressBeforePaging)
        {
            if (currPage > 1) // to również 'pages > 1' i 'lastPageQueries != null'
            {
                var nextPageQueries = lastPageQueries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var pageKey = lastPageQueries.Keys.Single(k => k.EndsWith("page"));
                nextPageQueries[pageKey] = currPage.ToString();
                var uriBuilder = new UriBuilder(addressBeforePaging) { Query = nextPageQueries.ToQueryString() };

                NavigateToAndStopWaitingForUrlAfter(uriBuilder.Uri, 20);
            }
        }

        public void ClosePopups(bool wait = false)
        {
            if (!wait) DisableWaitingForElements();
            var closeButtons = FindElementsByXPath("//div[contains(text(), '[Close]') or contains(text(), '[Zamknij]')]").ToList();
            if (!wait) EnableWaitingForElements();
            closeButtons.ForEach(cb => cb.Click());
        }
    }
}
