using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BettingBot.Source.Common.UtilityClasses
{
    public class CustomMenuItem : INotifyPropertyChanged
    {
        private bool isEnabled = true;
        private string _text;
        private ObservableCollection<CustomMenuItem> _subItems;
        public event PropertyChangedEventHandler PropertyChanged;

        public CustomMenuItem(string text)
        {
            Text = text;
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled == value) return;
                isEnabled = value;
                OnNotifyPropertyChanged("IsEnabled");
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                OnNotifyPropertyChanged("Text");
            }
        }

        public ObservableCollection<CustomMenuItem> SubItems
        {
            get => _subItems ?? (_subItems = new ObservableCollection<CustomMenuItem>());
            set
            {
                if (_subItems == value) return;
                _subItems = value;
                OnNotifyPropertyChanged("SubItems");
            }
        }

        private void OnNotifyPropertyChanged(string ptopertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ptopertyName));
        }

        public override string ToString()
        {
            return $"{Text}, {(isEnabled ? "enabled" : "disabled")}";
        }
    }
}
