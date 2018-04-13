using System;
using System.Collections.Generic;
using BettingBot.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.ViewModels;
using BetshootTipsterResponse = BettingBot.Source.Clients.Agility.Betshoot.Responses.TipsterResponse;
using HintwiseTipsterResponse = BettingBot.Source.Clients.Selenium.Hintwise.Responses.TipsterResponse;

namespace BettingBot.Source.DbContext.Models
{
    [Serializable]
    public class DbTipster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        
        public int? WebsiteId { get; set; }
        
        public virtual DbWebsite Website { get; set; }
        public virtual ICollection<DbBet> Bets { get; set; } = new List<DbBet>();

        public DbTipster(int id, string name, string link, int? websiteId)
        {
            Id = id;
            Name = name;
            WebsiteId = websiteId;
            Link = link;
        }

        public DbTipster()
        {
        }

        public static DbTipster Me()
        {
            return new DbTipster(-1, "my", "", null);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbTipster)) return false;

            var ot = (DbTipster) obj;
            return Id == ot.Id && Name == ot.Name && Link == ot.Link && WebsiteId == ot.WebsiteId;
        }

        public bool NameLinkEquals(object obj)
        {
            if (!(obj is DbTipster)) return false;

            var ot = (DbTipster)obj;
            return Name == ot.Name && Link == ot.Link;
        }

        public bool NameDomainEquals(object obj)
        {
            if (!(obj is DbTipster)) return false;

            var ot = (DbTipster)obj;
            return Name == ot.Name 
                && Link.UrlToDomain() == ot.Link.UrlToDomain();
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ 19
                * Link.UrlToDomain().GetHashCode() ^ 23;
        }

        public BetshootTipsterResponse ToBetshootTipsterResponse() => TipsterConverter.ToBetshootTipsterResponse(this);
        public HintwiseTipsterResponse ToHintwiseTipsterResponse() => TipsterConverter.ToHintwiseTipsterResponse(this);
        public TipsterGvVM ToTipsterGvVM() => TipsterConverter.ToTipsterGvVM(this);
    }
}