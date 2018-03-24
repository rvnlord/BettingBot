using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using BettingBot.Common.UtilityClasses;
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
}
