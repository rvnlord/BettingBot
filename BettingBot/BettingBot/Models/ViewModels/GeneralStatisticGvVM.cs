namespace BettingBot.Models.ViewModels
{
    public class GeneralStatisticGvVM
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public GeneralStatisticGvVM(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
