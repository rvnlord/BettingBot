using System;
using System.Linq;
using BettingBot.Source.Common;
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
            if (disciplineStr.EqAnyIgnoreCase("Hockey", "Ice Hockey"))
                return DisciplineType.Hockey;
            if (disciplineStr.EqIgnoreCase("Baseball"))
                return DisciplineType.Baseball;
            if (disciplineStr.EqIgnoreCase("Handball"))
                return DisciplineType.Handball;
            throw new InvalidCastException("Nie można przekonwertować wartości string na poprawną dyscyplinę");
        }

        public static string DisciplineTypeToLocalizedString(DisciplineType? discipline)
        {
            if (discipline == null)
                return null;
            
            return LocalizationManager.GetDisciplineTypeLocalizedString(discipline);
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
        Baseball = 4,
        Handball
    }
}
