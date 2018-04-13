namespace BettingBot.Source.Clients.Selenium
{
    public interface ISeleniumAuthenticable
    {
        void EnsureLogin();
        void Login();
        bool IsLogged();
    }
}
