using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BettingBot.Models
{
    [Serializable]
    public class Tipster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        
        public int? WebsiteId { get; set; }
        
        public virtual Website Website { get; set; }
        public virtual ICollection<Bet> Bets { get; set; }

        public Tipster(int id, string name, string link, int? websiteId)
        {
            Id = id;
            Name = name;
            WebsiteId = websiteId;
            Link = link;

            Bets = new List<Bet>();
        }

        public Tipster()
        {
            Bets = new List<Bet>();
        }

        public static Tipster Me()
        {
            return new Tipster(-1, "my", "", null);
        }

        public bool EqualsIdNameLinkWebsiteId(object obj)
        {
            if (!(obj is Tipster)) return false;

            var ot = (Tipster) obj;
            return Id == ot.Id && Name == ot.Name && Link == ot.Link && WebsiteId == ot.WebsiteId;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Link?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (WebsiteId?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}