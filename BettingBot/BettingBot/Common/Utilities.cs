using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using BettingBot.Common.UtilityClasses;
using Telerik.Windows.Controls;
using Point = System.Windows.Point;
using DPoint = System.Drawing.Point;
using Size = System.Windows.Size;
using DSize = System.Drawing.Size;

namespace BettingBot.Common
{
    public static class StringUtils
    {
        public const string Space = " ";
    }

    public static class ArrayUtils
    {
        public static string[] Numbers { get; } = "1234567890".ToArray().Select(c => c.ToString()).ToArray();
        public static string[] Operators { get; } = "+-/*".ToArray().Select(c => c.ToString()).ToArray();
        public static string[] NumbersAndOperators => Numbers.Concat(Operators).ToArray();

        public static T[] ConcatMany<T>(params T[][] arrays) => arrays.SelectMany(x => x).ToArray();
    }

    public static class EnumUtils
    {
        public static List<DdlItem> EnumToDdlItems<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<int>().Select(i => new DdlItem(i, Enum.GetName(typeof(T), i))).ToList();
        }

        public static List<DdlItem> EnumToDdlItems<T>(Func<T, string> customNamesConverter)
        {
            return EnumToDdlItems<T>()
                .Select(item =>
                    new DdlItem(
                        item.Index,
                        customNamesConverter((T)Enum.ToObject(typeof(T), item.Index))))
                .ToList();
        }
    }

    public static class EnumerableUtils
    {
        public static IEnumerable<T> ConcatMany<T>(IEnumerable<T>[] enums)
        {
            return enums.SelectMany(x => x);
        }
    }

    public static class PointUtils
    {
        public static Point ScreenCenter()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            return new Point(screenWidth / 2, screenHeight / 2);
        }

        public static Point CenteredWindowTopLeft(Size size)
        {
            var center = ScreenCenter();
            return new Point(center.X - size.Width / 2, center.Y - size.Height / 2);
        }

        public static Point CenteredWindowTopLeft(DSize size)
        {
            return CenteredWindowTopLeft(size.ToSize());
        }
    }

    public static class GridViewSelectionUtilities
    {
        private static bool _isSyncingSelection;
        private static readonly List<Tuple<WeakReference, List<RadGridView>>> _collectionToGridViews = new List<Tuple<WeakReference, List<RadGridView>>>();

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(INotifyCollectionChanged),
            typeof(GridViewSelectionUtilities),
            new PropertyMetadata(null, OnSelectedItemsChanged));

        public static INotifyCollectionChanged GetSelectedItems(DependencyObject obj)
        {
            return (INotifyCollectionChanged)obj.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject obj, INotifyCollectionChanged value)
        {
            obj.SetValue(SelectedItemsProperty, value);
        }

        private static void OnSelectedItemsChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
        {
            var gridView = (RadGridView)target;

            if (args.OldValue is INotifyCollectionChanged oldCollection)
            {
                gridView.SelectionChanging -= GridView_SelectionChanging;
                oldCollection.CollectionChanged -= SelectedItems_CollectionChanged;
                RemoveAssociation(oldCollection, gridView);
            }

            if (args.NewValue is INotifyCollectionChanged newCollection)
            {
                gridView.SelectionChanging += GridView_SelectionChanging;
                newCollection.CollectionChanged += SelectedItems_CollectionChanged;
                //gridView.UnshiftEvent("SelectionChanged", new EventHandler<SelectionChangeEventArgs>(GridView_SelectionChanged));
                //newCollection.UnshiftEvent("CollectionChanged", new NotifyCollectionChangedEventHandler(SelectedItems_CollectionChanged));

                AddAssociation(newCollection, gridView);
                OnSelectedItemsChanged(newCollection, null, (IList)newCollection);
            }
        }

        private static void SelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            var collection = (INotifyCollectionChanged)sender;
            OnSelectedItemsChanged(collection, args.OldItems, args.NewItems);
        }

        private static void GridView_SelectionChanging(object sender, SelectionChangingEventArgs args)
        {
            if (_isSyncingSelection)
                return;

            var collection = (IList)GetSelectedItems((RadGridView)sender);
            foreach (var item in args.RemovedItems)
                collection.Remove(item);
            foreach (var item in args.AddedItems)
                collection.Add(item);
        }

        private static void OnSelectedItemsChanged(INotifyCollectionChanged collection, IList oldItems, IList newItems)
        {
            _isSyncingSelection = true;

            var gridViews = GetOrCreateGridViews(collection);
            foreach (var gridView in gridViews)
                SyncSelection(gridView, oldItems, newItems);

            _isSyncingSelection = false;
        }

        private static void SyncSelection(DataControl gridView, IEnumerable oldItems, IEnumerable newItems)
        {
            if (oldItems != null)
                SetItemsSelection(gridView, oldItems, false);
            if (newItems != null)
                SetItemsSelection(gridView, newItems, true);
        }

        private static void SetItemsSelection(DataControl gridView, IEnumerable items, bool shouldSelect)
        {
            foreach (var item in items)
            {
                var contains = gridView.SelectedItems.Contains(item);
                if (shouldSelect && !contains)
                    gridView.SelectedItems.Add(item);
                else if (contains && !shouldSelect)
                    gridView.SelectedItems.Remove(item);
            }
        }

        private static void AddAssociation(INotifyCollectionChanged collection, RadGridView gridView)
        {
            var gridViews = GetOrCreateGridViews(collection);
            gridViews.Add(gridView);
        }

        private static void RemoveAssociation(INotifyCollectionChanged collection, RadGridView gridView)
        {
            var gridViews = GetOrCreateGridViews(collection);
            gridViews.Remove(gridView);

            if (gridViews.Count == 0)
                CleanUp();
        }

        private static List<RadGridView> GetOrCreateGridViews(INotifyCollectionChanged collection)
        {
            foreach (var t in _collectionToGridViews)
            {
                var wr = t.Item1;
                if (wr.Target == collection)
                    return t.Item2;
            }

            _collectionToGridViews.Add(new Tuple<WeakReference, List<RadGridView>>(new WeakReference(collection), new List<RadGridView>()));
            return _collectionToGridViews[_collectionToGridViews.Count - 1].Item2;
        }

        private static void CleanUp()
        {
            for (var i = _collectionToGridViews.Count - 1; i >= 0; i--)
            {
                var isAlive = _collectionToGridViews[i].Item1.IsAlive;
                var behaviors = _collectionToGridViews[i].Item2;
                if (behaviors.Count == 0 || !isAlive)
                    _collectionToGridViews.RemoveAt(i);
            }
        }
    }
}
