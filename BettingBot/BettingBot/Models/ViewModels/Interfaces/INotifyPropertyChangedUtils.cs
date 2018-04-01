namespace BettingBot.Models.ViewModels.Interfaces
{
    public interface INotifyPropertyChangedUtils
    {
        void SetPropertyAndNotify<T>(ref T field, T propVal, string propName);
    }
}
