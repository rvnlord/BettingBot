namespace BettingBot.Source.ViewModels
{
    public class WebsiteGvVM : BaseVM
    {
        private int _id;
        private string _address;
        private LoginGvVM _login;

        public int Id { get => _id; set => SetPropertyAndNotify(ref _id, value, nameof(Id)); }
        public string Address { get => _address; set => SetPropertyAndNotify(ref _address, value, nameof(Address)); }
        public LoginGvVM Login { get => _login; set => SetPropertyAndNotify(ref _login, value, nameof(Login)); }
    }
}
