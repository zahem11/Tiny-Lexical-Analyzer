using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TinyLanguageScanner
{
    public class Scanner
    {
        private static readonly string[] Keywords =
        {
            "int", "float", "string", "read", "write",
            "repeat", "until", "if", "elseif",
            "else", "then", "return", "endl"
        };

        public static List<Token> Scan(string code)
        {
            List<Token> tokens = new List<Token>();

            string pattern =
                @"(?<Comment>/\*.*?\*/)|(?<String>"".*?"")|(?<Assign>:=)|(?<Bool>\|\||&&)|(?<Rel><>)|(?<Arith>[+\-*/])|(?<RelSingle>[=<>])|(?<Symbol>[(),;{}])|(?<Number>[0-9]+(\.[0-9]+)?)|(?<Identifier>[A-Za-z][A-Za-z0-9]*)";

            MatchCollection matches = Regex.Matches(code, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                string value = match.Value;

                if (match.Groups["Comment"].Success)
                {
                    continue;
                }

                if (match.Groups["Number"].Success)
                {
                    tokens.Add(new Token("NUMBER", value));
                }
                else if (match.Groups["String"].Success)
                {
                    tokens.Add(new Token("STRING", value));
                }
                else if (Array.Exists(Keywords, keyword => keyword == value))
                {
                    tokens.Add(new Token("KEYWORD", value));
                }
                else if (match.Groups["Identifier"].Success)
                {
                    tokens.Add(new Token("IDENTIFIER", value));
                }
                else if (match.Groups["Assign"].Success)
                {
                    tokens.Add(new Token("ASSIGN_OP", value));
                }
                else if (match.Groups["Arith"].Success)
                {
                    tokens.Add(new Token("ARITH_OP", value));
                }
                else if (match.Groups["Rel"].Success || match.Groups["RelSingle"].Success)
                {
                    tokens.Add(new Token("COND_OP", value));
                }
                else if (match.Groups["Bool"].Success)
                {
                    tokens.Add(new Token("BOOL_OP", value));
                }
                else
                {
                    tokens.Add(new Token("SYMBOL", value));
                }
            }

            return tokens;
        }
    }
}
