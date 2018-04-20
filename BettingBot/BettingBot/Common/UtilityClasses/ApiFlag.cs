namespace BettingBot.Common.UtilityClasses
{
    public class ApiFlag
    {
        public ApiFlagType FlagType { get; set; }
        public bool Value { get; set; }

        public ApiFlag(ApiFlagType flagType, bool value)
        {
            FlagType = flagType;
            Value = value;
        }
    }
}
