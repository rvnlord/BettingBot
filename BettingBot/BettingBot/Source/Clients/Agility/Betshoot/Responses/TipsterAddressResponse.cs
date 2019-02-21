using System;
using System.Linq;
using BettingBot.Source.Common;
using MoreLinq;

namespace BettingBot.Source.Clients.Agility.Betshoot.Responses
{
    public class TipsterAddressResponse : BetshootResponse
    {
        public string Address { get; set; }
        public string RelativeAddress { get; set; }

        public TipsterAddressResponse Parse(string html, string tipsterName, string address)
        {
            HandleErrors(html);

            OnInformationSending("Określanie adresu Tipstera...");

            var aTipsters = html.HtmlRoot().Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("tipsterprf"))
                .Select(n => n.Descendants("a").Single()).ToArray();
            var titleHrefTipsters = aTipsters
                .Select(a => new { title = a.GetAttributeValue("title", ""), href = a.GetAttributeValue("href", "") })
                .ToArray();

            var tipsterAddress = titleHrefTipsters.DistinctBy(th => th.title)
                .SingleOrDefault(th => th.title.Equals(tipsterName, StringComparison.OrdinalIgnoreCase))?.href;
            if (tipsterAddress == null)
                throw new BetshootException("Podany Tipster nie istnieje na stronie");

            OnInformationSending("Ustalono stronę Tipstera");

            return new TipsterAddressResponse
            {
                Address = tipsterAddress,
                RelativeAddress = tipsterAddress.Remove(address)
            };
        }
    }
}
