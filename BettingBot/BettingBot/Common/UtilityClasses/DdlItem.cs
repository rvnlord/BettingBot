namespace BettingBot.Common.UtilityClasses
{
    public class DdlItem
    {
        public int Index { get; set; }
        public string Text { get; set; }

        public DdlItem(int index, string text)
        {
            Index = index;
            Text = text;
        }

        public override string ToString()
        {
            return $"{Index}, {Text}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != typeof(DdlItem)) return false;
            var oDdlItem = (DdlItem) obj;
            return Index == oDdlItem.Index && Text == oDdlItem.Text;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ (Text != null ? Text.GetHashCode() : 0);
            }
        }
    }
}
