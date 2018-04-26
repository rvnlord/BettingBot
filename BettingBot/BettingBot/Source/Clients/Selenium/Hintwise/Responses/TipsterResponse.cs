using BettingBot.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Clients.Selenium.Hintwise.Responses
{
    public class TipsterResponse : HintwiseResponse
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public string Address { get; set; }

        public TipsterResponse Parse(HintwiseSeleniumDriverManager sdm)
        {
            HandleErrors(sdm);

            OnInformationSending("Określanie danych Tipstera...");

            var originalAddress = sdm.Url;
            var tipsterName = sdm.FindElementByXPath(".//div[@id='content']/div[1]/div[1]/div[1]/div[1]/h4/b").Text;
            var tipsterDomain = originalAddress.UrlToDomain();

            OnInformationSending("Ustalono dane Tipstera");

            Name = tipsterName;
            Domain = tipsterDomain;
            Address = originalAddress;
            return this;
        }

        public DbTipster ToDbTipster()
        {
            return TipsterConverter.ToDbTipster(this);
        }
    }
}