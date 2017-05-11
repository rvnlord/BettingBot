using System;
using System.Text;

namespace WPFDemo.Models
{
    /// <summary>
    /// Implements the Metaphone algorithm
    /// </summary>
    public class Metaphone
    {
        // Constants
        protected const int MaxEncodedLength = 6;
        protected const char NullChar = (char)0;
        protected const string Vowels = "AEIOU";

        // For tracking position within current string
        protected string _text;
        protected int _pos;

        /// <summary>
        /// Encodes the given text using the Metaphone algorithm.
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <returns></returns>
        public string Encode(string text)
        {
            // Process normalized text
            InitializeText(Normalize(text));

            // Write encoded string to StringBuilder
            StringBuilder builder = new StringBuilder();

            // Special handling of some string prefixes:
            //     PN, KN, GN, AE, WR, WH and X
            switch (Peek())
            {
                case 'P':
                case 'K':
                case 'G':
                    if (Peek(1) == 'N')
                        MoveAhead();
                    break;

                case 'A':
                    if (Peek(1) == 'E')
                        MoveAhead();
                    break;

                case 'W':
                    if (Peek(1) == 'R')
                        MoveAhead();
                    else if (Peek(1) == 'H')
                    {
                        builder.Append('W');
                        MoveAhead(2);
                    }
                    break;

                case 'X':
                    builder.Append('S');
                    MoveAhead();
                    break;
            }

            //
            while (!EndOfText && builder.Length < MaxEncodedLength)
            {
                // Cache this character
                char c = Peek();

                // Ignore duplicates except CC
                if (c == Peek(-1) && c != 'C')
                {
                    MoveAhead();
                    continue;
                }

                // Don't change F, J, L, M, N, R or first-letter vowel
                if (IsOneOf(c, "FJLMNR") ||
                    (builder.Length == 0 && IsOneOf(c, Vowels)))
                {
                    builder.Append(c);
                    MoveAhead();
                }
                else
                {
                    int charsConsumed = 1;

                    switch (c)
                    {
                        case 'B':
                            // B = 'B' if not -MB
                            if (Peek(-1) != 'M' || Peek(1) != NullChar)
                                builder.Append('B');
                            break;

                        case 'C':
                            // C = 'X' if -CIA- or -CH-
                            // Else 'S' if -CE-, -CI- or -CY-
                            // Else 'K' if not -SCE-, -SCI- or -SCY-
                            if (Peek(-1) != 'S' || !IsOneOf(Peek(1), "EIY"))
                            {
                                if (Peek(1) == 'I' && Peek(2) == 'A')
                                    builder.Append('X');
                                else if (IsOneOf(Peek(1), "EIY"))
                                    builder.Append('S');
                                else if (Peek(1) == 'H')
                                {
                                    if ((_pos == 0 && !IsOneOf(Peek(2), Vowels)) ||
                                        Peek(-1) == 'S')
                                        builder.Append('K');
                                    else
                                        builder.Append('X');
                                    charsConsumed++;    // Eat 'CH'
                                }
                                else builder.Append('K');
                            }
                            break;

                        case 'D':
                            // D = 'J' if DGE, DGI or DGY
                            // Else 'T'
                            if (Peek(1) == 'G' && IsOneOf(Peek(2), "EIY"))
                                builder.Append('J');
                            else
                                builder.Append('T');
                            break;

                        case 'G':
                            // G = 'F' if -GH and not B--GH, D--GH, -H--GH, -H---GH
                            // Else dropped if -GNED, -GN, -DGE-, -DGI-, -DGY-
                            // Else 'J' if -GE-, -GI-, -GY- and not GG
                            // Else K
                            if ((Peek(1) != 'H' || IsOneOf(Peek(2), Vowels)) &&
                                (Peek(1) != 'N' || (Peek(1) != NullChar &&
                                                    (Peek(2) != 'E' || Peek(3) != 'D'))) &&
                                (Peek(-1) != 'D' || !IsOneOf(Peek(1), "EIY")))
                            {
                                if (IsOneOf(Peek(1), "EIY") && Peek(2) != 'G')
                                    builder.Append('J');
                                else
                                    builder.Append('K');
                            }
                            // Eat GH
                            if (Peek(1) == 'H')
                                charsConsumed++;
                            break;

                        case 'H':
                            // H = 'H' if before or not after vowel
                            if (!IsOneOf(Peek(-1), Vowels) || IsOneOf(Peek(1), Vowels))
                                builder.Append('H');
                            break;

                        case 'K':
                            // K = 'C' if not CK
                            if (Peek(-1) != 'C')
                                builder.Append('K');
                            break;

                        case 'P':
                            // P = 'F' if PH
                            // Else 'P'
                            if (Peek(1) == 'H')
                            {
                                builder.Append('F');
                                charsConsumed++;    // Eat 'PH'
                            }
                            else
                                builder.Append('P');
                            break;

                        case 'Q':
                            // Q = 'K'
                            builder.Append('K');
                            break;

                        case 'S':
                            // S = 'X' if SH, SIO or SIA
                            // Else 'S'
                            if (Peek(1) == 'H')
                            {
                                builder.Append('X');
                                charsConsumed++;    // Eat 'SH'
                            }
                            else if (Peek(1) == 'I' && IsOneOf(Peek(2), "AO"))
                                builder.Append('X');
                            else
                                builder.Append('S');
                            break;

                        case 'T':
                            // T = 'X' if TIO or TIA
                            // Else '0' if TH
                            // Else 'T' if not TCH
                            if (Peek(1) == 'I' && IsOneOf(Peek(2), "AO"))
                                builder.Append('X');
                            else if (Peek(1) == 'H')
                            {
                                builder.Append('0');
                                charsConsumed++;    // Eat 'TH'
                            }
                            else if (Peek(1) != 'C' || Peek(2) != 'H')
                                builder.Append('T');
                            break;

                        case 'V':
                            // V = 'F'
                            builder.Append('F');
                            break;

                        case 'W':
                        case 'Y':
                            // W,Y = Keep if not followed by vowel
                            if (IsOneOf(Peek(1), Vowels))
                                builder.Append(c);
                            break;

                        case 'X':
                            // X = 'S' if first character (already done)
                            // Else 'KS'
                            builder.Append("KS");
                            break;

                        case 'Z':
                            // Z = 'S'
                            builder.Append('S');
                            break;
                    }
                    // Advance over consumed characters
                    MoveAhead(charsConsumed);
                }
            }
            // Return result
            return builder.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        protected void InitializeText(string text)
        {
            _text = text;
            _pos = 0;
        }

        /// <summary>
        /// Indicates if the current position is at the end of
        /// the text.
        /// </summary>
        protected bool EndOfText
        {
            get { return _pos >= _text.Length; }
        }

        /// <summary>
        /// Moves the current position ahead one character.
        /// </summary>
        void MoveAhead()
        {
            MoveAhead(1);
        }

        /// <summary>
        /// Moves the current position ahead the specified number.
        /// of characters.
        /// </summary>
        /// <param name="count">Number of characters to move
        /// ahead.</param>
        void MoveAhead(int count)
        {
            _pos = Math.Min(_pos + count, _text.Length);
        }

        /// <summary>
        /// Returns the character at the current position.
        /// </summary>
        /// <returns></returns>
        protected char Peek()
        {
            return Peek(0);
        }

        /// <summary>
        /// Returns the character at the specified position.
        /// </summary>
        /// <param name="ahead">Position to read relative
        /// to the current position.</param>
        /// <returns></returns>
        protected char Peek(int ahead)
        {
            int pos = (_pos + ahead);
            if (pos < 0 || pos >= _text.Length)
                return NullChar;
            return _text[pos];
        }

        /// <summary>
        /// Indicates if the specified character occurs within
        /// the specified string.
        /// </summary>
        /// <param name="c">Character to find</param>
        /// <param name="chars">String to search</param>
        /// <returns></returns>
        protected bool IsOneOf(char c, string chars)
        {
            return (chars.IndexOf(c) != -1);
        }

        /// <summary>
        /// Normalizes the given string by removing characters
        /// that are not letters and converting the result to
        /// upper case.
        /// </summary>
        /// <param name="text">Text to be normalized</param>
        /// <returns></returns>
        protected string Normalize(string text)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in text)
            {
                if (Char.IsLetter(c))
                    builder.Append(Char.ToUpper(c));
            }
            return builder.ToString();
        }
    }
}