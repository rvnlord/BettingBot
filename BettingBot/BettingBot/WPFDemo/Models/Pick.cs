using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MahApps.Metro.Controls;
using MoreLinq;
using WPFDemo.Common;
using static WPFDemo.Common.Utilities;

namespace WPFDemo.Models
{
    [Table("tblPicks")]
    [Serializable]
    public class Pick
    {
        public int Id { get; set; }
        public PickChoice Choice { get; set; }
        public double? Value { get; set; }

        public virtual IList<Bet> Bets { get; set; }

        public Pick()
        {
            Bets = new List<Bet>();
        }

        public Pick(int id, PickChoice choice, double? value)
        {
            Id = id;
            Choice = choice;
            Value = value;

            Bets = new List<Bet>();
        }

        public static Pick Parse(string pickStr, string matchStr)
        {
            pickStr = pickStr.ToLower().Trim();
            matchStr = matchStr.ToLower().Trim();

            var db = new LocalDbContext();
            var newId = db.Picks.Next(p => p.Id);
            
            var teams = matchStr.SplitByFirst(" vs ", " - ");
            if (teams.Length != 2)
                return new Pick(newId, PickChoice.Other, null);

            const string home = "home";
            const string draw = "draw";
            const string away = "away";
            const string plus = "+";
            const string minus = "-";
            const string under = "under";
            const string over = "over";
            const string ah = "ah";
            var homeTeam = teams[0].Trim();
            var awayTeam = teams[1].Trim();
            const string yes = "yes";
            const string no = "no";
            const string or = "or";
            string[] btts = { "both to score", "both teams to score", "btts" };

            pickStr = pickStr.RemoveWords("ah", "eh");

            if (pickStr == "x")
                pickStr = draw;
            var pickArr = pickStr.Split(".", true).Select(s => s.Trim()).ToArray();
            for (var i = 1; i < pickArr.Length - 1; i++)
            {
                if (pickArr[i] == "." && !pickArr[i - 1].EndsWithAny(Numbers) && !pickArr[i + 1].StartsWithAny(Numbers))
                    pickArr[i] += " ";
            }
            pickStr = string.Join("", pickArr);
            pickStr = pickStr.Replace("- ", "-").Replace("-", "- ").Replace(" dnb", " +0").Replace(" 0", " +0");

            if (!pickStr.ContainsAny(Operators))
            {
                if (!pickStr.HasSameWords(or))
                {
                    var pickStrSplit = pickStr.Split(" ");
                    if (pickStrSplit.Length >= 2 && pickStrSplit[1].ContainsAny(Numbers))
                    {
                        if (pickStr.HasSameWords(under))
                            return new Pick(newId, PickChoice.Under, pickStr.Split(Space).SkipUntil(s => s == under).Take(1).Single().ToDouble());
                        if (pickStr.HasSameWords(over))
                            return new Pick(newId, PickChoice.Over, pickStr.Split(Space).SkipUntil(s => s == over).Take(1).Single().ToDouble());
                    }

                    if (pickStr.HasSameWords(homeTeam, home) && pickStr.HasSameWords(awayTeam, away))
                        return new Pick(newId, pickStr.SameWords(homeTeam, home).Length >= pickStr.SameWords(awayTeam, away).Length ? PickChoice.Home : PickChoice.Away, null);
                    if (pickStr.HasSameWords(homeTeam, home))
                        return new Pick(newId, PickChoice.Home, null);
                    if (pickStr.HasSameWords(draw))
                        return new Pick(newId, PickChoice.Draw, null);
                    if (pickStr.HasSameWords(awayTeam, away))
                        return new Pick(newId, PickChoice.Away, null);
                }
                else
                {
                    if (pickStr.HasSameWords(homeTeam, home) && pickStr.HasSameWords(draw))
                        return new Pick(newId, PickChoice.HomeOrDraw, null);
                    if (pickStr.HasSameWords(homeTeam, home) && pickStr.HasSameWords(away, awayTeam))
                        return new Pick(newId, PickChoice.HomeOrAway, null);
                    if (pickStr.HasSameWords(draw) && pickStr.HasSameWords(away, awayTeam))
                        return new Pick(newId, PickChoice.DrawOrAway, null);
                }
            }

            if (pickStr.HasSameWords(btts))
                return new Pick(newId, PickChoice.BothToScore, pickStr.HasSameWords(yes) ? 1 : 0);

            var homeSim = pickStr.SameWords(home, homeTeam);
            var awaySim = pickStr.SameWords(away, awayTeam);

            var withNumToParse = pickStr.RemoveMany(home, away, homeTeam, awayTeam, minus, ah);
            if (!pickStr.Contains(" -"))
            {
                if (pickStr.SameWords(home, homeTeam).Length > pickStr.SameWords(away, awayTeam).Length)
                {
                    return new Pick(newId, PickChoice.HomeAsianHandicapAdd, withNumToParse.Split(Space).First(s => s.IsDouble()).ToDouble());
                }
                if (pickStr.SameWords(away, awayTeam).Length > pickStr.SameWords(home, homeTeam).Length)
                {
                    return new Pick(newId, PickChoice.AwayAsianHandicapAdd, withNumToParse.Split(Space).First(s => s.IsDouble()).ToDouble());
                }
            }
            else
            {
                if (pickStr.SameWords(home, homeTeam).Length > pickStr.SameWords(away, awayTeam).Length)
                {
                    return new Pick(newId, PickChoice.HomeAsianHandicapSubtract, withNumToParse.Split(Space).First(s => s.IsDouble()).ToDouble());
                }
                if (pickStr.SameWords(away, awayTeam).Length > pickStr.SameWords(home, homeTeam).Length)
                {
                    return new Pick(newId, PickChoice.AwayAsianHandicapSubtract, withNumToParse.Split(Space).First(s => s.IsDouble()).ToDouble());
                }
            }
        
            return new Pick(newId, PickChoice.Other, null);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(ConvertChoiceToString(Choice));

            if (Choice == PickChoice.BothToScore)
                sb.Append(Convert.ToBoolean(Value) ? " - Y" : " - N");
            else if (Value != null)
                sb.Append($" {Value}");

            return sb.ToString();
        }

