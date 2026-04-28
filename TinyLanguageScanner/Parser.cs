using System;
using System.Collections.Generic;

namespace TinyLanguageScanner
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int index;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token CurrentToken
        {
            get { return index < tokens.Count ? tokens[index] : null; }
        }

        public void ParseProgram()
        {
            if (tokens.Count == 0)
            {
                return;
            }

            ParseStatementSequence(stopAtBlockEnd: false);

            if (CurrentToken != null)
            {
                throw CreateSyntaxError($"Unexpected token '{CurrentToken.Lexeme}' after the end of the program.");
            }
        }

        private void ParseStatementSequence(bool stopAtBlockEnd)
        {
            while (CurrentToken != null)
            {
                if (stopAtBlockEnd && CurrentToken.Lexeme == "}")
                {
                    return;
                }

                if (CurrentToken.Lexeme == ";")
                {
                    MatchLexeme(";");
                    continue;
                }

                ParseStatement();

                if (CurrentToken == null || (stopAtBlockEnd && CurrentToken.Lexeme == "}"))
                {
                    return;
                }

                if (CurrentToken.Lexeme == ";")
                {
                    MatchLexeme(";");
                    continue;
                }

                if (CanStartStatement(CurrentToken))
                {
                    throw CreateSyntaxError($"Missing ';' before '{CurrentToken.Lexeme}'.");
                }

                return;
            }
        }

        private void ParseStatement()
        {
            if (CurrentToken == null)
            {
                throw CreateSyntaxError("Unexpected end of input while parsing a statement.");
            }

            if (IsTypeKeyword(CurrentToken))
            {
                ParseDeclaration();
            }
            else if (CurrentToken.Type == "IDENTIFIER")
            {
                ParseAssignment();
            }
            else if (CurrentToken.Lexeme == "read")
            {
                ParseRead();
            }
            else if (CurrentToken.Lexeme == "write")
            {
                ParseWrite();
            }
            else if (CurrentToken.Lexeme == "if")
            {
                ParseIf();
            }
            else if (CurrentToken.Lexeme == "repeat")
            {
                ParseRepeat();
            }
            else if (CurrentToken.Lexeme == "return")
            {
                ParseReturn();
            }
            else if (CurrentToken.Lexeme == "{")
            {
                ParseBlock();
            }
            else
            {
                throw CreateSyntaxError($"A statement cannot start with '{CurrentToken.Lexeme}'.");
            }
        }

        private void ParseDeclaration()
        {
            MatchLexeme(CurrentToken.Lexeme);
            MatchType("IDENTIFIER");

            if (CurrentToken != null && CurrentToken.Lexeme == ":=")
            {
                MatchLexeme(":=");
                ParseExpression();
            }
        }

        private void ParseAssignment()
        {
            MatchType("IDENTIFIER");
            MatchLexeme(":=");
            ParseExpression();
        }

        private void ParseRead()
        {
            MatchLexeme("read");
            MatchType("IDENTIFIER");
        }

        private void ParseWrite()
        {
            MatchLexeme("write");

            if (CurrentToken == null)
            {
                throw CreateSyntaxError("Expected a value after 'write'.");
            }

            if (CurrentToken.Lexeme == "endl")
            {
                MatchLexeme("endl");
                return;
            }

            ParseExpression();
        }

        private void ParseIf()
        {
            MatchLexeme("if");
            ParseCondition();
            MatchLexeme("then");
            ParseBlock();

            while (CurrentToken != null && CurrentToken.Lexeme == "elseif")
            {
                MatchLexeme("elseif");
                ParseCondition();
                MatchLexeme("then");
                ParseBlock();
            }

            if (CurrentToken != null && CurrentToken.Lexeme == "else")
            {
                MatchLexeme("else");
                ParseBlock();
            }
        }

        private void ParseRepeat()
        {
            MatchLexeme("repeat");
            ParseBlock();
            MatchLexeme("until");
            ParseCondition();
        }

        private void ParseReturn()
        {
            MatchLexeme("return");

            if (CurrentToken != null && CurrentToken.Lexeme != ";" && CurrentToken.Lexeme != "}")
            {
                ParseExpression();
            }
        }

        private void ParseBlock()
        {
            if (CurrentToken != null && CurrentToken.Lexeme == "{")
            {
                MatchLexeme("{");
                ParseStatementSequence(stopAtBlockEnd: true);
                MatchLexeme("}");
                return;
            }

            ParseStatement();
        }

        private void ParseCondition()
        {
            ParseBooleanTerm();

            while (CurrentToken != null && CurrentToken.Lexeme == "||")
            {
                MatchLexeme("||");
                ParseBooleanTerm();
            }
        }

        private void ParseBooleanTerm()
        {
            ParseRelation();

            while (CurrentToken != null && CurrentToken.Lexeme == "&&")
            {
                MatchLexeme("&&");
                ParseRelation();
            }
        }

        private void ParseRelation()
        {
            ParseExpression();

            if (CurrentToken != null && IsRelationalOperator(CurrentToken))
            {
                MatchLexeme(CurrentToken.Lexeme);
                ParseExpression();
            }
        }

        private void ParseExpression()
        {
            ParseTerm();

            while (CurrentToken != null && (CurrentToken.Lexeme == "+" || CurrentToken.Lexeme == "-"))
            {
                MatchLexeme(CurrentToken.Lexeme);
                ParseTerm();
            }
        }

        private void ParseTerm()
        {
            ParseFactor();

            while (CurrentToken != null && (CurrentToken.Lexeme == "*" || CurrentToken.Lexeme == "/"))
            {
                MatchLexeme(CurrentToken.Lexeme);
                ParseFactor();
            }
        }

        private void ParseFactor()
        {
            if (CurrentToken == null)
            {
                throw CreateSyntaxError("Unexpected end of input while parsing an expression.");
            }

            if (CurrentToken.Type == "IDENTIFIER")
            {
                MatchType("IDENTIFIER");
            }
            else if (CurrentToken.Type == "NUMBER")
            {
                MatchType("NUMBER");
            }
            else if (CurrentToken.Type == "STRING")
            {
                MatchType("STRING");
            }
            else if (CurrentToken.Lexeme == "(")
            {
                MatchLexeme("(");
                ParseExpression();
                MatchLexeme(")");
            }
            else
            {
                throw CreateSyntaxError($"Expected an identifier, number, string, or parenthesized expression but found '{CurrentToken.Lexeme}'.");
            }
        }

        private void MatchLexeme(string expectedLexeme)
        {
            if (CurrentToken == null || CurrentToken.Lexeme != expectedLexeme)
            {
                throw CreateSyntaxError($"Expected '{expectedLexeme}' but found '{CurrentToken?.Lexeme ?? "end of input"}'.");
            }

            index++;
        }

        private void MatchType(string expectedType)
        {
            if (CurrentToken == null || CurrentToken.Type != expectedType)
            {
                throw CreateSyntaxError($"Expected {expectedType} but found '{CurrentToken?.Lexeme ?? "end of input"}'.");
            }

            index++;
        }

        private static bool IsTypeKeyword(Token token)
        {
            return token.Lexeme == "int" || token.Lexeme == "float" || token.Lexeme == "string";
        }

        private static bool IsRelationalOperator(Token token)
        {
            return token.Lexeme == "<" || token.Lexeme == ">" || token.Lexeme == "=" || token.Lexeme == "<>";
        }

        private static bool CanStartStatement(Token token)
        {
            return token.Type == "IDENTIFIER"
                || token.Lexeme == "{"
                || token.Lexeme == "if"
                || token.Lexeme == "repeat"
                || token.Lexeme == "read"
                || token.Lexeme == "write"
                || token.Lexeme == "return"
                || IsTypeKeyword(token);
        }

        private Exception CreateSyntaxError(string message)
        {
            return new Exception($"Syntax Error near token {index + 1}: {message}");
        }
    }
}
