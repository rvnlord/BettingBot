using System.ComponentModel;
using System.Windows.Controls;
using BettingBot.Source.Common.UtilityClasses;

namespace BettingBot.Source.Controls
{
    public abstract class UserControlWithNotifyProperty : UserControl, INotifyPropertyChanged, INotifyPropertyChangedUtils
    {
        public void SetPropertyAndNotify<T>(ref T field, T propVal, string propName)
        {
            if (Equals(field, propVal)) return;
            field = propVal;
            OnPropertyChanging(propName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanging(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
