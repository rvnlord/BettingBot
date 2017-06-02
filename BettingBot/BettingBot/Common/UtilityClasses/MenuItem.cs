using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BettingBot.Common.UtilityClasses
{
    public class MenuItem : INotifyPropertyChanged
    {
        private bool isEnabled = true;
        private string _text;
        private ObservableCollection<MenuItem> _subItems;
        public event PropertyChangedEventHandler PropertyChanged;

        public MenuItem(string text)
        {
            Text = text;
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value) return;
                isEnabled = value;
                OnNotifyPropertyChanged("IsEnabled");
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value) return;
                _text = value;
                OnNotifyPropertyChanged("Text");
            }
        }

        public ObservableCollection<MenuItem> SubItems
        {
            get { return _subItems ?? (_subItems = new ObservableCollection<MenuItem>()); }
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
