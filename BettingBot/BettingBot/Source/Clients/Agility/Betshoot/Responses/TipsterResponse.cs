using System;
using System.Linq;
using BettingBot.Source.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using DomainParser.Library;

namespace BettingBot.Source.Clients.Agility.Betshoot.Responses
{
    public class TipsterResponse : BetshootResponse
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public string Address { get; set; }

        public TipsterResponse Parse(string html, string url)
        {
            HandleErrors(html);
            
            OnInformationSending("Określanie danych Tipstera...");
            var tipsterName = html.HtmlRoot().Descendants().Single(n => n.GetAttributeValue("class", "").Equals("top-info")).Descendants("h1").Single().InnerText;

            var domainName = DomainName.TryParse(new Uri(url).Host, out DomainName completeDomain) ? completeDomain.SLD : "";
            OnInformationSending("Ustalono dane Tipstera");

            return new TipsterResponse
            {
                Name = tipsterName,
                Domain = domainName,
                Address = url
            };
        }

        public DbTipster ToDbTipster()
        {
            return TipsterConverter.ToDbTipster(this);
        }
    }
}
