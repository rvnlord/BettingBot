namespace BettingBot.Common.UtilityClasses
{
    public interface INotifyPropertyChangedUtils
    {
        void SetPropertyAndNotify<T>(ref T field, T propVal, string propName);
    }
}
