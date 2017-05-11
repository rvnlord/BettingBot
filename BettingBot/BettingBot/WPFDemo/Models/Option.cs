using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace WPFDemo.Models
{
    [Table("tblOptions")]
    [Serializable]
    public class Option
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Key { get; set; }
        public string Value { get; set; }

        public Option() { }

        public Option(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}