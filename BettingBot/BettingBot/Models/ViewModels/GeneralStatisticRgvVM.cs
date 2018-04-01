namespace BettingBot.Models.ViewModels
{
    public class GeneralStatisticRgvVM
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public GeneralStatisticRgvVM(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
