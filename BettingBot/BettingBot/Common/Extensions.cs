using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using System.Web;
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
using BettingBot.Common.UtilityClasses;
using BettingBot.Properties;
using BettingBot.Source;
using BettingBot.Source.Clients.Api.FootballData.Responses;
using BettingBot.Source.Clients.Selenium;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using HtmlAgilityPack;
using Convert = System.Convert;
using CustomMenuItem = BettingBot.Common.UtilityClasses.CustomMenuItem;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using RestSharp;
using ContextMenu = BettingBot.Common.UtilityClasses.ContextMenu;
using Point = System.Windows.Point;
using DPoint = System.Drawing.Point;
using Size = System.Windows.Size;
using DSize = System.Drawing.Size;
using Parameter = BettingBot.Common.UtilityClasses.Parameter;
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
        public static readonly Action EmptyDelegate = delegate { };

        #endregion

        #region T Extensions

        public static bool EqualsAny<T>(this T o, params T[] os)
        {
            return os.Any(s => s.Equals(o));
        }

        public static bool In<T>(this T o, params T[] os)
        {
            return o.EqualsAny(os);
        }

        public static TDest MapTo<TDest, TSource>(this TSource srcEl)
            where TSource : class
            where TDest : class
        {
            var destEl = (TDest)Activator.CreateInstance(typeof(TDest), new object[] { });
            Mapper.Map(srcEl, destEl);
            return destEl;
        }

        public static T MapToSameType<T>(this T srcEl) where T : class
        {
            var destEl = (T)Activator.CreateInstance(typeof(T), new object[] { });
            Mapper.Map(srcEl, destEl);
            return destEl;
        }

        public static T Copy<T>(this T src) where T : class => src.MapToSameType();

        #endregion

        #region String Extensions

        public static bool HasValueBetween(this string str, string start, string end)
        {
            return !String.IsNullOrEmpty(str) && !String.IsNullOrEmpty(start) && !String.IsNullOrEmpty(end) &&
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
            return string.IsNullOrWhiteSpace(str) || str == defVal;
        }
        
        public static string Remove(this string str, string substring)
        {
            return str.Replace(substring, "");
        }

        public static string RemoveMany(this string str, params string[] substrings)
        {
            return substrings.Aggregate(str, (current, substring) => current.Remove(substring));
        }
        
        public static string[] Split(this string str, string separator, bool includeSeparator = false, StringSplitOptions? options = null)
        {
            var split = str.Split(new[] { separator }, options ?? StringSplitOptions.RemoveEmptyEntries);

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

        public static string[] SameWords(this string str, string otherStr, int minWordLength, Func<string, bool> condition = null)
        {
            return str.SameWords(otherStr, false, " ", minWordLength, condition);
        }
        
        public static string[] SameWords(this string str, string otherStr, bool caseSensitive = false, string splitBy = " ", int minWordLength = 1, Func<string, bool> condition = null)
        {
            if (!caseSensitive)
            {
                str = str.ToLower();
                otherStr = otherStr.ToLower();
            }
            
            var str1Arr = str.Split(splitBy).Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
            if (condition != null)
                str1Arr = str1Arr.Where(condition).ToArray();
            var str2Arr = otherStr.Split(splitBy).Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
            if (condition != null)
                str1Arr = str1Arr.Where(condition).ToArray();
            var intersection = str1Arr.Intersect(str2Arr).Where(w => w.Length >= minWordLength);
            return intersection.ToArray();
        }

        public static string[] SameWords(this string str, string[] otherStrings, bool casaeSensitive, string splitBy = " ", int minWordLength = 1, Func<string, bool> condition = null)
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

        public static bool HasSameWords(this string str, string otherStr, int minWordLength, Func<string, bool> condition)
        {
            return str.SameWords(otherStr, false, " ", minWordLength, condition).Any();
        }

        public static bool HasSameWords(this string str, string otherStr, bool caseSensitive = false, string splitBy = " ", int minWordLength = 1, Func<string, bool> condition = null)
        {
            return str.SameWords(otherStr, caseSensitive, splitBy, minWordLength, condition).Any();
        }

        public static bool HasSameWords(this string str, string[] otherStrings, bool caseSensitive, string splitBy = " ", int minWordLength = 1, Func<string, bool> condition = null)
        {
            return str.SameWords(otherStrings, caseSensitive, splitBy, minWordLength, condition).Any();
        }

        public static bool HasSameWords(this string str, params string[] otherStrings)
        {
            return str.SameWords(otherStrings, false, " ", 1, null).Any();
        }
        
        public static bool IsDouble(this string str)
        {
            return str.ToDoubleN() != null;
        }

        public static bool StartsWithAny(this string str, params string[] strings)
        {
            return strings.Any(str.StartsWith);
        }

        public static bool StartsWithAnyIgnoreCase(this string str, params string[] strings)
        {
            return strings.Select(w => w.ToLower()).Any(str.ToLower().StartsWith);
        }

        public static bool StartsWithIgnoreCase(this string str, string startWith)
        {
            return str.ToLower().StartsWith(startWith.ToLower());
        }

        public static bool EndsWithAny(this string str, params string[] strings)
        {
            return strings.Select(s => s.ToLower()).Any(str.ToLower().EndsWith);
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
            if (String.IsNullOrWhiteSpace(str))
                return null;
            return DomainName.TryParse(new Uri(str).Host, out DomainName completeDomain) ? completeDomain.SLD : "";
        }

        public static string AfterFirst(this string str, string substring)
        {
            if (str == null) return null;
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
            if (str == null) return null;
            if (!String.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).First();
            return str;
        }

        public static string AfterLast(this string str, string substring)
        {
            if (str == null) return null;
            if (!String.IsNullOrEmpty(substring) && str.Contains(substring))
                return str.Split(substring).Last();
            return str;
        }

        public static string BeforeLast(this string str, string substring)
        {
            if (str == null) return null;
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

        public static HtmlNode HtmlRoot(this string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode;
        }

        public static Dictionary<string, string> QueryStringToDictionary(this string queryString)
        {
            var nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static bool EqIgnoreCase(this string str, string ostr)
        {
            return String.Equals(str, ostr, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqAnyIgnoreCase(this string str, params string[] os)
        {
            return os.Any(s => s.EqIgnoreCase(str));
        }

        public static bool EqAnyIgnoreCase(this string str, IEnumerable<string> os)
        {
            return os.Any(s => s.EqIgnoreCase(str));
        }
        
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static JToken ToJToken(this string json)
        {
            JsonReader jReader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            return JToken.Load(jReader);
        }

        public static JObject ToJObject(this string json)
        {
            JsonReader jReader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            return JObject.Load(jReader);
        }

        public static JArray ToJArray(this string json)
        {
            JsonReader jReader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            return JArray.Load(jReader);
        }

        public static MatchStatus ToMatchStatus(this string matchStatus)
        {
            return MatchConverter.ToMatchStatus(matchStatus);
        }

        public static string EnsureSuffix(this string str, string suffix)
        {
            if (!str.EndsWith(suffix))
                str += suffix;
            return str;
        }

        public static bool ContainsAny(this string str, IEnumerable<string> strings)
        {
            var lStr = str.ToLower();
            return strings.Select(s => s.ToLower()).Any(lStr.Contains);
        }

        public static bool ContainsAll(this string str, IEnumerable<string> strings)
        {
            var lStr = str.ToLower();
            return strings.Select(s => s.ToLower()).All(lStr.Contains);
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

        public static bool BetweenExcl(this double d, double greaterThan, double lesserThan)
        {
            TUtils.SwapIf((gt, lt) => gt > lt, ref greaterThan, ref lesserThan);
            return d > greaterThan && d < lesserThan;
        }

        public static bool Between(this double d, double greaterThan, double lesserThan)
        {
            TUtils.SwapIf((gt, lt) => gt > lt, ref greaterThan, ref lesserThan);
            return d >= greaterThan && d <= lesserThan;
        }
        
        public static double Round(this double number, int digits = 0)
        {
            return Math.Round(number, digits);
        }

        public static UnixTimestamp ToUnixTimestamp(this double d) => new UnixTimestamp(d);

        public static ExtendedTime ToExtendedTime(this double unixTimestamp, TimeZoneKind timeZone = TimeZoneKind.UTC)
        {
            return new ExtendedTime(unixTimestamp, timeZone);
        }

        public static double Abs(this double d)
        {
            return Math.Abs(d);
        }

        #endregion

        #region Int Extensions

        public static bool BetweenExcl(this int d, int greaterThan, int lesserThan)
        {
            TUtils.SwapIf((gt, lt) => gt > lt, ref greaterThan, ref lesserThan);
            return d > greaterThan && d < lesserThan;
        }

        public static bool Between(this int d, int greaterThan, int lesserThan)
        {
            TUtils.SwapIf((gt, lt) => gt > lt, ref greaterThan, ref lesserThan);
            return d >= greaterThan && d <= lesserThan;
        }

        public static T ToEnum<T>(this int n) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T must be an Enum");
            return (T)(object)n;
        }

        #endregion

        #region Long Extensions 

        public static ExtendedTime ToExtendedTime(this long l) => new ExtendedTime(l);

        public static UnixTimestamp ToUnixTimestamp(this long l) => new UnixTimestamp(l);

        #endregion

        #region DateTime Extensions

        public static string MonthName(this DateTime date)
        {
            return LocalizationManager.Culture.DateTimeFormat.GetMonthName(date.Month);
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

        public static DateTime As(this DateTime dateTime, DateTimeKind kind)
        {
            return DateTime.SpecifyKind(dateTime, kind);
        }

        public static ExtendedTime ToExtendedTime(this DateTime dt, TimeZoneKind tz = TimeZoneKind.UTC)
        {
            return new ExtendedTime(dt, tz);
        }

        public static UnixTimestamp ToUnixTimestamp(this DateTime dateTime)
        {
            return new UnixTimestamp(dateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }

        public static bool BetweenExcl(this DateTime d, DateTime greaterThan, DateTime lesserThan)
        {
            TUtils.SwapIf((gt, lt) => gt > lt, ref greaterThan, ref lesserThan);
            return d > greaterThan && d < lesserThan;
        }

        public static bool Between(this DateTime d, DateTime greaterThan, DateTime lesserThan)
        {
            TUtils.SwapIf((gt, lt) => gt > lt, ref greaterThan, ref lesserThan);
            return d >= greaterThan && d <= lesserThan;
        }

        public static DateTime SubtractDays(this DateTime dt, int days)
        {
            return dt.AddDays(-days);
        }

        public static DateTime SubtractYears(this DateTime dt, int years)
        {
            return dt.AddYears(-years);
        }

        #endregion

        #region TimeSpan Extensions

        public static TimeSpan Abs(this TimeSpan ts)
        {
            return ts < TimeSpan.Zero ? -ts : ts;
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

        public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, params T[] arr)
        {
            return enumerable.Except(arr.AsEnumerable());
        }

        public static bool ContainsAny(this string str, params string[] strings)
        {
            return str.ContainsAny(strings.AsEnumerable());
        }

        public static bool ContainsAll(this string str, params string[] strings)
        {
            return str.ContainsAll(strings.AsEnumerable());
        }

        #endregion

        #region - List Extensions

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
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

        public static T[] IListToArray<T>(this IList<T> list)
        {
            var array = new T[list.Count];
            list.CopyTo(array, 0);
            return array;
        }

        public static object[] IListToArray(this IList list)
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

        public static void RemoveRange(this IList list, IEnumerable items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            foreach (var current in items)
                list.Remove(current);
        }


        public static void RemoveIfExists<T>(this List<T> list, T item)
        {
            if (list.Contains(item))
                list.Remove(item);
        }

        public static List<DbLeague> WithDiscipline(this List<DbLeague> dbLeagues, DisciplineType discipline)
        {
            foreach (var l in dbLeagues)
            {
                l.DisciplineId = discipline.ToInt();
                l.Discipline = new DbDiscipline
                {
                    Id = discipline.ToInt(),
                    Name = discipline.ConvertToString()
                };
            }
            return dbLeagues;
        }

        #endregion

        #region - IEnumerable Extensions

        public static string JoinAsString<T>(this IEnumerable<T> enumerable, string strBetween = "")
        {
            return enumerable.ToStringDelimitedBy(strBetween);
        }
        
        public static int Index<T>(this IEnumerable<T> en, T el)
        {
            var i = 0;
            foreach (var item in en)
            {
                if (Equals(item, el)) return i;
                i++;
            }
            return -1;
        }

        public static T LastOrNull<T>(this IEnumerable<T> enumerable)
        {
            var en = enumerable as T[] ?? enumerable.ToArray();
            return en.Any() ? en.Last() : (T)Convert.ChangeType(null, typeof(T));
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
            return columns.Single(c => String.Equals(c.DataMemberName(), dataMemberName, StringComparison.Ordinal));
        }

        public static NameValueCollection ToNameValueCollection(this IEnumerable<KeyValuePair<string, string>> en)
        {
            var nvc = new NameValueCollection();
            foreach (var q in en)
                nvc.Add(q.Key, q.Value);
            return nvc;
        }

        public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> en, bool distinct = true)
        {
            var duplicates = en.GroupBy(s => s).SelectMany(grp => grp.Skip(1));
            return distinct ? duplicates.Distinct() : duplicates;
        }

        public static List<LoginGvVM> ToLoginsGvVM(this IEnumerable<DbLogin> dbLogins)
        {
            return dbLogins.Select(l => l.ToLoginGvVM()).ToList();
        }

        public static List<WebsiteGvVM> ToWebsitesGvVM(this IEnumerable<DbWebsite> dbWebsites)
        {
            return dbWebsites.Select(l => l.ToWebsiteGvVM()).ToList();
        }

        public static List<TipsterGvVM> ToTipstersGvVM(this IEnumerable<DbTipster> dbTipsters)
        {
            return dbTipsters.Select(t => t.ToTipsterGvVM()).OrderBy(t => t.Name.ToLower()).ToList();
        }

        public static List<BetToDisplayGvVM> ToBetsToDisplayGvVM(this IEnumerable<DbBet> dbBets)
        {
            return dbBets.Select(b => b.ToBetToDisplayGvVM()).ToList();
        }

        public static List<DbLeague> ToDbLeagues(this IEnumerable<CompetitionResponse> competitions)
        {
            return competitions.Select(c => c.ToDbLeague()).ToList();
        }
        
        public static List<TDest> MapCollectionTo<TDest, TSource>(this IEnumerable<TSource> source)
            where TSource : class
            where TDest : class
        {
            return source.Select(srcEl => srcEl.MapTo<TDest, TSource>()).ToList();
        }
        
        public static List<TDest> MapCollectionToSameType<TDest>(this IEnumerable<TDest> source) where TDest : class
        {
            return source.Select(srcEl => srcEl.MapToSameType()).ToList();
        }

        public static List<T> CopyCollection<T>(this IEnumerable<T> src) where T : class => src.MapCollectionToSameType();

        public static List<DbBet> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbBet> dbBets)
        {
            return dbBets.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<DbMatch> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbMatch> dbMatches)
        {
            return dbMatches.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<DbTeam> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbTeam> dbTeams)
        {
            return dbTeams.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<DbTeamAlternateName> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbTeamAlternateName> dbTeamAlternateNames)
        {
            return dbTeamAlternateNames.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<DbLeague> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbLeague> dbLeagues)
        {
            return dbLeagues.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<DbLeagueAlternateName> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbLeagueAlternateName> dbLeagueAlternateNames)
        {
            return dbLeagueAlternateNames.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<DbDiscipline> CopyCollectionWithoutNavigationProperties(this IEnumerable<DbDiscipline> dbDisciplines)
        {
            return dbDisciplines.Select(b => b.CopyWithoutNavigationProperties()).ToList();
        }

        public static List<BetToAssociateGvVM> ToBetsToAssociateGvVM(this IEnumerable<BetToDisplayGvVM> betsToDisplayGvVM)
        {
            return betsToDisplayGvVM.Select(b => b.ToBetToAssociateGvVM()).ToList();
        }

        public static List<MatchToAssociateGvVM> ToMatchesToAssociateGvVM(this IEnumerable<DbMatch> dbMatches)
        {
            return dbMatches.Select(m => m.ToMatchToAssociateGvVM()).ToList();
        }

        public static List<SentBetGvVM> ToSentBetsGvVM(this IEnumerable<DbBet> dbBets)
        {
            var sentBets = dbBets.Select(b => b.ToSentBetGvVM()).OrderBy(b => b.LocalTimestamp).ThenBy(b => b.HomeName).ToList();
            var i = 0;
            sentBets.ForEach(b => b.Nr = ++i);
            return sentBets;
        }

        public static bool ContainsAll<T>(this IEnumerable<T> en1, IEnumerable<T> en2)
        {
            var arr1 = en1.Distinct().ToArray();
            var arr2 = en2.Distinct().ToArray();
            return arr1.Intersect(arr2).Count() == arr2.Length;
        }

        public static bool ContainsAny<T>(this IEnumerable<T> en1, IEnumerable<T> en2)
        {
            var arr1 = en1.Distinct().ToArray();
            var arr2 = en2.Distinct().ToArray();
            return arr1.Intersect(arr2).Any();
        }

        public static bool ContainsAll<T>(this IEnumerable<T> en1, params T[] en2)
        {
            return en1.ContainsAll(en2.AsEnumerable());
        }

        public static bool ContainsAny<T>(this IEnumerable<T> en1, params T[] en2)
        {
            return en1.ContainsAny(en2.AsEnumerable());
        }

        public static bool ContainsAll(this IEnumerable<string> en1, IEnumerable<string> en2)
        {
            var arr1 = en1.Select(x => x.ToLower()).Distinct().ToArray();
            var arr2 = en2.Select(x => x.ToLower()).Distinct().ToArray();
            return arr1.Intersect(arr2).Count() == arr2.Length;
        }

        public static bool ContainsAny(this IEnumerable<string> en1, IEnumerable<string> en2)
        {
            var arr1 = en1.Select(x => x.ToLower()).Distinct().ToArray();
            var arr2 = en2.Select(x => x.ToLower()).Distinct().ToArray();
            return arr1.Intersect(arr2).Any();
        }

        public static bool ContainsAll(this IEnumerable<string> en1, params string[] en2)
        {
            return en1.ContainsAll(en2.AsEnumerable());
        }
        
        public static bool ContainsAny(this IEnumerable<string> en1, params string[] en2)
        {
            return en1.ContainsAny(en2.AsEnumerable());
        }

        public static bool CollectionEqual<T>(this IEnumerable<T> col1, IEnumerable<T> col2)
        {
            return col1.OrderBy(t => t).Distinct().SequenceEqual(col2.OrderBy(t => t).Distinct());
        }

        public static IEnumerable<TSource> ExceptBy<TSource, TSelector>(this IEnumerable<TSource> en, TSource el, Func<TSource, TSelector> selector)
        {
            return en.ExceptBy(el.ToEnumerable(), selector);
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

        public static IQueryable<DbTipster> ButSelf(this IQueryable<DbTipster> tipsters)
        {
            var self = DbTipster.Me();
            return tipsters.Where(t => t.Name != self.Name);
        }

        #endregion

        #region - DbSet Extensions

        public static void RemoveBy<TSource>(this DbSet<TSource> dbSet, Expression<Func<TSource, bool>> predicate) where TSource : class
        {
            dbSet.RemoveRange(dbSet.Where(predicate));
        }

        public static void RemoveByMany<TSource, TKey>(this DbSet<TSource> dbSet, Func<TSource, TKey> selector, IEnumerable<TKey> matches) 
            where TSource : class
        {
            foreach (var match in matches)
            {
                var toDel = dbSet.AsEnumerable().Where(e => selector(e).Equals(match));
                dbSet.RemoveRange(toDel);
            }
        }

        public static void RemoveDuplicatesBy<TSource, TKey>(this DbSet<TSource> dbSet, Func<TSource, TKey> selector) where TSource : class
        {
            var knownKeys = new HashSet<TKey>();
            foreach (var entity in dbSet)
                if (!knownKeys.Add(selector(entity)))
                    dbSet.Remove(entity);
        }

        public static int FirstFree<T>(this DbSet<T> dbSet, Func<T, int> selector) where T : class
        {
            var col = dbSet.Select(selector).ToArray();
            var maxId = dbSet.Any() ? col.Max() : 0;
            var range = Enumerable.Range(0, maxId + 2);
            return range.Except(col).Min(); // nie zawsze można przewidzieć ile elementów dodamy przed zapisaniem do bazy (potrzebujemy x wolnych id)
        }

        public static int Next<T>(this DbSet<T> dbSet, Func<T, int> selector) where T : class
        {
            return dbSet.Any() ? dbSet.Select(selector).Max() + 1 : 0;
        }

        public static void RemoveUnused(this DbSet<DbWebsite> dbWebsites, IQueryable<DbTipster> dbTipsters)
        {
            var websiteIdsWoLogin = dbWebsites.Where(w => w.LoginId == null).Select(w => w.Id).ToArray();
            var usedWebsiteIds = dbTipsters.ButSelf().Select(t => t.WebsiteId).Where(wid => wid != null).Cast<int>().Distinct().ToArray();
            var notUsedWebsiteIds = websiteIdsWoLogin.Except(usedWebsiteIds);
            dbWebsites.RemoveByMany(ws => ws.Id, notUsedWebsiteIds);
        }

        #endregion

        #region - ICollection Extensions

        public static T[] IColToArray<T>(this ICollection<T> col)
        {
            var array = new T[col.Count];
            col.CopyTo(array, 0);
            return array;
        }

        public static object[] IColToArray(this ICollection col)
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
            return Array.IndexOf(col.IColToArray(), item);
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

        #region - Dictionary Extensions;

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

        public static void AddIf<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, bool? condition, TKey key, TValue value) where TValue : class
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

        public static Parameters ToParameters(this Dictionary<string, string> dictParameters) // order here won't be kept because dictionaries doesn't keep track of order
        {
            return new Parameters(dictParameters != null && dictParameters.Count > 0
                ? dictParameters.Select(kvp => new Parameter(kvp.Key, kvp.Value)).ToArray()
                : Enumerable.Empty<Parameter>().ToArray());
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

        #region - NameValueCollection Extensions

        public static IEnumerable<KeyValuePair<string, string>> AsEnumerable(this NameValueCollection nvc)
        {
            return nvc.AllKeys.SelectMany(nvc.GetValues, (k, v) => new KeyValuePair<string, string>(k, v));
        }

        public static Dictionary<string, string> ToDictionary(this NameValueCollection nvc)
        {
            return nvc.AsEnumerable().ToDictionary();
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
                if (!(rawChild is DependencyObject depObjRawChild)) continue;
                var child = depObjRawChild;
                if (child is T tChild)
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

        public static IEnumerable<Visual> FindVisualDescendants(this Visual parent)
        {
            if (parent == null)
                yield break;
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                if (!(VisualTreeHelper.GetChild(parent, i) is Visual child))
                    continue;
                yield return child;
                foreach (var grandChild in child.FindVisualDescendants())
                    yield return grandChild;
            }
        }

        public static IEnumerable<T> FindVisualDescendants<T>(this Visual parent) where T : Visual
        {
            if (parent == null)
                yield break;
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                if (!(VisualTreeHelper.GetChild(parent, i) is Visual child))
                    continue;
                if (child is T tChild)
                    yield return tChild;
                foreach (var grandChild in child.FindVisualDescendants<T>())
                    yield return grandChild;
            }
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

        public static T Refresh<T>(this T control) where T : FrameworkElement
        {
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            return control;
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

        #region ComboBox Extensions
        
        public static void SelectByItem(this ComboBox ddl, DdlItem item)
        {
            ddl.SelectedItem = ddl.ItemsSource.Cast<DdlItem>().Single(i => i.Equals(item));
        }

        public static void SelectById(this ComboBox ddl, int customId)
        {
            var item = ddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == customId);
            ddl.SelectedItem = item;
        }

        public static void SelectByText(this ComboBox ddl, string customText)
        {
            var item = ddl.ItemsSource.Cast<DdlItem>().Single(i => i.Text == customText);
            ddl.SelectedItem = item;
        }

        public static DdlItem SelectedItem(this ComboBox ddl)
        {
            return (DdlItem) ddl.SelectedItem;
        }

        public static int SelectedId(this ComboBox ddl)
        {
            return ((DdlItem) ddl.SelectedItem).Index;
        }

        public static string SelectedText(this ComboBox ddl)
        {
            return ((DdlItem)ddl.SelectedItem).Text;
        }

        public static T SelectedEnumValue<T>(this ComboBox ddl)
        {
            var selectedItem = (DdlItem)ddl.SelectedItem;
            var enumType = typeof(T);

            var value = (Enum)Enum.ToObject(enumType, selectedItem.Index);
            if (Enum.IsDefined(enumType, value) == false)
                throw new NotSupportedException($"Nie można przekonwertować wartości na podany typ: {enumType}");

            return (T)(object)value;
        }

        #endregion

        #region ListBox Extensions

        public static void SelectById(this ListBox mddl, int id)
        {
            var item = mddl.ItemsSource.Cast<DdlItem>().Single(i => i.Index == id);
            mddl.SelectedItems.Add(item);
        }

        public static void SelectByIds(this ListBox mddl, IEnumerable<int> ids)
        {
            var ddlItems = mddl.ItemsSource.Cast<DdlItem>().Where(i => ids.Any(id => i.Index == id)).ToList();
            foreach (var item in ddlItems)
                mddl.SelectedItems.Add(item);
        }

        public static int[] SelectedIds(this ListBox mddl)
        {
            return mddl.SelectedItems.Cast<DdlItem>().Select(i => i.Index).ToArray();
        }

        public static void SelectByItem(this ListBox mddl, DdlItem ddlItem)
        {
            mddl.UnselectAll();
            mddl.SelectedItems.Add(ddlItem);
        }

        public static void SelectByItems(this ListBox mddl, DdlItem[] ddlItems)
        {
            mddl.UnselectAll();
            foreach (var item in ddlItems)
                mddl.SelectedItems.Add(item);
        }

        public static DdlItem[] SelectedItems(this ListBox mddl)
        {
            return mddl.SelectedItems.Cast<DdlItem>().ToArray();
        }

        public static void SelectAll(this ListBox mddl)
        {
            mddl.UnselectAll();
            var items = mddl.Items.IColToArray();
            foreach (var item in items)
                mddl.SelectedItems.Add(item);
        }

        public static void UnselectAll(this ListBox mddl)
        {
            var selectedItems = mddl.SelectedItems.IColToArray();
            foreach (var item in selectedItems)
                mddl.SelectedItems.Remove(item);
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
            {
                var scrollViewer = gv.GetScrollViewer();
                scrollViewer?.ScrollToEnd();
            } 
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
                sv?.ScrollToVerticalOffset(items.Index(itemToScrollTo));
            }
        }

        public static void SetSelecteditemsSource<T>(this DataGrid gv, ObservableCollection<T> items)
        {
            GridViewSelectionUtils.SetSelectedItems(gv, items);
        }

        #endregion

        #region RemoteWebDriver Extensions

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

        public static void Highlight(this FrameworkElement control, Color color)
        {
            control.AnimateChangeColor(color);
        }

        public static void Unhighlight(this FrameworkElement control, Color defaultColor)
        {
            control.AnimateChangeColor(defaultColor);
        }

        private static async void AnimateChangeColor(this FrameworkElement control, Color color)
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

            var loaderText = "...";
            if (LocalizationManager.IsInitialized)
                loaderText = LocalizationManager.GetGeneralLoaderLocalizedString();

            var status = new TextBlock
            {
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 18,
                Margin = new Thickness(0, 125, 0, 0),
                Text = loaderText, // "Ładowanie..."
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

        #region DataGridTextColumn Extensions

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

        #region Enum Extensions

        public static string ConvertToString(this Enum en, bool toLower = false, string betweenWords = "")
        {
            var name = Enum.GetName(en.GetType(), en);
            if (!String.IsNullOrEmpty(betweenWords))
                name = Regex.Replace(name ?? "", @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", $"{betweenWords}$0");
            if (toLower)
                name = name?.ToLower();
            return name;
        }

        public static string EnumToString(this Enum en, bool toLower = false, string betweenWords = "")
        {
            return en.ConvertToString(toLower, betweenWords);
        }

        public static string GetDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name == null) return null;
            var field = type.GetField(name);
            if (field == null) return null;
            var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attr?.Description;
        }

        public static T DescriptionToEnum<T>([NotNull] this string descr) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException("T musi być typu Enum");
            var enumVals = EnumUtils.GetValues<T>();
            foreach (var ev in enumVals)
                if ((ev as Enum).GetDescription() == descr)
                    return ev;
            throw new NullReferenceException("Nie istnieje wartość enuma odpowiadająca opisowi");
        }

        #endregion

        #region RestRequest Extensions

        public static void RemoveParameter(this RestRequest restRequest, string name)
        {
            restRequest.Parameters.RemoveBy(x => x.Name == name);
        }

        #endregion

        #region JObject Extensions

        public static JToken VorN(this JObject jObj, string key)
        {
            if (jObj == null) return null;
            return jObj.ContainsKey(key) ? jObj[key] : null;
        }

        #endregion

        #region JToken Extensions

        public static JObject ToJObject(this JToken jToken)
        {
            return (JObject) jToken;
        }

        public static JArray ToJArray(this JToken jToken)
        {
            return (JArray) jToken;
        }

        public static bool IsNullOrEmpty(this JToken jToken)
        {
            return (jToken == null) ||
                (jToken.Type == JTokenType.Array && !jToken.HasValues) ||
                (jToken.Type == JTokenType.Object && !jToken.HasValues) ||
                (jToken.Type == JTokenType.String && jToken.ToString() == String.Empty) ||
                (jToken.Type == JTokenType.Null);
        }

        public static string ToStringN(this JToken jToken)
        {
            return jToken.IsNullOrEmpty() ? null : jToken.ToString();
        }

        public static MatchStatus ToMatchStatus(this JToken jToken)
        {
            return jToken.ToString().ToMatchStatus();
        }

        #endregion

        #region IWebElement Extensions

        public static string XPath(this IWebElement element, string current = "")
        {
            var tag = element.TagName;
            if (tag.EqIgnoreCase("html"))
                return "/html[1]" + current;
            var id = element.GetId();
            if (id != null)
                return $"//{tag}[@id='{id}']{current}";
            var parentElement = element.FindElement(By.XPath(".."));
            var childrenElements = parentElement.FindElements(By.XPath("*"));
            var count = 0;
            foreach (var childElement in childrenElements)
            {
                var childElementTag = childElement.TagName;
                if (tag.EqIgnoreCase(childElementTag))
                    count++;
                if (element.Equals(childElement))
                {
                    var childElementClasses = childElement.GetClasses();
                    if (childElementClasses.Any())
                        return XPath(parentElement, $"/{tag}[{childElementClasses.Select(c => $"contains(@class, '{c}')").JoinAsString(" and ")}]{current}");
                    return XPath(parentElement, $"/{tag}[{count}]{current}");
                }
            }
            return null;
        }

        public static string[] GetClasses(this IWebElement element)
        {
            var classes = element.GetAttribute("class").Split(" ").Where(c => !String.IsNullOrWhiteSpace(c)).ToArray();
            return !classes.Any() 
                ? Enumerable.Empty<string>().ToArray() 
                : classes;
        }

        public static string GetOnlyClass(this IWebElement element)
        {
            return element.GetClasses().Single();
        }

        public static string GetId(this IWebElement element)
        {
            var id = element.GetAttribute("id");
            return String.IsNullOrWhiteSpace(id) ? null : id;
        }

        public static bool HasClass(this IWebElement element, string cl)
        {
            return element.GetClasses().Any(c => c.EqIgnoreCase(cl));
        }

        public static void TryClickUntilNotCovered(this IWebElement element)
        {
            bool exceptionThrown;
            do
            {
                try
                {
                    element.Click();
                    exceptionThrown = false;
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is WebDriverException)
                {
                    if (!ex.Message.ContainsAny("Other element would receive the click"))
                        throw;
                    exceptionThrown = true;
                    Thread.Sleep(250);
                }
            } while (exceptionThrown);

        }

        #endregion

        #region HtmlNode

        public static string[] GetClasses(this HtmlNode element)
        {
            return element.GetAttributeValue("class", "").Split(" ");
        }

        public static string GetOnlyClass(this HtmlNode element)
        {
            return element.GetAttributeValue("class", "").Split(" ").Single();
        }

        public static string GetId(this HtmlNode element)
        {
            return element.GetAttributeValue("id", "");
        }

        public static bool HasClass(this HtmlNode element, params string[] classes)
        {
            return element.GetAttributeValue("class", "").Split(" ").Select(cl => cl.ToLower()).Intersect(classes).Any();
        }

        public static bool HasClass(this HtmlNode element, string cl)
        {
            return element.HasClass(cl.ToEnumerable().ToArray());
        }

        #endregion

        #region Object Extensions

        public static string ToStringN(this object o)
        {
            string strO = null;
            try { strO = o.ToString(); } catch (Exception) {  }
            return String.IsNullOrWhiteSpace(strO) ? null : strO;
        }

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
            return int.TryParse(obj.ToDoubleN()?.Round().ToString().BeforeFirst("."), NumberStyles.Any, LocalizationManager.Culture, out int val) ? val : (int?) null;
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
            return UInt32.TryParse(obj.ToDoubleN()?.Round().ToString().BeforeFirst("."), NumberStyles.Any, LocalizationManager.Culture, out uint val) ? val : (uint?)null;
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
            return Int64.TryParse(obj.ToDoubleN()?.Round().ToString().BeforeFirst("."), NumberStyles.Any, LocalizationManager.Culture, out long val) ? val : (long?)null;
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
            
            var strD = obj.ToString().Replace(",", ".");
            var isNegative = strD.StartsWith("-");
            if (isNegative || strD.StartsWith("+"))
                strD = strD.Skip(1);
            
            var parsedVal = Double.TryParse(strD, NumberStyles.Any, LocalizationManager.Culture, out double tmpvalue) ? tmpvalue : (double?)null;
            if (isNegative)
                parsedVal = -parsedVal;
            return parsedVal;
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
            return Decimal.TryParse(obj.ToString().Replace(",", "."), NumberStyles.Any, LocalizationManager.Culture, out decimal tmpvalue) ? tmpvalue : (decimal?)null;
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

        public static ExtendedTime ToExtendedTimeN(this object o, string format = null, TimeZoneKind tz = TimeZoneKind.UTC)
        {
            if (String.IsNullOrWhiteSpace(o?.ToString()))
                return null;

            var parsedDateTime = format != null
                ? DateTime.ParseExact(o.ToString(), format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
                : DateTime.Parse(o.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            return parsedDateTime.ToExtendedTime(tz);
        }

        public static ExtendedTime ToExtendedTime(this object o, string format = null, TimeZoneKind tz = TimeZoneKind.UTC)
        {
            var extTime = o.ToExtendedTimeN(format, tz);
            if (extTime == null)
                throw new InvalidCastException(nameof(o));
            return extTime;
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

        public static void AddEventHandlers(this object o, string eventName, List<Delegate> ehs)
        {
            EventHelper.AddEventHandlers(o, eventName, ehs);
        }

        public static List<Delegate> RemoveEventHandlers(this object o, string eventName)
        {
            return EventHelper.RemoveEventHandlers(o, eventName);
        }

        #endregion
    }
}
