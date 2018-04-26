using System;
using BettingBot.Common;
using OpenQA.Selenium;

namespace BettingBot.Source.Clients.Selenium.Hintwise.Responses
{
    public class TipsterAddressResponse : HintwiseResponse
    {
        public string Address { get; set; }
        public string RelativeAddress { get; set; }

        public TipsterAddressResponse Parse(HintwiseSeleniumDriverManager sdm, string tipsterName, string baseAddress)
        {
            HandleErrors(sdm);

            OnInformationSending("Określanie adresu strony Tipstera...");
            IWebElement getDivTipsters() => sdm.FindElementByXPath(".//div[@id='blog-index']"); // znajdź tabelę tipsterów

            var originalAddress = sdm.Url;
            var divTipsters = getDivTipsters();
            var lastPageQueries = sdm.PagerLastPageQueries(divTipsters);
            var pages = sdm.PagerLastPage(lastPageQueries);
            string foundTipsterLink = null;
            string foundTipsterName = null; // staleel exception bo divtipsters jest z poprzedniej strony

            for (var currPage = 1; currPage <= pages; currPage++)
            {
                sdm.PagerNavigateToCurrentPage(currPage, lastPageQueries, originalAddress);
                divTipsters = getDivTipsters(); // ta sama tabela, ten sam element (xpath), ale selenium będzie go traktował jako z innej strony ponieważ zmieniony został querystring urla
                var trTipsterRows = divTipsters.FindElements(By.XPath(".//table[@class='items']/tbody/tr"));

                foreach (var trTipsterRow in trTipsterRows)
                {
                    var tdTipsterRowCells = trTipsterRow.FindElements(By.TagName("td"));
                    var aTipsterLink = tdTipsterRowCells[0].FindElement(By.TagName("a"));
                    foundTipsterLink = aTipsterLink.GetAttribute("href");
                    foundTipsterName = aTipsterLink.Text.RemoveHTMLSymbols().Trim();

                    if (foundTipsterName.EqIgnoreCase(tipsterName))
                        break;
                }

                if (foundTipsterName.EqIgnoreCase(tipsterName))
                    break;
            }

            OnInformationSending("Ustalono adres strony Tipstera");

            if (!foundTipsterName.EqIgnoreCase(tipsterName))
                throw new HintwiseException("Podany Tipster nie istnieje na stronie");

            Address = foundTipsterLink;
            RelativeAddress = foundTipsterLink.Remove(baseAddress);
            return this;
        }
    }
}
