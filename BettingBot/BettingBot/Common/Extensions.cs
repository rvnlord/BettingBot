using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using AutoMapper;
using DomainParser.Library;
using MoreLinq;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using Expression = System.Linq.Expressions.Expression;
using BettingBot.Models;
using BettingBot.Common.UtilityClasses;
using BettingBot.Properties;
using Convert = System.Convert;
using CustomMenuItem = BettingBot.Common.UtilityClasses.CustomMenuItem;
using MahApps.Metro.Controls;
using ContextMenu = BettingBot.Common.UtilityClasses.ContextMenu;
using Point = System.Windows.Point;
using DPoint = System.Drawing.Point;
using Size = System.Windows.Size;
using DSize = System.Drawing.Size;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace BettingBot.Common
{
    public static class Extensions
    {
        #region Constants

        private const double TOLERANCE = 0.00001;

        #endregion

        #region Fields

        private static readonly Dictionary<FrameworkElement, Storyboard> _storyBoards = new Dictionary<FrameworkElement, Storyboard>();
        private static readonly Dictionary<string, bool> _panelAnimations = new Dictionary<string, bool>();
        private static readonly object _lock = new object();
        private static readonly Action _emptyDelegate = delegate { };

        public static CultureInfo Culture = new CultureInfo("pl-PL")
        {
            NumberFormat = new NumberFormatInfo { NumberDecimalSeparator = "." },
            DateTimeFormat = { ShortDatePattern = "dd-MM-yyyy" } // nie tworzyć nowego obiektu DateTimeFormat tutaj tylko przypisać jego interesujące nas właściwości, bo nowy obiekt nieokreślone właściwości zainicjalizuje wartościami dla InvariantCulture, czyli angielskie nazwy dni, miesięcy itd.
        };

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
            return str.AfterFirst(start).BeforeLast(end);
        }

        public static string TakeUntil(this string str, string end)
        {
            return str.Split(new[] { end }, StringSplitOptions.None)[0];
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
            return String.IsNullOrWhiteSpace(str) || str == defVal;
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
        
        public static bool IsDouble(this string str)
        {
            return str.ToDoubleN() != null;
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
            return String.Join(separator, str.Split(separator).Where(w => w != word));
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

        public static string AfterFirst(this string str, string substring)
        {
            if (!String.IsNullOrEmpty(substring) && str.Contains(substring))
            {
                var split = str.Split(substring);
                if (str.StartsWith(substring))
                    split = new[] { "" }.Concat(split).ToArray();
                return split.Skip(1).JoinAsString(substring);
            }
            return str;
        }

        public static string BeforeFirst(this string str, string substring)
        {
            if (!String.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).First();
            return str;
        }

        public static string AfterLast(this string str, string substring)
        {
            if (!String.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).Last();
            return str;
        }

        public static string BeforeLast(this string str, string substring)
        {
            if (!String.IsNullOrEmpty(substring) && str.Contains(substring))
            {
                var split = str.Split(substring);
                if (str.EndsWith(substring))
                    split = split.Concat(new[] { "" }).ToArray();
                var l = split.Length;
                return split.Take(l - 1).JoinAsString(substring);
            }

            return str;
        }

        public static string ToStringDelimitedBy<T>(this IEnumerable<T> enumerable, string strBetween = "")
        {
            return String.Join(strBetween, enumerable);
        }

        public static string JoinAsString<T>(this IEnumerable<T> enumerable, string strBetween = "")
        {
            return enumerable.ToStringDelimitedBy(strBetween);
        }

        public static string ToUrlEncoded(this string str)
        {
            return Uri.EscapeDataString(str);
        }

        public static string Take(this string str, int n)
        {
            return new string(str.AsEnumerable().Take(n).ToArray());
        }

        public static string Skip(this string str, int n)
        {
            return new string(str.AsEnumerable().Skip(n).ToArray());
        }

        #endregion

        #region Double Extensions

        public static bool Eq(this double x, double y)
        {
            return Math.Abs(x - y) < TOLERANCE;
        }

        public static bool Eq(this double? x, double? y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.ToDouble().Eq(y.ToDouble());
        }

        public static bool Between(this double d, double greaterThan, double lesserThan)
        {
            if (lesserThan < greaterThan)
            {
                var temp = lesserThan;
                lesserThan = greaterThan;
                greaterThan = temp;
            }
            return d > greaterThan && d < lesserThan;
        }

        public static double Round(this double number, int digits = 0)
        {
            return Math.Round(number, digits);
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
            for (var i = 0; i < list.Count; i++)
                array[i] = list[i];
            return array;
        }

        public static List<T> ReplaceAll<T>(this List<T> list, IEnumerable<T> en)
        {
            var newList = en.ToList();
            list.RemoveAll();
            list.AddRange(newList);
            return list;
        }

        public static List<T> ReplaceAll<T>(this List<T> list, T newEl)
        {
            return list.ReplaceAll(newEl.ToEnumerable());
        }
        
        public static void RemoveAll<T>(this IList<T> collection)
        {
            while (collection.Count != 0)
                collection.RemoveAt(0);
        }
        
        public static int RemoveAll(this IList list, Predicate<object> match)
        {
            var list2 = list.Cast<object>().Where(current => match(current)).ToList();
            foreach (var current2 in list2)
                list.Remove(current2);
            return list2.Count;
        }

        public static void AddRange(this IList list, IEnumerable items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            foreach (var current in items)
                list.Add(current);
        }

        #endregion

        #region - IEnumerable Extensions

        public static T LastOrNull<T>(this IEnumerable<T> enumerable)
        {
            var en = enumerable as T[] ?? enumerable.ToArray();
            return en.Any() ? en.Last() : (T)Convert.ChangeType(null, typeof(T));
        }

        public static List<T> MapTo<T>(this IEnumerable source)
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

        public static IEnumerable<TSource> WhereByMany<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> selector, 
            IEnumerable<TKey> matches) where TSource : class
        {
            return source.Where(e => matches.Any(sel => Equals(sel, selector(e))));
        }

        public static IEnumerable<TSource> OrderByWith<TSource, TResult>(this IEnumerable<TSource> en, Func<TSource, TResult> selector, IEnumerable<TResult> order)
        {
            return order.Select(el => en.Select(x => new { x, res = selector(x) }).Single(e => Equals(e.res, el)).x);
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, T el)
        {
            return enumerable.Except(new[] { el });
        }

        public static List<object> DisableControls(this IEnumerable<object> controls)
        {
            var disabledControls = new List<object>();
            foreach (var c in controls)
            {
                var piIsEnabled = c.GetType().GetProperty("IsEnabled");
                var isEnabled = (bool?) piIsEnabled?.GetValue(c);
                if (isEnabled == true)
                {
                    piIsEnabled.SetValue(c, false);
                    disabledControls.Add(c);
                }
            }
            return disabledControls;
        }

        public static void EnableControls(this IEnumerable<object> controls)
        {
            foreach (var c in controls)
            {
                var piIsEnabled = c.GetType().GetProperty("IsEnabled");
                piIsEnabled?.SetValue(c, true);

                if (c.GetType() == typeof(ContextMenu))
                {
                    var cm = (ContextMenu) c;
                    if (cm.IsOpen())
                    {
                        var wnd = cm.Control.FindLogicalAncestor<Window>();
                        var handler = wnd.GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == $"cm{cm.Control.Name.Take(1).ToUpper()}{cm.Control.Name.Skip(1)}_Open");
                        handler?.Invoke(wnd, new object[] { cm, new ContextMenuOpenEventArgs(cm) });
                    }
                }
            }
        }

        public static void ToggleControls(this IEnumerable<object> controls)
        {
            foreach (var c in controls)
                c.SetProperty("IsEnabled", c.GetProperty<bool>("IsEnabled"));
        }

        public static bool AllEqual<T>(this IEnumerable<T> en)
        {
            var arr = en.ToArray();
            return arr.All(el => Equals(el, arr.First()));
        }

        public static TilesMenu TilesMenu(this Panel spMenu, bool isFullSize, int resizeValue, Color mouseOverColor, Color mouseOutColor, Color resizeMouseOverColor, Color resizeMouseOutColor)
        {
            return new TilesMenu(spMenu, isFullSize, resizeValue, mouseOverColor, mouseOutColor, resizeMouseOverColor, resizeMouseOutColor);
        }

        public static void Highlight<T>(this IEnumerable<T> controls, Color color) where T : Control
        {
            controls.ForEach(control => control.Highlight(color));
        }

        public static DataGridTextColumn ByDataMemberName(this IEnumerable<DataGridTextColumn> columns, string dataMemberName)
        {
            return columns.Single(c => string.Equals(c.DataMemberName(), dataMemberName, StringComparison.Ordinal));
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

        public static IQueryable<Tipster> ButSelf(this IQueryable<Tipster> tipsters)
        {
            var self = Tipster.Me();
            return tipsters.Where(t => t.Name != self.Name);
        }

        #endregion

        #region - DbSet Extensions

        public static void RemoveBy<TSource>(this DbSet<TSource> dbSet, Expression<Func<TSource, bool>> selector) where TSource : class
        {
            dbSet.RemoveRange(dbSet.Where(selector));
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
        
        public static void RemoveUnused(this DbSet<Website> dbWebsites, IQueryable<Tipster> dbTipsters)
        {
            var websiteIdsWoLogin = dbWebsites.Where(w => w.LoginId == null).Select(w => w.Id).ToArray();
            var usedWebsiteIds = dbTipsters.ButSelf().Select(t => t.WebsiteId).Where(wid => wid != null).Cast<int>().Distinct().ToArray();
            var notUsedWebsiteIds = websiteIdsWoLogin.Except(usedWebsiteIds);
            dbWebsites.RemoveByMany(ws => ws.Id, notUsedWebsiteIds);
        }

        #endregion

        #region - ICollection Extensions

        public static T[] ToArray<T>(this ICollection<T> col)
        {
            var array = new T[col.Count];
            col.CopyTo(array, 0);
            return array;
        }

        public static object[] ToArray(this ICollection col)
        {
            var array = new object[col.Count];
            col.CopyTo(array, 0);
            return array;
        }

        public static int Index<T>(this ICollection<T> col, T item)
        {
            return Array.IndexOf(col.ToArray(), item);
        }

        public static int Index(this ICollection col, object item)
        {
            return Array.IndexOf(col.ToArray(), item);
        }

        public static int RemoveAll<T>(this ICollection<T> collection, Predicate<T> match)
        {
            var array = (from item in collection
                where match(item)
                select item).ToArray();
            var array2 = array;
            foreach (var item2 in array2)
                collection.Remove(item2);
            return array.Length;
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            items.ForEach(collection.Add);
        }

        #endregion

        #region - ItemCollection Extensions

        public static T[] ToArray<T>(this ItemCollection col)
        {
            var array = new T[col.Count];
            col.CopyTo(array, 0);
            return array;
        }

        public static void AddRange(this ItemCollection list, IEnumerable items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            foreach (var current in items)
                list.Add(current);
        }

        public static object Last(this ItemCollection col)
        {
            return col[col.Count - 1];
        }

        #endregion

        #region - TelerikGridViewColumnCollection Extensions

        public static GridViewColumn Last(this GridViewColumnCollection col)
        {
            return col[col.Count - 1];
        }

        #endregion

        #region - UIElementCollection Extensions

        public static UIElementCollection ReplaceAll<T>(this UIElementCollection col, IEnumerable<T> en) where T : UIElement
        {
            var list = en.ToList();
            col.RemoveAll();
            col.AddRange(list);
            return col;
        }

        public static void RemoveAll(this UIElementCollection collection)
        {
            while (collection.Count != 0)
                collection.RemoveAt(0);
        }

        #endregion

        #region - Dictionary Extensions

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueProvider)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value
                : defaultValueProvider();
        }

        public static TValue VorDef<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return GetValueOrDefault(dictionary, key);
        }

        public static TValue VorDef<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return GetValueOrDefault(dictionary, key, defaultValue);
        }

        public static TValue VorDef<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueProvider)
        {
            return GetValueOrDefault(dictionary, key, defaultValueProvider);
        }

        public static TValue GetValueOrNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out TValue val);
            return val;
        }

        public static TValue VorN<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            return GetValueOrNull(dictionary, key);
        }

        public static TValue VorN_Ts<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            lock (_lock)
                return GetValueOrNull(dictionary, key);
        }

        public static void V_Ts<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue val)
        {
            lock (_lock)
                dictionary[key] = val;
        }

        public static void AddIfNotNull<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TValue : class
        {
            if (value != null)
                dictionary.Add(key, value);
        }

        public static void AddIfNotNullAnd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, bool? condition, TKey key, TValue value) where TValue : class
        {
            if (value != null && condition == true)
                dictionary.Add(key, value);
        }

        public static string ToQueryString(this Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count <= 0) return "";
            var sb = new StringBuilder();
            foreach (var item in parameters)
                sb.Append($"&{item.Key.ToUrlEncoded()}={item.Value.ToUrlEncoded()}");
            return sb.ToString().Skip(1);
        }

        public static bool Exists<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
        {
            return dict.VorN(key) != null;
        }

        public static void RemoveIfExists<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
        {
            if (dict.Exists(key))
                dict.Remove(key);
        }


        #endregion

        #region - Observable Collection Extensions

        public static ObservableCollection<T> ReplaceAll<T>(this ObservableCollection<T> obsCol, IEnumerable<T> newEnumerable)
        {
            var list = newEnumerable.ToList();
            obsCol.RemoveAll();
            obsCol.AddRange(list);
            return obsCol;
        }

        public static ObservableCollection<T> ReplaceAll<T>(this ObservableCollection<T> obsCol, T newEl)
        {
            return obsCol.ReplaceAll(newEl.ToEnumerable());
        }

        #endregion

        #endregion

        #region DbContext Extensions



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

        public static IEnumerable<T> FindLogicalDescendants<T>(this DependencyObject depObj) where T : DependencyObject
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

                foreach (var childOfChild in FindLogicalDescendants<T>(child))
                    yield return childOfChild;
            }
        }
        
        public static IEnumerable<Control> FindLogicalDescendants<T1, T2>(this DependencyObject depObj)
            where T1 : DependencyObject
            where T2 : DependencyObject
        {
            return ConcatMany(FindLogicalDescendants<T1>(depObj).Cast<Control>(), FindLogicalDescendants<T2>(depObj).Cast<Control>());
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

        public static T FindLogicalAncestor<T>(this DependencyObject child) where T : DependencyObject
        {
            while (true)
            {
                var parentObject = LogicalTreeHelper.GetParent(child);
                if (parentObject == null) return null;
                if (parentObject is T parent) return parent;
                child = parentObject;
            }
        }

        public static T FindLogicalAncestor<T>(this DependencyObject child, Func<T, bool> condition) where T : DependencyObject
        {
            while (true)
            {
                var parentObject = LogicalTreeHelper.GetParent(child);
                if (parentObject == null) return null;
                if (parentObject is T parent && condition(parent)) return parent;
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

            var bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            var rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.IntersectsWith(bounds);
        }

        public static bool HasContextMenu(this FrameworkElement el)
        {
            return ContextMenusManager.ContextMenus.VorN(el)?.IsCreated == true;
        }

        public static ContextMenu ContextMenu(this FrameworkElement el)
        {
            return ContextMenusManager.ContextMenu(el);
        }
       
        public static Task AnimateAsync(this FrameworkElement fwElement, PropertyPath propertyPath, AnimationTimeline animation)
        {
            lock (_lock)
            {
                var tcs = new TaskCompletionSource<bool>();
                var storyBoard = new Storyboard();
                void storyBoard_Completed(object s, EventArgs e) => tcs.TrySetResult(true);

                var isSbInDict = _storyBoards.VorN(fwElement) != null;
                if (isSbInDict)
                {
                    _storyBoards[fwElement].Stop(fwElement);
                    _storyBoards.Remove(fwElement);
                    _storyBoards.Add(fwElement, storyBoard);
                }
                Storyboard.SetTarget(animation, fwElement);
                Storyboard.SetTargetProperty(animation, propertyPath);
                storyBoard.Children.Add(animation);
                storyBoard.Completed += storyBoard_Completed;

                storyBoard.Begin(fwElement, true);
                return tcs.Task;
            }
        }

        public static Task AnimateAsync(this FrameworkElement fwElement, DependencyProperty dp, AnimationTimeline animation)
        {
            return AnimateAsync(fwElement, new PropertyPath(dp), animation);
        }

        public static Task AnimateAsync(this FrameworkElement fwElement, string propertyPath, AnimationTimeline animation)
        {
            return AnimateAsync(fwElement, new PropertyPath(propertyPath), animation);
        }

        public static void Animate(this FrameworkElement fwElement, PropertyPath propertyPath, AnimationTimeline animation, EventHandler callback = null)
        {
            lock (_lock)
            {
                var storyBoard = new Storyboard();

                var isSbInDict = _storyBoards.VorN(fwElement) != null;
                if (isSbInDict)
                {
                    _storyBoards[fwElement].Stop(fwElement);
                    _storyBoards.Remove(fwElement);
                    _storyBoards.Add(fwElement, storyBoard);
                }
                Storyboard.SetTarget(animation, fwElement);
                Storyboard.SetTargetProperty(animation, propertyPath);
                storyBoard.Children.Add(animation);
                if (callback != null)
                    storyBoard.Completed += callback;

                storyBoard.Begin(fwElement, true);
            }
        }

        public static void Animate(this FrameworkElement fwElement, DependencyProperty dp, AnimationTimeline animation, EventHandler callback = null)
        {
            Animate(fwElement, new PropertyPath(dp), animation, callback);
        }

        public static void Animate(this FrameworkElement fwElement, string propertyPath, AnimationTimeline animation, EventHandler callback = null)
        {
            Animate(fwElement, new PropertyPath(propertyPath), animation, callback);
        }

        public static void SlideShow(this Panel c)
        {
            if (!_panelAnimations.VorN_Ts(c.Name + "IsOpened"))
                c.SlideToggle();
        }

        public static void SlideHide(this Panel c)
        {
            if (_panelAnimations.VorN_Ts(c.Name + "IsOpened"))
                c.SlideToggle();
        }

        public static async void SlideToggle(this Panel c)
        {
            var strIsOpened = c.Name + "IsOpened";
            var isOpened = _panelAnimations.VorN_Ts(strIsOpened);
            var slideGrid = c.HasSlideGrid() ? c.GetSlideGrid() : c.CreateAndAddSlideGrid(!isOpened ? 0 : c.Width);
            _panelAnimations.V_Ts(strIsOpened, !isOpened);
            var slideAni = new DoubleAnimation(isOpened ? 0 : c.Width, new Duration(TimeSpan.FromMilliseconds(500)));

            if (isOpened) // jeśli otwarty na początku
                c.Visibility = Visibility.Hidden;

            await slideGrid.AnimateAsync(FrameworkElement.WidthProperty, slideAni);
            c.RemoveSlideGrid();

            if (_panelAnimations.VorN_Ts(strIsOpened)) // jeśli otwarty po animacji (w dowolnej kolejności)
                c.Visibility = Visibility.Visible;
        }

        private static bool HasSlideGrid(this Panel c)
        {
            lock (_lock)
            {
                var parentGrid = c.FindLogicalAncestor<Grid>();
                return parentGrid.Children.OfType<Grid>().Any(grid => grid.Name == "gridSlide" + c.Name);
            }
        }

        private static Grid GetSlideGrid(this Panel c)
        {
            lock (_lock)
            {
                var parentGrid = c.FindLogicalAncestor<Grid>();
                return parentGrid.Children.OfType<Grid>().Single(grid => grid.Name == "gridSlide" + c.Name);
            }
        }

        private static Grid CreateSlideGrid(this Panel c, double slideGridWidth)
        {
            lock (_lock)
            {
                var phName = "gridSlide" + c.Name;
                var gridPh = new Grid
                {
                    Width = slideGridWidth,
                    Height = c.Height,
                    Background = c.Background,
                    Margin = c.Margin,
                    HorizontalAlignment = c.HorizontalAlignment,
                    VerticalAlignment = c.VerticalAlignment,
                    Name = phName
                };
                Panel.SetZIndex(gridPh, Panel.GetZIndex(c));
                return gridPh;
            }
        }

        private static Grid AddSlideGrid(this Panel c, Grid slideGrid)
        {
            lock (_lock)
            {
                var parentGrid = c.FindLogicalAncestor<Grid>();
                parentGrid.Children.Add(slideGrid);
                return slideGrid;
            }
        }

        private static Grid CreateAndAddSlideGrid(this Panel c, double slideGridWidth)
        {
             return c.AddSlideGrid(c.CreateSlideGrid(slideGridWidth));
        }

        private static void RemoveSlideGrid(this Panel c)
        {
            lock (_lock)
            {
                var parentGrid = c.FindLogicalAncestor<Grid>();
                var slideGrid = parentGrid.Children.OfType<Grid>().SingleOrDefault(grid => grid.Name == "gridSlide" + c.Name);
                if (slideGrid != null)
                    parentGrid.Children.Remove(slideGrid);
            }
        }

        public static int ZIndex(this FrameworkElement fe)
        {
            return Panel.GetZIndex(fe);
        }

        public static void ZIndex(this FrameworkElement fe, int zINdex)
        {
            Panel.SetZIndex(fe, zINdex);
        }

        public static void Position(this FrameworkElement control, Point pos)
        {
            Canvas.SetLeft(control, pos.X);
            Canvas.SetTop(control, pos.Y);
        }

        public static void PositionX(this FrameworkElement control, double posX)
        {
            Canvas.SetLeft(control, posX);
        }

        public static void PositionY(this FrameworkElement control, double posY)
        {
            Canvas.SetTop(control, posY);
        }

        public static Point Position(this FrameworkElement control)
        {
            control.Refresh();
            return new Point(Canvas.GetLeft(control), Canvas.GetTop(control));
        }

        public static double PositionX(this FrameworkElement control)
        {
            control.Refresh();
            return Canvas.GetLeft(control);
        }

        public static double PositionY(this FrameworkElement control)
        {
            control.Refresh();
            return Canvas.GetTop(control);
        }

        public static void Margin(this FrameworkElement control, Point pos)
        {
            var initOpacity = control.Opacity;
            var initVisibility = control.Visibility;
            control.Opacity = 0;
            control.Visibility = Visibility.Visible;
            control.Margin = new Thickness(pos.X, pos.Y, control.Margin.Right, control.Margin.Bottom);
            control.Visibility = initVisibility;
            control.Opacity = initOpacity;
        }

        public static void MarginX(this FrameworkElement control, double posX)
        {
            control.Margin = new Thickness(posX, control.Margin.Top, control.Margin.Right, control.Margin.Bottom);
        }

        public static void MarginY(this FrameworkElement control, double posY)
        {
            control.Margin = new Thickness(control.Margin.Left, posY, control.Margin.Right, control.Margin.Bottom);
        }

        public static Point MarginPosition(this FrameworkElement control)
        {
            control.Refresh();
            return new Point(control.Margin.Left, control.Margin.Top);
        }

        public static double MarginX(this FrameworkElement control)
        {
            control.Refresh();
            return control.Margin.Left;
        }

        public static double MarginY(this FrameworkElement control)
        {
            control.Refresh();
            return control.Margin.Top;
        }


        public static void Refresh(this FrameworkElement control)
        {
            control.Dispatcher.Invoke(DispatcherPriority.Render, _emptyDelegate);
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
            if (text != placeholder && !String.IsNullOrWhiteSpace(text) && !force)
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
            thisTxtBox.Text = String.Empty;
        }

        public static bool IsNullWhitespaceOrTag(this TextBox txt)
        {
            return txt.Text?.IsNullWhiteSpaceOrDefault(txt.Tag?.ToString() ?? "") ?? false;
        }

        #endregion

        #region ComboBox Extensions

        public static void SelectByCustomId(this ComboBox rddl, int id)
        {
            rddl.SelectedItem = rddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
        }

        public static T SelectedEnumValue<T>(this ComboBox rddl)
        {
            var selectedItem = (DdlItem)rddl.SelectedItem;
            var enumType = typeof(T);

            var value = (Enum)Enum.ToObject(enumType, selectedItem.Index);
            if (Enum.IsDefined(enumType, value) == false)
                throw new NotSupportedException($"Nie można przekonwertować wartości na podany typ: {enumType}");

            return (T)(object)value;
        }

        #endregion

        #region ListBox Extensions

        public static void SelectByCustomId(this ListBox mddl, int id)
        {
            var item = mddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
            mddl.SelectedItems.Add(item);
        }

        public static void SelectAll(this ListBox mddl)
        {
            mddl.UnselectAll();
            var items = mddl.Items.ToArray();
            foreach (var item in items)
                mddl.SelectedItems.Add(item);
        }

        public static void UnselectAll(this ListBox mddl)
        {
            var selectedItems = mddl.SelectedItems.ToArray();
            foreach (var item in selectedItems)
                mddl.SelectedItems.Remove(item);
        }

        public static void SelectByCustomIds(this ListBox mddl, IEnumerable<int> ids)
        {
            var ddlItems = mddl.ItemsSource.Cast<DdlItem>().Where(i => ids.Any(id => i.Index == id)).ToList();
            foreach (var item in ddlItems)
                mddl.SelectedItems.Add(item);
        }

        public static int[] SelectedCustomIds(this ListBox mddl)
        {
            return mddl.SelectedItems.Cast<DdlItem>().Select(i => i.Index).ToArray();
        }

        public static void ScrollToStart(this ListBox lv, bool selectLast = false)
        {
            if (lv.Items.Count > 0)
                lv.GetScrollViewer().ScrollToTop();
        }

        public static void ScrollToEnd(this ListBox lv, bool selectLast = false)
        {
            if (lv.Items.Count > 0)
                lv.GetScrollViewer().ScrollToBottom();
        }


        #endregion

        #region DataGrid Extensions
        
        public static void ScrollToEnd(this DataGrid gv)
        {
            if (gv.Items.Count > 0)
                gv.ScrollIntoView(gv.Items.Last());
        }

        public static void ScrollToStart(this DataGrid gv)
        {
            if (gv.Items.Count > 0)
                gv.ScrollIntoView(gv.Items[0]);
        }

        public static void ScrollTo<T>(this DataGrid gv, T item)
        {
            if (gv.Items.Count > 0)
            {
                var sv = gv.GetScrollViewer();
                var items = gv.Items.Cast<T>().ToArray();
                var itemToScrollTo = items.Single(i => Equals(i, item));
                sv.ScrollToVerticalOffset(items.Index(itemToScrollTo));
            }
        }

        public static void SetSelecteditemsSource<T>(this DataGrid gv, ObservableCollection<T> items)
        {
            GridViewSelectionUtilities.SetSelectedItems(gv, items);
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

        #region Point

        public static Point Translate(this Point p, double x, double y)
        {
            return new Point(p.X + x, p.Y + y);
        }

        public static Point Translate(this Point p, Point t)
        {
            return new Point(p.X + t.X, p.Y + t.Y);
        }

        public static Point TranslateX(this Point p, double x)
        {
            return new Point(p.X + x, p.Y);
        }

        public static Point TranslateY(this Point p, double y)
        {
            return new Point(p.X, p.Y + y);
        }

        public static bool IsOnScreen(this Point absolutePoint)
        {
            var screenRect = new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            return screenRect.Contains(absolutePoint);
        }
        
        public static bool IsXOnScreen(this Point absolutePoint)
        {
            return absolutePoint.X >= 0 && absolutePoint.X <= SystemParameters.PrimaryScreenWidth;
        }

        public static bool IsYOnScreen(this Point absolutePoint)
        {
            return absolutePoint.Y >= 0 && absolutePoint.Y <= SystemParameters.PrimaryScreenHeight;
        }

        public static Point Select(this Point absolutePoint, Func<Point, Point> selector)
        {
            return selector(absolutePoint);
        }

        public static Point ToPoint(this DPoint p)
        {
            return new Point(p.X, p.Y);
        }

        public static DPoint ToDrawingPoint(this Point p)
        {
            return new DPoint(p.X.ToInt(), p.Y.ToInt());
        }

        #endregion

        #region Size Extensions

        public static Size ToSize(this DSize s)
        {
            return new Size(s.Width, s.Height);
        }

        #endregion

        #region Control Extensions

        public static void Highlight(this Control control, Color color)
        {
            control.AnimateChangeColor(color);
        }

        public static void Unhighlight(this Control control, Color defaultColor)
        {
            control.AnimateChangeColor(defaultColor);
        }

        private static async void AnimateChangeColor(this Control control, Color color)
        {
            var colorAni = new ColorAnimation(color, new Duration(TimeSpan.FromMilliseconds(500)));
            await control.AnimateAsync("(Panel.Background).(SolidColorBrush.Color)", colorAni);
        }

        public static T CloneControl<T>(this T control, string newName) where T : Control
        {
            var sb = new StringBuilder();
            var writer = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                OmitXmlDeclaration = true,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
            });
            var mgr = new XamlDesignerSerializationManager(writer) { XamlWriterMode = XamlWriterMode.Expression };
            XamlWriter.Save(control, mgr);
            var sr = new StringReader(sb.ToString());
            var xmlReader = XmlReader.Create(sr);
            var clonedControl = (T)XamlReader.Load(xmlReader);
            if (!String.IsNullOrWhiteSpace(newName))
                clonedControl.Name = newName;
            return clonedControl;
        }

        public static Size Size(this Control control)
        {
            control.Refresh();
            return new Size(control.Width, control.Height);
        }
        
        #endregion

        #region Panel Extensions

        public static void ShowLoader(this Panel control)
        {
            var rect = new Rectangle
            {
                Margin = new Thickness(0),
                Fill = new SolidColorBrush(Color.FromArgb(192, 0, 0, 0)),
                Name = "prLoaderContainer"
            };

            var loader = new ProgressRing
            {
                Foreground = (Brush)control.FindLogicalAncestor<Window>().FindResource("AccentColorBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 80,
                Height = 80,
                IsActive = true,
                Name = "prLoader"
            };

            var status = new TextBlock
            {
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 18,
                Margin = new Thickness(0, 125, 0, 0),
                Text = "Ładowanie...",
                Name = "prLoaderStatus"
            };

            var zIndex = control.FindLogicalDescendants<FrameworkElement>().Concat(control).MaxBy(Panel.GetZIndex).ZIndex();
            Panel.SetZIndex(rect, zIndex + 1);
            Panel.SetZIndex(loader, zIndex + 1);
            Panel.SetZIndex(status, zIndex + 1);

            control.Children.AddRange(new FrameworkElement[] { rect, loader, status });
        }

        public static void HideLoader(this Panel control)
        {
            var loaders = control.FindLogicalDescendants<FrameworkElement>().Where(c => c.Name == "prLoader").ToArray();
            var loaderContainers = control.FindLogicalDescendants<FrameworkElement>().Where(c => c.Name == "prLoaderContainer").ToArray();
            var loaderStatuses = control.FindLogicalDescendants<FrameworkElement>().Where(c => c.Name == "prLoaderStatus").ToArray();
            var loaderParts = ArrayUtils.ConcatMany(loaders, loaderContainers, loaderStatuses);

            loaderParts.ForEach(lp => control.Children.Remove(lp));
        }

        public static bool HasLoader(this Panel control)
        {
            return control.FindLogicalDescendants<Rectangle>().Any(r => r.Name == "prLoaderContainer");
        }

        #endregion

        #region DataGridTextColumn Extesnions

        public static string DataMemberName(this DataGridTextColumn column)
        {
            return ((Binding) column.Binding).Path.Path;
        }

        #endregion

        #region WIndow Extensions

        public static void CenterOnScreen(this Window wnd)
        {
            wnd.Position(PointUtils.CenteredWindowTopLeft(wnd.Size()));
        }

        #endregion

        #region T Extensions

        public static IEnumerable<T> ToEnumerable<T>(this T item)
        {
            yield return item;
        }

        #endregion

        #region Object Extensions

        public static T ToEnum<T>(this object value) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T must be an Enum");
            return (T)Enum.Parse(typeof(T), value.ToString().RemoveMany(" ", "-"), true);
        }

        public static int? ToIntN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToInt32(obj);
            if (obj.GetType().IsEnum) return (int) obj;
            return Int32.TryParse(obj.ToDoubleN()?.Round().ToString().BeforeFirst("."), NumberStyles.Any, Culture, out int val) ? val : (int?) null;
        }

        public static int ToInt(this object obj)
        {
            var intN = obj.ToIntN();
            if (intN != null) return (int)intN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static uint? ToUIntN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToUInt32(obj);
            if (obj.GetType().IsEnum) return (uint) obj;
            return UInt32.TryParse(obj.ToDoubleN()?.Round().ToString().BeforeFirst("."), NumberStyles.Any, Culture, out uint val) ? val : (uint?)null;
        }

        public static uint ToUInt(this object obj)
        {
            var uintN = obj.ToUIntN();
            if (uintN != null) return (uint)uintN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static long? ToLongN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToInt64(obj);
            if (obj.GetType().IsEnum) return (long) obj;
            return Int64.TryParse(obj.ToDoubleN()?.Round().ToString().BeforeFirst("."), NumberStyles.Any, Culture, out long val) ? val : (long?)null;
        }

        public static long ToLong(this object obj)
        {
            var longN = obj.ToLongN();
            if (longN != null) return (long)longN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static double? ToDoubleN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToDouble(obj);
            return Double.TryParse(obj.ToString().Replace(",", "."), NumberStyles.Any, Culture, out double tmpvalue) ? tmpvalue : (double?)null;
        }

        public static double ToDouble([NotNull] this object obj)
        {
            var doubleN = obj.ToDoubleN();
            if (doubleN != null) return (double)doubleN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static decimal? ToDecimalN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return Convert.ToDecimal(obj);
            return Decimal.TryParse(obj.ToString().Replace(",", "."), NumberStyles.Any, Culture, out decimal tmpvalue) ? tmpvalue : (decimal?)null;
        }

        public static decimal ToDecimal([NotNull] this object obj)
        {
            var decimalN = obj.ToDecimalN();
            if (decimalN != null) return (decimal)decimalN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static DateTime? ToDateTimeN(this object obj)
        {
            return DateTime.TryParse(obj?.ToString(), out DateTime tmpvalue) ? tmpvalue : (DateTime?)null;
        }

        public static DateTime ToDateTime(this object obj)
        {
            var DateTimeN = obj.ToDateTimeN();
            if (DateTimeN != null) return (DateTime)DateTimeN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static DateTime ToDateTimeExact(this object obj, string format)
        {
            return DateTime.ParseExact(obj?.ToString(), format, CultureInfo.InvariantCulture);
        }

        public static bool? ToBoolN(this object obj)
        {
            if (obj == null) return null;
            if (obj is bool) return (bool) obj;
            if (obj.ToIntN() != null) return Convert.ToBoolean(obj.ToInt());
            return Boolean.TryParse(obj.ToString(), out bool tmpvalue) ? tmpvalue : (bool?)null;
        }

        public static bool ToBool(this object obj)
        {
            var boolN = obj.ToBoolN();
            if (boolN != null) return (bool)boolN;
            throw new ArgumentNullException(nameof(obj));
        }

        public static T GetProperty<T>(this object src, string propName)
        {
            return (T)src.GetType().GetProperty(propName)?.GetValue(src, null);
        }

        public static void SetProperty<T>(this object src, string propName, T propValue)
        {
            src.GetType().GetProperty(propName)?.SetValue(src, propValue);
        }

        public static T GetField<T>(this object src, string fieldName)
        {
            return (T)src.GetType().GetField(fieldName)?.GetValue(src);
        }

        public static void SetField<T>(this object src, string fieldName, T fieldValue)
        {
            src.GetType().GetField(fieldName)?.SetValue(src, fieldValue);
        }



        #endregion
    }
}
