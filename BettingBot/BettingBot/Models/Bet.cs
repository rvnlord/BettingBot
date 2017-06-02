using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using BettingBot.Models.Interfaces;

namespace BettingBot.Models
{
    [Serializable]
    public class Bet : IBet
    {
        public int Id { get; set; }

        public DateTime Date { get; set; }
        public string Match { get; set; }
        public int BetResult { get; set; }
        public string MatchResult { get; set; }
        public double Odds { get; set; }
        public string PickOriginalString { get; set; }

        public int TipsterId { get; set; }
        public int PickId { get; set; }

        public virtual Tipster Tipster { get; set; }
        public virtual Pick Pick { get; set; }

        public IBet DeepClone()
        {
            var m = new MemoryStream();
            var b = new BinaryFormatter();
            b.Serialize(m, this);
            m.Position = 0;

            return (IBet)b.Deserialize(m);
        }

        public override string ToString()
        {
            return $"{Id} - {Date}, {Match}, {Tipster.Name}";
        }

        public override bool Equals(object obj)
        {
            var b = (Bet) obj;
            return b != null && 
                Equals(new { Date, Match, TipsterId }, new { b.Date, b.Match, b.TipsterId });
        }

        public override int GetHashCode()
        {
            return Date.GetHashCode() ^ 7 * Match.GetHashCode() ^ 17 * Tipster.GetHashCode() ^ 19;
        }
    }
}