        public static string ConvertChoiceToString(PickChoice choice)
        {
            if (choice == PickChoice.Home) { return ("H"); }
            if (choice == PickChoice.Draw) { return ("D"); }
            if (choice == PickChoice.Away) { return ("A"); }
            if (choice == PickChoice.HomeOrDraw) { return ("H D"); }
            if (choice == PickChoice.HomeOrAway) { return ("H A"); }
            if (choice == PickChoice.DrawOrAway) { return ("D A"); }
            if (choice == PickChoice.Over) { return ("Over"); }
            if (choice == PickChoice.Under) { return ("Under"); }
            if (choice == PickChoice.HomeAsianHandicapAdd) { return ("H +"); }
            if (choice == PickChoice.AwayAsianHandicapAdd) { return ("A +"); }
            if (choice == PickChoice.HomeAsianHandicapSubtract) { return ("H -"); }
            if (choice == PickChoice.AwayAsianHandicapSubtract) { return ("A -"); }
            if (choice == PickChoice.BothToScore) { return ("BTTS"); }
            if (choice == PickChoice.Other) { return "(Inny)"; }
            return "(Nieobsługiwany typ)";
        }

        public bool EqualsWoId(Pick otherPick)
        {
            return Choice == otherPick.Choice && Value == otherPick.Value;
        }
    }

    public enum PickChoice
    {
        Home,
        Draw,
        Away,
        HomeOrDraw,
        HomeOrAway,
        DrawOrAway,
        Over,
        Under,
        HomeAsianHandicapAdd,
        AwayAsianHandicapAdd,
        HomeAsianHandicapSubtract,
        AwayAsianHandicapSubtract,
        BothToScore,
        Other
    }
}
