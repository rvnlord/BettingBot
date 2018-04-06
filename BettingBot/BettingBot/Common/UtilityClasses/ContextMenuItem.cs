using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;

namespace BettingBot.Common.UtilityClasses
{
    public class ContextMenuItem
    {
        private StackPanel _spMenuContainer;
        private Color _mouseOverColor;
        private Color _defaultColor;
        private Style _style;
        private Tile _tile;
        private bool _isEnabled;

        public PackIconModernKind Icon { get; set; }
        public string Text { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _tile.IsEnabled = value;
                _isEnabled = value;
            }
        }

        public ContextMenuItem(string text, PackIconModernKind icon = PackIconModernKind.None)
        {
            Icon = icon;
            Text = text;
        }

        public ContextMenuItem Create(StackPanel spMenuContainer)
        {
            _spMenuContainer = spMenuContainer;
            _mouseOverColor = ((SolidColorBrush) spMenuContainer.FindResource("MouseOverContextMenuItemTileBrush")).Color;
            _defaultColor = ((SolidColorBrush) spMenuContainer.FindResource("DefaultContextMenuItemTileBrush")).Color;
            _style = (Style) spMenuContainer.FindResource("ContextMenuItemTile");
            var iconStyle = (Style) spMenuContainer.FindResource("ContextMenuItemIcon");
            var tbStyle = (Style) spMenuContainer.FindResource("ContextMenuItemTextblock");
            
            var iconFactory = new FrameworkElementFactory(typeof(PackIconModern));
            iconFactory.SetValue(PackIconModern.KindProperty, Icon);
            iconFactory.SetValue(FrameworkElement.StyleProperty, iconStyle);

            var tbFactory = new FrameworkElementFactory(typeof(TextBlock));
            tbFactory.SetValue(TextBlock.TextProperty, Text);
            tbFactory.SetValue(FrameworkElement.StyleProperty, tbStyle);

            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            gridFactory.AppendChild(iconFactory);
            gridFactory.AppendChild(tbFactory);

            var dataTemplate = new DataTemplate(typeof(Tile)) { VisualTree = gridFactory };
            _tile = new Tile
            {
                Name = $"tl{spMenuContainer.Name.AfterFirst("sp")}{Text.Remove(" ")}Item",
                Background = new SolidColorBrush(_defaultColor),
                Style = _style,
                ContentTemplate = dataTemplate,
            };
            _tile.MouseEnter += tlContextMenuItem_MouseEnter;
            _tile.MouseLeave += tlContextMenuItem_MouseLeave;
            _tile.Click += tlContextMenuItem_Click;

            if (spMenuContainer.Children.ToArray().Any())
            {
                var prevLastTile = spMenuContainer.Children.OfType<Tile>().Last();
                prevLastTile.Margin = new Thickness(prevLastTile.Margin.Left, prevLastTile.Margin.Top, prevLastTile.Margin.Right, 0);
            }
            
            spMenuContainer.Children.Add(_tile);
            IsEnabled = true;
            return this;
        }

        public ContextMenuItem Destroy()
        {
            _spMenuContainer.Children.Remove(_tile);
            if (_spMenuContainer.Children.ToArray().Any())
            {
                var lastTile = _spMenuContainer.Children.OfType<Tile>().Last();
                lastTile.Margin = new Thickness(lastTile.Margin.Left, lastTile.Margin.Top, lastTile.Margin.Right, 2);
            }
            _tile.MouseEnter -= tlContextMenuItem_MouseEnter;
            _tile.MouseLeave -= tlContextMenuItem_MouseLeave;
            _tile.Click -= tlContextMenuItem_Click;
            _tile = null;
            _style = null;
            _spMenuContainer = null;
            return this;
        }

        public void Enable() => IsEnabled = true;
        public void Disable() => IsEnabled = false;

        private void tlContextMenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            tile.Highlight(_mouseOverColor);
        }

        private void tlContextMenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            tile.Highlight(_defaultColor);
        }

        private void tlContextMenuItem_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            OnContextMenuItemClicking(new ContextMenuItemClickEventArgs(this));
        }

        public event ContextMenuItemClickEventHandler ContextMenuItemClick;
        protected virtual void OnContextMenuItemClicking(ContextMenuItemClickEventArgs e) => ContextMenuItemClick?.Invoke(this, e);
    }

    public delegate void ContextMenuItemClickEventHandler(object sender, ContextMenuItemClickEventArgs e);

    public class ContextMenuItemClickEventArgs
    {
        public ContextMenuItem Item { get; }

        public ContextMenuItemClickEventArgs(ContextMenuItem item)
        {
            Item = item;
        }
    }
}
