using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BettingBot.Source.DbContext.Models
{
    [Table("tblOptions")]
    [Serializable]
    public class DbOption
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Key { get; set; }
        public string Value { get; set; }

        public DbOption() { }

        public DbOption(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DbOption)) return false;
            var o = (DbOption)obj;

            return Key == o.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode() ^ 387;
        }
    }
}