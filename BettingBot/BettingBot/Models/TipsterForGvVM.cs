using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models
{
    public class TipsterForGvVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Domain
        {
            get
            {
                var sign = Website.LoginId != null ? "+" : "-";
                return $"{Website.Address} ({sign})";
            }
        }

        public string Link { get; set; }

        public int? WebsiteId { get; set; }

        public virtual Website Website { get; set; }
    }
}
