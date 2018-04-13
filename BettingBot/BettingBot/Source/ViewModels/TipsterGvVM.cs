using System;
using BettingBot.Source.DbContext.Models;
using Org.BouncyCastle.Asn1.Esf;

namespace BettingBot.Source.ViewModels
{
    public class TipsterGvVM : BaseVM //, IComparable<TipsterGvVM>
    {
        private int _id;
        private string _name;
        private string _link;
        private string _websiteAddress;
        private string _websiteLoginName;
        private string _websiteLoginPassword;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public string Name { get => _name; set => SetPropertyAndNotify(ref _name, value, nameof(Name)); }
        public string Link { get => _link; set => SetPropertyAndNotify(ref _link, value, nameof(Link)); }
        public string WebsiteAddress { get => _websiteAddress; set => SetPropertyAndNotify(ref _websiteAddress, value, nameof(WebsiteAddress)); }
        public string WebsiteLoginName { get => _websiteLoginName; set => SetPropertyAndNotify(ref _websiteLoginName, value, nameof(WebsiteLoginName)); }
        public string WebsiteLoginPassword { get => _websiteLoginPassword; set => SetPropertyAndNotify(ref _websiteLoginPassword, value, nameof(WebsiteLoginPassword)); }
        public string DomainWithOp
        {
            get
            {
                if (WebsiteAddress == null) return null;
                var op = WebsiteLoginName != null && WebsiteLoginPassword != null ? "+" : "-";
                return $"{WebsiteAddress} ({op})";
            }
        }

        //public int CompareTo(TipsterGvVM other)
        //{
        //    var compareWebsites = string.Compare(DomainWithOp, other.DomainWithOp, StringComparison.Ordinal);
        //    var compareNames = string.Compare(_name, other.Name, StringComparison.Ordinal);
        //    return compareWebsites == 0 ? compareNames : compareWebsites;
        //}

        public override bool Equals(object obj)
        {
            if (!(obj is TipsterGvVM)) return false;
            var oTipster = (TipsterGvVM) obj;

            return _name == oTipster.Name && _link == oTipster.Link;
        }

        public override int GetHashCode() => _name.GetHashCode() * 11 ^ _link.GetHashCode() * 17;
    }
}
