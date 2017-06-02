using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BettingBot.Models
{
    public class UserForGvVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string HiddenPassword => "***";

        public virtual ICollection<Website> Websites { get; set; }

        public string Addresses => string.Join(", ", Websites.Select(w => w.Address));
    }
}
