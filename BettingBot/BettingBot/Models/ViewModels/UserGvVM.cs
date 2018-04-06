using System.Collections.Generic;
using System.Linq;
using BettingBot.Common;
using BettingBot.Models.ViewModels.Abstracts;

namespace BettingBot.Models.ViewModels
{
    public class UserGvVM : BaseVM
    {
        private int _id;
        private string _name;
        private string _password;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public string Name { get => _name; set => SetPropertyAndNotify(ref _name, value, nameof(Name)); }
        public string Password { get => _password; set => SetPropertyAndNotify(ref _password, value, nameof(Password)); }

        public string HiddenPassword => "***";
        public string Addresses => Websites.Select(w => w.Address).JoinAsString(", ");
        
        public virtual ICollection<Website> Websites { get; set; }
    }
}
