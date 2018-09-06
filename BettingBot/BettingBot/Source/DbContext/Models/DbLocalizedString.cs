using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettingBot.Source.DbContext.Models
{
    [Table("tblLocalizedStrings")]
    [Serializable]
    public class DbLocalizedString
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Key { get; set; }
        public string Value { get; set; }

        public DbLocalizedString() { }

        public DbLocalizedString(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbLocalizedString)) return false;
            var o = (DbLocalizedString)obj;

            return Key == o.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() ^ 387;
        }
    }
}