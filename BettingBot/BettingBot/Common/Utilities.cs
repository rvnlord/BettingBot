using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Common
{
    public class Utilities
    {
        public const string Space = " ";

        private static readonly string[] _numbers = "1234567890".ToArray().Select(c => c.ToString()).ToArray();
        private static readonly string[] _signs = "+-/*".ToArray().Select(c => c.ToString()).ToArray();

        public static string[] NumbersAndSigns => _numbers.Concat(_signs).ToArray();
        public static string[] Numbers => _numbers;
        public static string[] Operators => _signs;

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

        public static IEnumerable<T> ConcatMany<T>(IEnumerable<T>[] enums)
        {
            return enums.SelectMany(x => x);
        }
    }
}
