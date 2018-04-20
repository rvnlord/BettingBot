using System;
using BettingBot.Common;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Converters
{
    public class DisciplineConverter
    {
        public static DisciplineType? ToDisciplineTypeOrNull(string disciplineStr)
        {
            if (disciplineStr == null) // fallback bo betshoot pamięta tylko sotatni miesiąc danych o ligach i dyscyplinach
                return null;
            return ToDisciplineTypeInternal(disciplineStr);
        }

        public static DisciplineType ToDisciplineType(string disciplineStr)
        {
            return ToDisciplineTypeInternal(disciplineStr);
        }

        private static DisciplineType ToDisciplineTypeInternal(string disciplineStr)
        {
            if (disciplineStr.EqAnyIgnoreCase("Soccer", "Football"))
                return DisciplineType.Football;
            if (disciplineStr.EqIgnoreCase("Basketball"))
                return DisciplineType.Basketball;
            if (disciplineStr.EqIgnoreCase("Tennis"))
                return DisciplineType.Tennis;
            if (disciplineStr.EqIgnoreCase("Hockey"))
                return DisciplineType.Hockey;
            if (disciplineStr.EqIgnoreCase("Baseball"))
                return DisciplineType.Baseball;
            throw new InvalidCastException("Nie można przekonwertować wartości string na poprawną dyscyplinę");
        }

        public static string DisciplineTypeToLocalizedString(DisciplineType? discipline)
        {
            if (discipline == null)
                return null;
            if (discipline == DisciplineType.Football)
                return "Piłka nożna";
            if (discipline == DisciplineType.Basketball)
                return "Koszykówka";
            if (discipline == DisciplineType.Tennis)
                return "Tenis";
            if (discipline == DisciplineType.Hockey)
                return "Hokej";
            if (discipline == DisciplineType.Baseball)
                return "Baseball";

            throw new InvalidCastException("Nie można przekonwertować wartości string na poprawną dyscyplinę");
        }

        public static DbDiscipline CopyWithoutNavigationProperties(DbDiscipline dbDiscipline)
        {
            return new DbDiscipline
            {
                Id = dbDiscipline.Id,
                Name = dbDiscipline.Name
            };
        }
    }

    public enum DisciplineType
    {
        Football = 0,
        Basketball = 1,
        Tennis = 2,
        Hockey = 3,
        Baseball = 4
    }
}
