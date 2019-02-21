using System.Linq;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source.Converters
{
    public static class LoginConverter
    {
        public static LoginGvVM ToLoginGvVM(DbLogin dbLogin)
        {
            return new LoginGvVM
            {
                Id = dbLogin.Id,
                Name = dbLogin.Name,
                Password = dbLogin.Password,
                WebsiteAddresses = dbLogin.Websites?.Select(w => w.Address).ToList()
            };
        }
    }
}
