using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Clients.Responses
{
    public class PickResponse
    {
        public string PickOriginalString { get; }
        public PickChoice Choice { get; set; }
        public double? Value { get; set; }

        public PickResponse(string pickOriginalString, PickChoice choice, double? value)
        {
            PickOriginalString = pickOriginalString;
            Choice = choice;
            Value = value;
        }

        public override string ToString() => PickConverter.PickToString(Choice, Value);
        public DbPick ToDbPick() => PickConverter.ToDbPick(this);
    }
}
