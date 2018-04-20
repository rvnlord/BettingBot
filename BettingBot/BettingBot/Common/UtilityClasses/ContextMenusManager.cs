using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MoreLinq;

namespace BettingBot.Common.UtilityClasses
{
    public static class ContextMenusManager
    {
        private static Panel _contextMenusContainer;

        public static Panel ContextMenusContainer
        {
            get => _contextMenusContainer;
            set
            {
                _contextMenusContainer = value;
                _contextMenusContainer.PreviewMouseRightButtonUp += ContextMenusContainer_PreviewMouseRightButtonUp;
                _contextMenusContainer.PreviewMouseLeftButtonDown += ContextMenusContainer_PreviewMouseLeftButtonDown;
            }
        }

        public static Dictionary<FrameworkElement, ContextMenu> ContextMenus { get; set; } = new Dictionary<FrameworkElement, ContextMenu>();

        public static ContextMenu ContextMenu(FrameworkElement fe)
        {
            if (ContextMenus.VorN(fe) == null)
                ContextMenus[fe] = new ContextMenu(fe);
            return ContextMenus[fe];
        }

        public static void CloseAll()
        {
            ContextMenus.ForEach(cm => cm.Value?.Close());
        }

        public static void CloseAll(Func<ContextMenu, bool> selector)
        {
            ContextMenus.Select(kvp => kvp.Value).Where(selector).ForEach(cm => cm?.Close());
        }

        private static void ContextMenusContainer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var cmCon = (Panel) sender;
            var mouseHoveredElements = cmCon.FindLogicalDescendants<FrameworkElement>() // TextBox, RadDatePicker, RadGridView
                .Where(f =>
                    f.GetType() != typeof(Grid) && f.GetType() != typeof(MetroAnimatedTabControl) &&
                    (f.FindLogicalAncestor<Grid>() == null || f.FindLogicalAncestor<Grid>().IsVisible) &&
                    (f.FindLogicalAncestor<MetroTabItem>() == null || f.FindLogicalAncestor<MetroTabItem>().IsSelected) &&
                    f.FindLogicalAncestor<StackPanel>(sp => sp.Name.EndsWith("ContextMenu")) == null &&
                    f.HasClientRectangle(cmCon) && f.ClientRectangle(cmCon).Contains(e.GetPosition(cmCon)) &&
                    f.IsVisible && f.Opacity > 0).ToList();

            mouseHoveredElements = mouseHoveredElements.GroupBy(Panel.GetZIndex).MaxBy(g => g.Key).ToList();
            if (!mouseHoveredElements.Any())
                CloseAll();

            if (mouseHoveredElements.Any(f => f.FindLogicalAncestor<Grid>(anc => anc.Name.EndsWith("Flyout")) != null))
                mouseHoveredElements.RemoveBy(f => f.FindLogicalAncestor<Grid>(anc => anc.Name.EndsWith("Flyout")) == null);

            if (mouseHoveredElements.Count > 1)
            {
                var message =
                    "Występuje wiele elementów do wyświetlenia menu kontekstowego (conajmniej dwa mają jednakowe ZIndeksy):\n" +
                    string.Join("\n", mouseHoveredElements.Select(el => $"({el.ZIndex()}: {el.Name})"));
                Debug.Print(message);
            }

            foreach (var c in mouseHoveredElements)
            {
                e.Handled = true;
                CloseAll(cm => !cm.IsHovered());
                if (c.HasContextMenu() && c.IsWithinBounds(cmCon) && !c.ContextMenu().IsHovered())
                    c.ContextMenu().Open();
            }
        }

        private static void ContextMenusContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAll(cm => !cm.IsHovered());
        }
    }
}
