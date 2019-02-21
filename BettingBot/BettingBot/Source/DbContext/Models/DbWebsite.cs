using System;
using System.Collections.Generic;
using System.Linq;
using BettingBot.Source.Converters;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source.DbContext.Models
{
    [Serializable]
    public class DbWebsite
    {
        public int Id { get; set; }
        public string Address { get; set; }

        public int? LoginId { get; set; }

        public virtual DbLogin Login { get; set; }
        public virtual ICollection<DbTipster> Tipsters { get; set; } = new List<DbTipster>();

        public DbWebsite(int id, string address, int? loginId)
        {
            Id = id;
            Address = address;
            LoginId = loginId;
        }

        public DbWebsite()
        {
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbWebsite)) return false;
            var o = (DbWebsite)obj;

            return Address == o.Address;
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode() ^ 387;
        }

        public WebsiteGvVM ToWebsiteGvVM()
        {
            return WebsiteConverter.ToWebsiteGvVM(this);
        }
    }
}
