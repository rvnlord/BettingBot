using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoreLinq;

namespace BettingBot.Common.UtilityClasses
{
    public class ContextMenu
    {
        public FrameworkElement Control { get; }
        public List<ContextMenuItem> Items { get; private set; }
        public bool IsCreated { get; private set; }
        public bool IsEmpty => !Items.Any();
        public StackPanel VisualContainer { get; set; }

        public bool IsEnabled
        {
            get => Items.Any(i => i.IsEnabled);
            set
            {
                if (value)
                    EnableAll();
                else 
                    DisableAll();
            }
        }
        
        public ContextMenu(FrameworkElement fe)
        {
            Control = fe;
            Items = new List<ContextMenuItem>();
        }

        public ContextMenu Create(IEnumerable<ContextMenuItem> menuItems)
        {
            var spStyle = (Style) Control.FindResource("ContextMenuStackPanel");
            VisualContainer = new StackPanel { Style = spStyle, Name = $"sp{Control.Name}ContextMenu"};
            Items = menuItems.Select(i => i.Create(VisualContainer)).ToList();
            Items.ForEach(i => i.ContextMenuItemClick += MenuItem_Click);
            ContextMenusManager.ContextMenusContainer.Children.Add(VisualContainer);
            IsCreated = true;
            VisualContainer.Visibility = Visibility.Hidden;
            return this;
        }

        public ContextMenu Create(params ContextMenuItem[] menuItems)
        {
            return Create(menuItems.AsEnumerable());
        }

        public void Destroy()
        {
            Items.ForEach(i =>
            {
                i.Destroy();
                i.ContextMenuItemClick -= MenuItem_Click;
            });
            Items.RemoveAll();
            ContextMenusManager.ContextMenusContainer.Children.Remove(VisualContainer);
            IsCreated = false;
        }

        public void Add(ContextMenuItem menuItem)
        {
            Items.Add(menuItem.Create(VisualContainer));
            menuItem.ContextMenuItemClick += MenuItem_Click;
        }

        public void AddRange(IEnumerable<ContextMenuItem> menuItems)
        {
            menuItems.ForEach(i =>
            {
                Items.Add(i.Create(VisualContainer));
                i.ContextMenuItemClick += MenuItem_Click;
            });
        }

        public void Remove(ContextMenuItem menuItem)
        {
            Items.Remove(menuItem.Destroy());
            menuItem.ContextMenuItemClick -= MenuItem_Click;
        }

        public void RemoveBy(Func<ContextMenuItem, bool> selector)
        {
            Items.RemoveAll(i => selector(i.Destroy()));
            Items.Where(selector).ForEach(i => i.ContextMenuItemClick -= MenuItem_Click);
        }

        public void ReplaceAll(IEnumerable<ContextMenuItem> menuItems)
        {
            Items.ForEach(i =>
            {
                i.Destroy();
                i.ContextMenuItemClick -= MenuItem_Click;
            });
            Items.RemoveAll();
            menuItems.ForEach(i =>
            {
                Items.Add(i.Create(VisualContainer));
                i.ContextMenuItemClick += MenuItem_Click;
            });
        }

        public void Open() => OpenAt(Mouse.GetPosition(ContextMenusManager.ContextMenusContainer));

        public void OpenAt(Point pos)
        {
            ContextMenusManager.CloseAll();

            VisualContainer.Opacity = 0;
            VisualContainer.Visibility = Visibility.Visible;
            
            var menusContainer = ContextMenusManager.ContextMenusContainer;

            if (pos.Y + VisualContainer.ActualHeight > menusContainer.ActualHeight)
                pos.Y = menusContainer.ActualHeight - VisualContainer.ActualHeight;
            if (pos.X + VisualContainer.ActualWidth > menusContainer.ActualWidth)
                pos.X = menusContainer.ActualWidth - VisualContainer.ActualWidth;

            VisualContainer.Margin(pos);

            VisualContainer.Opacity = 1;
            OnContextMenuOpening(new ContextMenuOpenEventArgs(this));
        }

        public void Close()
        {
            if (IsCreated)
                VisualContainer.Visibility = Visibility.Hidden;
        }

        public Point Position()
        {
            return VisualContainer.MarginPosition();
        }

        public void Enable(params string[] items)
        {
            Items.WhereByMany(i => i.Text, items).ForEach(i => i.Enable());
        }

        public void EnableAll()
        {
            Items.ForEach(i => i.Enable());
        }

        public void Disable(params string[] items)
        {
            Items.WhereByMany(i => i.Text, items).ForEach(i => i.Disable());
        }

        public void Disable(params int[] itemIds)
        {
            Items.WhereByMany(Items.Index, itemIds).ForEach(i => i.Disable());
        }

        public void DisableAll()
        {
            Items.ForEach(i => i.Disable());
        }

        private void MenuItem_Click(object sender, ContextMenuItemClickEventArgs e)
        {
            ContextMenusManager.CloseAll();
            OnContextMenuClicking(new ContextMenuClickEventArgs(e.Item, Items.Index(e.Item)));
        }

        public bool IsHovered()
        {
            if (!IsCreated) return false;
            var cmCon = ContextMenusManager.ContextMenusContainer;
            return VisualContainer.Visibility == Visibility.Visible 
                && VisualContainer.HasClientRectangle(cmCon) 
                && VisualContainer.ClientRectangle(cmCon).Contains(Mouse.GetPosition(cmCon));
        }

        public bool IsOpen()
        {
            return VisualContainer.Visibility == Visibility.Visible;
        }

        public event ContextMenuClickEventHandler ContextMenuClick;
        protected virtual void OnContextMenuClicking(ContextMenuClickEventArgs e) => ContextMenuClick?.Invoke(this, e);
        public event ContextMenuOpenEventHandler ContextMenuOpen;
        protected virtual void OnContextMenuOpening(ContextMenuOpenEventArgs e) => ContextMenuOpen?.Invoke(this, e);
    }

    public delegate void ContextMenuClickEventHandler(object sender, ContextMenuClickEventArgs e);

    public class ContextMenuClickEventArgs
    {
        public ContextMenuItem ClickedItem { get; }
        public int ClickedIndex { get; }
        public ContextMenuClickEventArgs(ContextMenuItem item, int index)
        {
            ClickedItem = item;
            ClickedIndex = index;
        }
    }

    public delegate void ContextMenuOpenEventHandler(object sender, ContextMenuOpenEventArgs e);

    public class ContextMenuOpenEventArgs
    {
        public ContextMenu OpenedMenu { get; }
        public ContextMenuOpenEventArgs(ContextMenu menu) => OpenedMenu = menu;
    }
}
