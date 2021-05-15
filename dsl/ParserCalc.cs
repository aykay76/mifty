using System;
using System.Collections.Generic;
using System.IO;

namespace dsl
{
    public class ParserCalc : Parser
    {
        // TODO: need to think about moving these, and the icode reverse lookup into a language spec that can be shared between
        // this and icode.

        // special tokens and keywords that are language specific, defined here and not in the generic TokenType/Scanner classes
        int tokenEquals = 16;
        int tokenSemicolon = 17;
        int tokenPlus = 18;
        int tokenMinus = 19;
        int tokenMultiply = 20;
        int tokenDivide = 21;
        int tokenOpenParentheses = 22;
        int tokenCloseParentheses = 23;

        // I might want reserved key words like "pi" - add here as additional token types
        int tokenPi = 24;

        Dictionary<int, string> symbolStrings = new Dictionary<int, string> {
          { 24, "pi" }
        };

        protected override void GetToken()
        {
            base.GetToken();

            // tokenise special tokens according to this language
            // multi-character special tokens can also be added by checking scanner.curr for continuation
            if (token.Type == TokenType.Special)
            {
                SpecialToken st = token as SpecialToken;
                switch (st.Token)
                {
                    case '=':
                        token.Type = tokenEquals;
                        break;
                    case ';':
                        token.Type = tokenSemicolon;
                        break;
                    case '+':
                        token.Type = tokenPlus;
                        break;
                    case '-':
                        token.Type = tokenMinus;
                        break;
                    case '/':
                        token.Type = tokenDivide;
                        break;
                    case '*':
                        token.Type = tokenMultiply;
                        break;
                    case '(':
                        token.Type = tokenOpenParentheses;
                        break;
                    case ')':
                        token.Type = tokenCloseParentheses;
                        break;
                }
            }
            else if (token.Type == TokenType.Identifier)
            {
                // rule out identifiers that are actually reserved words. could be const values like "pi" or logic keywords like "if/then/else/while etc."
                WordToken wt = token as WordToken;
                switch (wt.Word)
                {
                    case "pi":
                        token.Type = tokenPi;
                        break;
                }
            }
        }

        public override void Parse(string filename)
        {
            FileStream fs = File.OpenRead(filename);
            StreamReader sr = new StreamReader(fs);
            scanner = new Scanner(sr);

            GetToken();

            do
            {
                ParseStatement();

                while (token.Type == tokenSemicolon)
                {
                    GetToken();
                }
            }
            while (!(token.Type == TokenType.EndOfFile));
        }

        protected override void ParseStatement()
        {
            if (token.Type == TokenType.Identifier)
            {
                ParseAssignment();
            }
        }

        protected override void ParseAssignment()
        {
            WordToken identifier = token as WordToken;
            var node = SearchAll(identifier.Word);
            if (node == null)
            {
                node = EnterLocal(identifier.Word);
            }

            GetToken();

            if (token.Type == tokenEquals)
            {
                GetToken();

                ParseExpression();

                node.Value = runtimeStack.Pop();
            }
        }

        protected override void ParseExpression()
        {
            ParseSimpleExpression();
        }

        protected override void ParseSimpleExpression()
        {
            bool negative = false;

            if (token.Type == tokenMinus)
            {
                negative = true;
                GetToken();
            }

            ParseTerm();

            if (negative)
            {
                runtimeStack.Push(-runtimeStack.Pop());
            }

            while (token.Type == tokenPlus || token.Type == tokenMinus)
            {
                char op = ((SpecialToken)token).Token;

                GetToken();
                ParseTerm();

                double op2 = runtimeStack.Pop();
                double op1 = runtimeStack.Pop();

                if (op == '+')
                {
                    runtimeStack.Push(op1 + op2);
                }
                else
                {
                    runtimeStack.Push(op1 - op2);
                }
            }
        }

        protected override void ParseTerm()
        {
            ParseFactor();

            while (token.Type == tokenMultiply || token.Type == tokenDivide)
            {
                char op = ((SpecialToken)token).Token;

                GetToken();
                ParseFactor();

                double op2 = runtimeStack.Pop();
                double op1 = runtimeStack.Pop();

                if (op == '*')
                {
                    runtimeStack.Push(op1 * op2);
                }
                else
                {
                    if (op2 == 0.0)
                    {
                        throw new DivideByZeroException();
                    }
                    else
                    {
                        runtimeStack.Push(op1 / op2);
                    }
                }
            }
        }

        protected override void ParseFactor()
        {
            if (token.Type == TokenType.Identifier)
            {
                WordToken wt = token as WordToken;
                var node = SearchAll(wt.Word);
                if (node == null)
                {
                    throw new InvalidOperationException("Unknown identifier");
                }
                else
                {
                    runtimeStack.Push(node.Value);
                }

                GetToken();
            }
            else if (token.Type == tokenPi)
            {
                runtimeStack.Push(Math.PI);
                GetToken();
            }
            else if (token.Type == TokenType.Numeric)
            {
                NumberToken nt = token as NumberToken;
                runtimeStack.Push(nt.Value);
                GetToken();
            }
            else if (token.Type == tokenOpenParentheses)
            {
                GetToken();

                ParseExpression();

                if (token.Type == tokenCloseParentheses)
                {
                    GetToken();
                }
                else
                {
                    throw new InvalidOperationException("No closing parentheses");
                }
            }
        }
    }
}