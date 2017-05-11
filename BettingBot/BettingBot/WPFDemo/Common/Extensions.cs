using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoMapper;
using DomainParser.Library;
using MoreLinq;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.GridView;
using Expression = System.Linq.Expressions.Expression;
using WPFDemo.Models;
using WPFDemo.Common.UtilityClasses;
using MenuItem = WPFDemo.Common.UtilityClasses.MenuItem;

namespace WPFDemo.Common
{
    public static class Extensions
    {
        #region Constants

        private const double TOLERANCE = 0.00001;

        #endregion

        #region T Extensions

        public static bool EqualsAny<T>(this T o, params T[] os)
        {
            return os.Any(s => s.Equals(o));
        }

        #endregion

        #region String Extensions

        public static bool HasValueBetween(this string str, string start, string end)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end) &&
                   str.Contains(start) &&
                   str.Contains(end) &&
                   str.IndexOf(start, StringComparison.Ordinal) < str.IndexOf(end, StringComparison.Ordinal);
        }

        public static string Between(this string str, string start, string end)
        {
            return Regex.Match(str, $@"\{start}([^)]*)\{end}").Groups[1].Value;
        }

        public static string UntilWithout(this string str, string end)
        {
            return str.Split(new[] { end }, StringSplitOptions.None)[0];
        }

        public static string RemoveHTMLSymbols(this string str)
        {
            return str.Replace("&nbsp;", "")
                .Replace("&amp;", "");
        }

        public static bool IsNullWhiteSpaceOrDefault(this string str, string defVal)
        {
            return string.IsNullOrWhiteSpace(str) || str == defVal;
        }

        public static bool ContainsAny(this string str, params string[] strings)
        {
            return strings.Any(str.Contains);
        }

        public static string Remove(this string str, string substring)
        {
            return str.Replace(substring, "");
        }

        public static string RemoveMany(this string str, params string[] substrings)
        {
            return substrings.Aggregate(str, (current, substring) => current.Remove(substring));
        }

        public static bool IsSimilarTo(this string str, string otherStr)
        {
            var m = new Metaphone();
            return m.Encode(str) == m.Encode(otherStr);
        }

        public static bool IsSimilarToAny(this string str, params string[] strings)
        {
            return strings.Any(s => s.IsSimilarTo(str));
        }

        public static string[] Split(this string str, string separator, bool includeSeparator = false)
        {
            var split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            if (includeSeparator)
            {
                var splitWithSeparator = new string[split.Length + split.Length - 1];
                var j = 0;
                for (var i = 0; i < splitWithSeparator.Length; i++)
                {
                    if (i % 2 == 1)
                        splitWithSeparator[i] = separator;
                    else
                        splitWithSeparator[i] = split[j++];
                }
                split = splitWithSeparator;
            }
            return split;
        }

        public static string[] SplitByFirst(this string str, params string[] strings)
        {
            foreach (var s in strings)
                if (str.Contains(s))
                    return str.Split(s);
            return new[] { str };
        }
        
        public static string[] SameWords(this string str, string otherStr, bool casaeSensitive = false, string splitBy = " ", int minWordLength = 1)
        {
            if (casaeSensitive)
            {
                str = str.ToLower();
                otherStr = otherStr.ToLower();
            }
            
            var str1Arr = str.Split(splitBy);
            var str2Arr = otherStr.Split(splitBy);
            var intersection = str1Arr.Intersect(str2Arr).Where(w => w.Length >= minWordLength);
            return intersection.ToArray();
        }

        public static string[] SameWords(this string str, string[] otherStrings, bool casaeSensitive, string splitBy = " ", int minWordLength = 1)
        {
            var sameWords = new List<string>();

            foreach (var otherStr in otherStrings)
                sameWords.AddRange(str.SameWords(otherStr, casaeSensitive, splitBy, minWordLength));

            return sameWords.Distinct().ToArray();
        }

        public static string[] SameWords(this string str, params string[] otherStrings)
        {
            return str.SameWords(otherStrings, false, " ", 1);
        }

        public static bool HasSameWords(this string str, string otherStr, bool caseSensitive = false, string splitBy = " ", int minWordLength = 1)
        {
            return str.SameWords(otherStr, caseSensitive, splitBy, minWordLength).Any();
        }

        public static bool HasSameWords(this string str, string[] otherStrings, bool caseSensitive, string splitBy = " ", int minWordLength = 1)
        {
            return str.SameWords(otherStrings, caseSensitive, splitBy, minWordLength).Any();
        }

        public static bool HasSameWords(this string str, params string[] otherStrings)
        {
            return str.SameWords(otherStrings, false, " ", 1).Any();
        }

        public static string[] SimilarWords(this string str, string otherStr, bool caseSensitive = false, string splitBy = " ", int minWordLength = 2, bool includeMistyped = true)
        {
            if (caseSensitive)
            {
                str = str.ToLower();
                otherStr = otherStr.ToLower();
            }

            var str1Arr = str.Split(splitBy).Where(w => w.Length >= minWordLength).ToArray();
            var str2Arr = otherStr.Split(splitBy).Where(w => w.Length >= minWordLength).ToArray();

            var m = new Metaphone();
            var metaphoneStr1Arr = str1Arr.Select(w => m.Encode(w)).ToArray();
            var metaphoneStr2Arr = str2Arr.Select(w => m.Encode(w)).ToArray();
            var metaphoneIntersection = metaphoneStr1Arr.Intersect(metaphoneStr2Arr).ToArray();

            var mistypedIntersection = new List<string>();

            if (includeMistyped) // uwaga, przy takim porównywaniu wyjdzie, że II is similar to Munich
                foreach (var s1 in str1Arr)
                    foreach (var s2 in str2Arr)
                        if (Math.Abs(s1.Length - s2.Length) <= 2 && (s1.ContainsAll(s2.Select(c => c.ToString()).ToArray()) || s2.ContainsAll(s1.Select(c => c.ToString()).ToArray())))
                            mistypedIntersection.Add(m.Encode(s1));

            return metaphoneIntersection.Concat(mistypedIntersection).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray();
        }

        public static string[] SimilarWords(this string str, string[] otherStrings, bool caseSensitive, string splitBy = " ", int minWordLength = 2)
        {
            var sameWords = new List<string>();

            foreach (var otherStr in otherStrings)
                sameWords.AddRange(str.SimilarWords(otherStr, caseSensitive, splitBy, minWordLength));

            return sameWords.Distinct().ToArray();
        }

        public static string[] SimilarWords(this string str, params string[] otherStrings)
        {
            return str.SimilarWords(otherStrings, false, " ", 2);
        }

        public static bool HasSimilarWords(this string str, string otherStr, bool caseSensitive = false, string splitBy = " ", int minWordLength = 2)
        {
            return str.SimilarWords(otherStr, caseSensitive, splitBy, minWordLength).Any();
        }

        public static bool HasSimilarWords(this string str, string[] otherStrings, bool caseSensitive, string splitBy = " ", int minWordLength = 2)
        {
            return str.SimilarWords(otherStrings, caseSensitive, splitBy, minWordLength).Any();
        }

        public static bool HasSimilarWords(this string str, params string[] otherStrings)
        {
            return str.SimilarWords(otherStrings, false, " ", 2).Any();
        }

        public static double? TryToDouble(this string str)
        {
            str = str.Replace(',', '.');
            double value;
            var isParsable = double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            if (isParsable)
                return value;
            return null;
        }

        public static double ToDouble(this string str)
        {
            var parsedD = str.TryToDouble();
            if (parsedD == null)
                throw new InvalidCastException("Nie można sparsować wartości double");
            return (double) parsedD;
        }

        public static bool IsDouble(this string str)
        {
            return str.TryToDouble() != null;
        }

        public static bool StartsWithAny(this string str, params string[] strings)
        {
            return strings.Any(str.StartsWith);
        }

        public static bool EndsWithAny(this string str, params string[] strings)
        {
            return strings.Any(str.EndsWith);
        }

        public static bool ContainsAll(this string str, params string[] strings)
        {
            return strings.All(str.Contains);
        }

        public static string RemoveWord(this string str, string word, string separator = " ")
        {
            return string.Join(separator, str.Split(separator).Where(w => w != word));
        }

        public static string RemoveWords(this string str, string[] words, string separator)
        {
            foreach (var w in words)
                str = str.RemoveWord(w);
            return str;
        }

        public static string RemoveWords(this string str, params string[] words)
        {
            return str.RemoveWords(words, " ");
        }

        public static bool IsUrl(this string str)
        {
            Uri uriResult;
            return Uri.TryCreate(str, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static string UrlToDomain(this string str)
        {
            DomainName completeDomain;
            return DomainName.TryParse(new Uri(str).Host, out completeDomain) ? completeDomain.SLD : "";
        }

        #endregion

        #region Double Extensions

        public static bool Eq(this double x, double y)
        {
            return Math.Abs(x - y) < TOLERANCE;
        }

        #endregion

        #region DateTime Extensions

        public static string MonthName(this DateTime date)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(date.Month);
        }

        public static DateTime Period(this DateTime date, int periodInDays)
        {
            var startDate = new DateTime();
            var myDate = new DateTime(date.Year, date.Month, date.Day);
            var diff = myDate - startDate;
            return myDate.AddDays(-(diff.TotalDays % periodInDays));
        }

        public static DateTime? ToDMY(this DateTime? dateTimeNullable)
        {
            if (dateTimeNullable == null)
                return null;

            var date = (DateTime)dateTimeNullable;
            date = new DateTime(date.Year, date.Month, date.Day);
            return date;
        }

        public static DateTime ToDMY(this DateTime dateTime)
        {
            var date = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
            return date;
        }


        #endregion

        #region Collections Extensions

        #region - Array Extensions

        public static T[] Swap<T>(this T[] a, int i, int j)
        {
            var temp = a[j];
            a[j] = a[i];
            a[i] = temp;
            return a;
        }


        #endregion

        #region - List Extensions

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static List<Bet> DeepClone(this IList<Bet> betsToClone)
        {
            return betsToClone.Select(b => (Bet)b.DeepClone()).ToList();
        }

        public static void RemoveBy<TSource>(this List<TSource> source, Func<TSource, bool> selector) where TSource : class
        {
            var src = source.ToArray();
            foreach (var entity in src)
            {
                if (selector(entity))
                    source.Remove(entity);
            }
        }

        public static void RemoveByMany<TSource, TKey>(this List<TSource> source, Func<TSource, TKey> selector, IEnumerable<TKey> matches) where TSource : class
        {
            foreach (var match in matches)
                source.RemoveBy(e => Equals(selector(e), match));
        }

        public static T[] ToArray<T>(this IList<T> list)
        {
            var array = new T[list.Count];
            list.CopyTo(array, 0);
            return array;
        }

        public static object[] ToArray(this IList list)
        {
            var array = new object[list.Count];
            list.CopyTo(array, 0);
            return array;
        }


        #endregion

        #region - IEnumerable Extensions

        public static T LastOrNull<T>(this IEnumerable<T> enumerable)
        {
            var en = enumerable as T[] ?? enumerable.ToArray();
            return en.Any() ? en.Last() : (T)Convert.ChangeType(null, typeof(T));
        }

        public static List<T> MapToVMCollection<T>(this IEnumerable source)
        {
            var dest = new List<T>();
            foreach (var srcEl in source)
            {
                var destEl = (T)Activator.CreateInstance(typeof(T), new object[] { });
                Mapper.Map(srcEl, destEl);
                dest.Add(destEl);
            }
            return dest;
        }

        public static IEnumerable<T> ConcatMany<T>(this IEnumerable<T> enumerable, params IEnumerable<T>[] enums)
        {
            return enumerable.Concat(enums.SelectMany(x => x));
        }

        public static IEnumerable<TSource> WhereByMany<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IEnumerable<TKey> matches) where TSource : class
        {
            return source.Where(e => matches.Any(sel => Equals(sel, selector(e))));
        }


        #endregion

        #region - IQueryable Extensions

        public static TEntity LastOrNullBy<TEntity, TValue>(this IQueryable<TEntity> query, Expression<Func<TEntity, TValue>> keyFieldPredicate)
        {
            if (keyFieldPredicate == null)
                throw new ArgumentNullException(nameof(keyFieldPredicate));

            var p = keyFieldPredicate.Parameters.Single();

            if (query.LongCount() == 0)
                return default(TEntity);

            var max = query.Max(keyFieldPredicate);

            var equalsOne = (Expression)Expression.Equal(keyFieldPredicate.Body, Expression.Constant(max, typeof(TValue)));
            return query.Single(Expression.Lambda<Func<TEntity, bool>>(equalsOne, p));
        }


        #endregion

        #region - DbSet Extensions

        public static void RemoveBy<TSource>(this DbSet<TSource> dbSet, Func<TSource, bool> selector) where TSource : class
        {
            var set = dbSet.ToArray();
            foreach (var entity in set)
            {
                if (selector(entity))
                    dbSet.Remove(entity);
            }
        }

        public static void RemoveByMany<TSource, TKey>(this DbSet<TSource> dbSet, Func<TSource, TKey> selector, IEnumerable<TKey> matches) where TSource : class
        {
            foreach (var match in matches)
                dbSet.RemoveBy(e => Equals(selector(e), match));
        }

        public static void RemoveDuplicatesBy<TSource, TKey>(this DbSet<TSource> dbSet, Func<TSource, TKey> selector) where TSource : class
        {
            var knownKeys = new HashSet<TKey>();
            foreach (var entity in dbSet)
                if (!knownKeys.Add(selector(entity)))
                    dbSet.Remove(entity);
        }

        public static int Next<T>(this DbSet<T> dbSet, Func<T, int> selector) where T : class
        {
            return dbSet.Any() ? dbSet.Select(selector).Max() + 1 : 0;
        }

        public static IQueryable<Tipster> ButSelf(this DbSet<Tipster> tipsters)
        {
            var self = Tipster.Me();
            return tipsters.Where(t => t.Name != self.Name);
        }

        #endregion

        #region - ItemCollection Extensions

        public static T[] ToArray<T>(this ItemCollection list)
        {
            var array = new T[list.Count];
            list.CopyTo(array, 0);
            return array;
        }

        #endregion

        #endregion

        #region PrimitivePropertyConfiguration Extensions

        public static PrimitivePropertyConfiguration HasUniqueIndexAnnotation(
            this PrimitivePropertyConfiguration property,
            string indexName,
            int columnOrder)
        {
            var indexAttribute = new IndexAttribute(indexName, columnOrder)
            {
                IsUnique = true
            };
            var indexAnnotation = new IndexAnnotation(indexAttribute);

            return property.HasColumnAnnotation(IndexAnnotation.AnnotationName, indexAnnotation);
        }

        #endregion

        #region DependencyObject

        public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            foreach (var rawChild in LogicalTreeHelper.GetChildren(depObj))
            {
                var depObjRawChild = rawChild as DependencyObject;
                if (depObjRawChild == null) continue;
                var child = depObjRawChild;
                var tChild = child as T;
                if (tChild != null)
                    yield return tChild;

                foreach (var childOfChild in FindLogicalChildren<T>(child))
                    yield return childOfChild;
            }
        }

        public static IEnumerable<Control> FindLogicalChildren<T1, T2>(this DependencyObject depObj) 
            where T1 : DependencyObject
            where T2 : DependencyObject
        {
            return ConcatMany(FindLogicalChildren<T1>(depObj).Cast<Control>(), FindLogicalChildren<T2>(depObj).Cast<Control>());
        }
        
        public static ScrollViewer GetScrollViewer(this DependencyObject o)
        {
            if (o is ScrollViewer) { return (ScrollViewer)o; }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                    continue;
                return result;
            }

            return null;
        }

        public static T FindLogicalParent<T>(this DependencyObject child) where T : DependencyObject
        {
            while (true)
            {
                var parentObject = LogicalTreeHelper.GetParent(child);
                if (parentObject == null) return null;
                var parent = parentObject as T;
                if (parent != null) return parent;
                child = parentObject;
            }
        }

        #endregion

        #region FrameworkElement

        public static Rect ClientRectangle(this FrameworkElement el, FrameworkElement container)
        {
            var rect = VisualTreeHelper.GetDescendantBounds(el);
            var loc = el.TransformToAncestor(container).Transform(new Point(0, 0));
            rect.Location = loc;
            return rect;
        }

        public static bool HasClientRectangle(this FrameworkElement el, FrameworkElement container)
        {
            return !VisualTreeHelper.GetDescendantBounds(el).IsEmpty;
        }

        public static bool IsWithinBounds(this FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.IntersectsWith(bounds);
        }

        public static bool HasContextMenu(this FrameworkElement el)
        {
            return RadContextMenu.GetContextMenu(el) != null;
        }

        public static RadContextMenu ContextMenu(this FrameworkElement el)
        {
            return RadContextMenu.GetContextMenu(el);
        }

        #endregion

        #region Textbox Extensions

        public static void ResetValue(this TextBox thisTxtBox, bool force = false)
        {
            var text = thisTxtBox.Text;
            var tag = thisTxtBox.Tag;
            if (tag == null)
                return;

            var placeholder = tag.ToString();
            if (text != placeholder && !string.IsNullOrWhiteSpace(text) && !force)
                return;

            var currBg = ((SolidColorBrush)thisTxtBox.Foreground).Color;
            var newBrush = new SolidColorBrush(Color.FromArgb(128, currBg.R, currBg.G, currBg.B));

            thisTxtBox.FontStyle = FontStyles.Italic;
            thisTxtBox.Foreground = newBrush;
            thisTxtBox.Text = placeholder;
        }

        public static void ClearValue(this TextBox thisTxtBox, bool force = false)
        {
            var text = thisTxtBox.Text;
            var tag = thisTxtBox.Tag;
            if (tag == null)
                return;

            var placeholder = tag.ToString();
            if (text != placeholder && !force)
                return;

            var currBg = ((SolidColorBrush)thisTxtBox.Foreground).Color;
            var newBrush = new SolidColorBrush(Color.FromArgb(255, currBg.R, currBg.G, currBg.B));

            thisTxtBox.FontStyle = FontStyles.Normal;
            thisTxtBox.Foreground = newBrush;
            thisTxtBox.Text = string.Empty;
        }

        public static bool IsNullWhitespaceOrTag(this TextBox txt)
        {
            return txt.Text?.IsNullWhiteSpaceOrDefault(txt.Tag?.ToString() ?? "") ?? false;
        }

        #endregion

        #region RadComboBox Extensions

        public static void SelectByCustomId(this RadComboBox rddl, int id)
        {
            rddl.SelectedItem = rddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
        }

        public static T SelectedEnumValue<T>(this RadComboBox rddl)
        {
            var selectedItem = (DdlItem)rddl.SelectedItem;
            var enumType = typeof(T);

            var value = (Enum)Enum.ToObject(enumType, selectedItem.Index);
            if (Enum.IsDefined(enumType, value) == false)
                throw new NotSupportedException($"Nie można przekonwertować wartości na podany typ: {enumType}");

            return (T)(object)value;
        }

        #endregion

        #region RadListBox Extensions

        public static void SelectByCustomId(this RadListBox mddl, int id)
        {
            var item = mddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
            mddl.SelectedItems.Add(item);
        }

        public static void SelectAll(this RadListBox mddl)
        {
            mddl.UnselectAll();
            var items = mddl.Items.ToArray();
            foreach (var item in items)
                mddl.SelectedItems.Add(item);
        }

        public static void UnselectAll(this RadListBox mddl)
        {
            var selectedItems = mddl.SelectedItems.ToArray();
            foreach (var item in selectedItems)
                mddl.SelectedItems.Remove(item);
        }

        public static void SelectByCustomIds(this RadListBox mddl, IEnumerable<int> ids)
        {
            var ddlItems = mddl.ItemsSource.Cast<DdlItem>().Where(i => ids.Any(id => i.Index == id)).ToList();
            foreach (var item in ddlItems)
                mddl.SelectedItems.Add(item);
        }

        public static int[] SelectedCustomIds(this RadListBox mddl)
        {
            return mddl.SelectedItems.Cast<DdlItem>().Select(i => i.Index).ToArray();
        }


        #endregion

        #region RadGridView Extensions

        public static void RefreshWith<T>(this RadGridView rgv, ICollection<T> data, bool scrollToEnd = true, bool selectLast = true)
        {
            rgv.ItemsSource = null;
            rgv.ItemsSource = data;
            if (scrollToEnd) rgv.ScrollToEnd(selectLast);
        }

        public static void ScrollToEnd(this RadGridView rgv, bool selectLast = false)
        {
            if (rgv.Items.Count > 0)
                rgv.ScrollIntoViewAsync(rgv.Items[rgv.Items.Count - 1],
                    rgv.Columns[rgv.Columns.Count - 1],
                    (frameworkElement) =>
                    {
                        var gridViewRow = frameworkElement as GridViewRow;
                        if (gridViewRow != null && selectLast) gridViewRow.IsSelected = true;
                    });
        }

        public static void ScrollToStart(this RadGridView rgv, bool selectLast = false)
        {
            if (rgv.Items.Count > 0)
                rgv.ScrollIntoViewAsync(rgv.Items[0],
                    rgv.Columns[0],
                    (frameworkElement) =>
                    {
                        var gridViewRow = frameworkElement as GridViewRow;
                        if (gridViewRow != null) gridViewRow.IsSelected = selectLast;
                    });
        }

        public static void ScrollToStart(this RadListBox lv, bool selectLast = false)
        {
            if (lv.Items.Count > 0)
                lv.GetScrollViewer().ScrollToTop();
        }

        public static void ScrollToEnd(this RadListBox lv, bool selectLast = false)
        {
            if (lv.Items.Count > 0)
                lv.GetScrollViewer().ScrollToBottom();
        }


        #endregion

        #region RadContextMenu Extensions

        public static IEnumerable<MenuItem> ContextItems(this RadContextMenu cm)
        {
            return cm.Items.ToArray<MenuItem>();
        }

        public static void EnableAll(this RadContextMenu cm)
        {
            var items = cm.ContextItems();
            foreach (var item in items)
                item.IsEnabled = true;
        }

        public static void DisableAll(this RadContextMenu cm)
        {
            var items = cm.ContextItems();
            foreach (var item in items)
                item.IsEnabled = false;
        }

        public static void Enable(this RadContextMenu cm, string optionName)
        {
            var items = cm.ContextItems();
            foreach (var item in items)
            {
                if (item.Text != optionName) continue;
                item.IsEnabled = true;
                return;
            }
        }

        public static void Enable(this RadContextMenu cm, params string[] optionNames)
        {
            var items = cm.ContextItems();
            foreach (var item in items)
                if (optionNames.Any(o => o == item.Text))
                    item.IsEnabled = true;
        }

        public static void Disable(this RadContextMenu cm, string optionName)
        {
            var items = cm.ContextItems();
            foreach (var item in items)
            {
                if (item.Text != optionName) continue;
                item.IsEnabled = false;
                return;
            }
        }

        public static void Disable(this RadContextMenu cm, params string[] optionNames)
        {
            var items = cm.ContextItems();
            foreach (var item in items)
                if (optionNames.Any(o => o == item.Text))
                    item.IsEnabled = false;
        }

        #endregion

        #region RadMenuItem Extensions

        public static MenuItem ContextItem(this RadMenuItem menuItem)
        {
            return menuItem?.DataContext as MenuItem;
        }

        #endregion

        #region RemoteWebDriver Extensions

        public static void EnableImplicitWait(this RemoteWebDriver driver)
        {
            driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 30);
        }

        public static void DisableImplicitWait(this RemoteWebDriver driver)
        {
            driver.Manage().Timeouts().ImplicitWait = new TimeSpan(0, 0, 0);
        }

        public static bool IsClosed(this RemoteWebDriver driver)
        {
            return driver?.SessionId == null;
        }

        public static bool IsOpen(this RemoteWebDriver driver)
        {
            return driver?.SessionId != null;
        }

        #endregion
    }
}
