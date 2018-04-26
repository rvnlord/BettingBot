using System.Linq;
using System.Text;
using BettingBot.Common;
using BettingBot.Source.Clients.Agility.Betshoot.Responses;
using BettingBot.Source.DbContext.Models;
using MoreLinq;
using ArrayUtils = BettingBot.Common.ArrayUtils;
using StringUtils = BettingBot.Common.StringUtils;
using BettingBot.Source.Clients.Selenium.Hintwise.Responses;
using System;
using BettingBot.Source.Clients.Responses;

namespace BettingBot.Source.Converters
{
    public static class PickConverter
    {
        public static PickResponse ParseToPickResponse(string pickStr, string matchStr)
        {
            var pickOriginalString = pickStr;
            pickStr = pickStr.ToLower().Trim();
            matchStr = matchStr.ToLower().Trim();

            var teams = matchStr.SplitByFirst(" vs ", " - ");
            if (teams.Length != 2)
                return new PickResponse(pickOriginalString, PickChoice.Other, null);

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
            pickStr = pickStr.RemoveMany("(", ")");

            if (pickStr == "x")
                pickStr = draw;
            var pickArr = pickStr.Split(".", true).Select(s => s.Trim()).ToArray();
            for (var i = 1; i < pickArr.Length - 1; i++)
            {
                if (pickArr[i] == "." && !pickArr[i - 1].EndsWithAny(ArrayUtils.Numbers) && !pickArr[i + 1].StartsWithAny(ArrayUtils.Numbers))
                    pickArr[i] += " ";
            }
            pickStr = string.Join("", pickArr);
            pickStr = pickStr.Replace("- ", "-").Replace("-", "- ").Replace(" dnb", " +0").Replace(" 0", " +0");

            try
            {
                if (!pickStr.ContainsAny(ArrayUtils.Operators))
                {
                    if (!pickStr.HasSameWords(or))
                    {
                        var pickStrSplit = pickStr.Split(" ");
                        if (pickStrSplit.Length >= 2 && pickStrSplit[1].ContainsAny(ArrayUtils.Numbers))
                        {
                            if (pickStr.HasSameWords(under))
                                return new PickResponse(pickOriginalString, PickChoice.Under, pickStr.Split(StringUtils.Space).SkipUntil(s => s == under).Take(1).Single().ToDouble());
                            if (pickStr.HasSameWords(over))
                                return new PickResponse(pickOriginalString, PickChoice.Over, pickStr.Split(StringUtils.Space).SkipUntil(s => s == over).Take(1).Single().ToDouble());
                        }

                        if (pickStr.HasSameWords(homeTeam, home) && pickStr.HasSameWords(awayTeam, away))
                            return new PickResponse(pickOriginalString, pickStr.SameWords(homeTeam, null, home).Length >= pickStr.SameWords(awayTeam, away).Length ? PickChoice.Home : PickChoice.Away, null);
                        if (pickStr.HasSameWords(homeTeam, home))
                            return new PickResponse(pickOriginalString, PickChoice.Home, null);
                        if (pickStr.HasSameWords(draw))
                            return new PickResponse(pickOriginalString, PickChoice.Draw, null);
                        if (pickStr.HasSameWords(awayTeam, away))
                            return new PickResponse(pickOriginalString, PickChoice.Away, null);
                    }
                    else
                    {
                        if (pickStr.HasSameWords(homeTeam, home) && pickStr.HasSameWords(draw))
                            return new PickResponse(pickOriginalString, PickChoice.HomeOrDraw, null);
                        if (pickStr.HasSameWords(homeTeam, home) && pickStr.HasSameWords(away, awayTeam))
                            return new PickResponse(pickOriginalString, PickChoice.HomeOrAway, null);
                        if (pickStr.HasSameWords(draw) && pickStr.HasSameWords(away, awayTeam))
                            return new PickResponse(pickOriginalString, PickChoice.DrawOrAway, null);
                    }
                }

                if (pickStr.HasSameWords(btts))
                    return new PickResponse(pickOriginalString, PickChoice.BothToScore, pickStr.HasSameWords(yes) ? 1 : 0);

                var withNumToParse = pickStr.RemoveMany(home, away, homeTeam, awayTeam, minus, ah);
                if (!pickStr.Contains(" -"))
                {
                    if (pickStr.SameWords(home, homeTeam).Length > pickStr.SameWords(away, awayTeam).Length)
                    {
                        return new PickResponse(pickOriginalString, PickChoice.HomeAsianHandicapAdd, withNumToParse.Split(StringUtils.Space).First(s => s.IsDouble()).ToDouble());
                    }
                    if (pickStr.SameWords(away, awayTeam).Length > pickStr.SameWords(home, homeTeam).Length)
                    {
                        return new PickResponse(pickOriginalString, PickChoice.AwayAsianHandicapAdd, withNumToParse.Split(StringUtils.Space).First(s => s.IsDouble()).ToDouble());
                    }
                }
                else
                {
                    if (pickStr.SameWords(home, homeTeam).Length > pickStr.SameWords(away, awayTeam).Length)
                    {
                        return new PickResponse(pickOriginalString, PickChoice.HomeAsianHandicapSubtract, withNumToParse.Split(StringUtils.Space).First(s => s.IsDouble()).ToDouble());
                    }
                    if (pickStr.SameWords(away, awayTeam).Length > pickStr.SameWords(home, homeTeam).Length)
                    {
                        return new PickResponse(pickOriginalString, PickChoice.AwayAsianHandicapSubtract, withNumToParse.Split(StringUtils.Space).First(s => s.IsDouble()).ToDouble());
                    }
                }

                return new PickResponse(pickOriginalString, PickChoice.Other, null);
            }
            catch (Exception ex)
            {
                return new PickResponse(pickOriginalString, PickChoice.Other, null);
            }
        }
        
        public static string PickToString(PickChoice choice, double? value)
        {
            var sb = new StringBuilder();

            sb.Append(PickChoiceToString(choice));

            if (choice == PickChoice.BothToScore)
                sb.Append(value.ToBool() ? " - Y" : " - N");
            else if (value != null)
            {
                if (!sb.ToString().EndsWithAny(ArrayUtils.Operators))
                    sb.Append(" ");
                sb.Append($"{value}");
            }
                

            return sb.ToString();
        }

        public static string PickChoiceToString(PickChoice choice)
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

        public static DbPick ToDbPick(PickResponse pickResponse)
        {
            return new DbPick
            {
                Choice = pickResponse.Choice,
                Value = pickResponse.Value
            };
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
