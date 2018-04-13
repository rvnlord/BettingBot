using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;

namespace BettingBot.Source.Converters
{
    public class WebsiteConverter
    {
        public static WebsiteGvVM ToWebsiteGvVM(DbWebsite dbWebsite)
        {
            return new WebsiteGvVM
            {
                Address = dbWebsite.Address,
                Login = dbWebsite.Login?.ToLoginGvVM()
            };
        }
    }
}
