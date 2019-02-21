namespace BettingBot.Source.Common.UtilityClasses
{
    public class RepCounter<T>
    {
        public T Value { get; set; }
        public int Counter { get; set; }

        public RepCounter(T value, int counter)
        {
            Value = value;
            Counter = counter;
        }

        public override string ToString()
        {
            return $"{Value} [x{Counter}]";
        }
    }
}
