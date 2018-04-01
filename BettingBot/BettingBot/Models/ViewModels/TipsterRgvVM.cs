using System;
using BettingBot.Models.ViewModels.Abstracts;

namespace BettingBot.Models.ViewModels
{
    public class TipsterRgvVM : BaseVM, IComparable<TipsterRgvVM>
    {
        private int _id;
        private string _name;
        private string _link;
        private int? _websiteId;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public string Name { get => _name; set => SetPropertyAndNotify(ref _name, value, nameof(Name)); }
        public string Link { get => _link; set => SetPropertyAndNotify(ref _link, value, nameof(Link)); }
        public int? WebsiteId { get => _websiteId; set => SetPropertyAndNotify(ref _websiteId, value, nameof(WebsiteId)); }
        public string Domain
        {
            get
            {
                if (Website == null) return null;
                var sign = Website.LoginId != null ? "+" : "-";
                return $"{Website.Address} ({sign})";
            }
        }

        public virtual Website Website { get; set; }

        public int CompareTo(TipsterRgvVM other)
        {
            var compareWebsites = string.Compare(Domain, other.Domain, StringComparison.Ordinal);
            var compareNames = string.Compare(_name, other.Name, StringComparison.Ordinal);
            return compareWebsites == 0 ? compareNames : compareWebsites;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TipsterRgvVM)) return false;
            var oTipster = (TipsterRgvVM) obj;
            return _name == oTipster.Name && _websiteId == oTipster.WebsiteId;
        }

        public override int GetHashCode() => _name.GetHashCode() ^ _websiteId.GetHashCode();
    }
}
