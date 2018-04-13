using BettingBot.Common;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using BetshootTipsterResponse = BettingBot.Source.Clients.Agility.Betshoot.Responses.TipsterResponse;
using HintwiseTipsterResponse = BettingBot.Source.Clients.Selenium.Hintwise.Responses.TipsterResponse;

namespace BettingBot.Source.Converters
{
    public static class TipsterConverter
    {
        public static DbTipster ToDbTipster(BetshootTipsterResponse tipster)
        {
            return new DbTipster
            {
                Name = tipster.Name,
                Link = tipster.Address
            };
        }


        public static DbTipster ToDbTipster(HintwiseTipsterResponse tipster)
        {
            return new DbTipster
            {
                Name = tipster.Name,
                Link = tipster.Address
            };
        }

        public static string TipsterDomainTypeToString(DomainType tipsterDomainType)
        {
            return tipsterDomainType == DomainType.Custom 
                ? "Pełny Adres" : tipsterDomainType.ConvertToString();
        }

        public static BetshootTipsterResponse ToBetshootTipsterResponse(DbTipster dbTipster)
        {
            return new BetshootTipsterResponse
            {
                Name = dbTipster.Name,
                Address = dbTipster.Link,
                Domain = dbTipster.Link.UrlToDomain()
            };
        }

        public static HintwiseTipsterResponse ToHintwiseTipsterResponse(DbTipster dbTipster)
        {
            return new HintwiseTipsterResponse
            {
                Name = dbTipster.Name,
                Address = dbTipster.Link,
                Domain = dbTipster.Link.UrlToDomain()
            };
        }

        public static TipsterGvVM ToTipsterGvVM(DbTipster dbTipster)
        {
            return new TipsterGvVM
            {
                Id = dbTipster.Id,
                Name = dbTipster.Name,
                Link = dbTipster.Link,
                WebsiteAddress = dbTipster.Website?.Address, // VM nie powinien posiadać typów złożonych
                WebsiteLoginName = dbTipster.Website?.Login?.Name,
                WebsiteLoginPassword = dbTipster.Website?.Login?.Password
            };
        }
    }
}
