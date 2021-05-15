using System;
using System.IO;
using System.Text;

namespace dsl
{
    public class MasterFileParser : Parser
    {
        int tokenSemicolon = 16;
        int tokenControl = 17;
        int tokenOpenParentheses = 18;
        int tokenCloseParentheses = 19;
        int tokenOrigin = 20;
        int tokenInclude = 21;
        int tokenTTL = 22;
        int tokenDot = 23;
        // TODO: reserved token types for classes and types
        // so that I know when the end of a domain name comes
        // it will either terminate with a control character
        // a numeric (TTL), a class, or a type

        public override void Parse(string filename)
        {
            FileStream fs = File.OpenRead(filename);
            StreamReader sr = new StreamReader(fs);
            scanner = new Scanner(sr);

            // I think I can treat this like any other DSL
            // The following entries are defined according to RFC1035
            //     <blank>[<comment>]
            //     $ORIGIN <domain-name> [<comment>]
            //     $INCLUDE <file-name> [<domain-name>] [<comment>]
            //     <domain-name><rr> [<comment>]
            //     <blank><rr> [<comment>]
            // <rr> contents take one of the following forms:
            //     [<TTL>] [<class>] <type> <RDATA>
            //     [<class>] [<TTL>] <type> <RDATA>

            // Getting the first token will skip whitespace, so I will have either a comment, a RR or a control block

            GetToken();

            do
            {
                if (token.Type == tokenControl)
                {
                    // get the next token which should be a string containing
                    // ORIGIN, INCLUDE or TTL
                    GetToken();

                    Console.WriteLine($"{token.Type} at {token.sr},{token.sc}");
                    if (token.Type == tokenOrigin)
                    {
                        ParseOrigin();
                    }
                }
                else if (token.Type == TokenType.Numeric)
                {

                }
            }
            while (!(token.Type == TokenType.EndOfFile));
        }

        protected void ParseOrigin()
        {
            // $ORIGIN is followed by a domain name and an optional comment (which will be swallowed by tokeniser)
            GetToken();

            ParseDomainName();
        }

        protected string ParseDomainName()
        {
            StringBuilder builder = new StringBuilder();
            do
            {
                if (token.Type == TokenType.Identifier)
                {
                    WordToken wt = token as WordToken;
                    builder.Append(wt.Word);
                }
                else if (token.Type == tokenDot)
                {
                    builder.Append(".");
                }

                GetToken();
            }
            while (token.Type != TokenType.EndOfFile);
            // Console.WriteLine($"{token.Type} at {token.sr},{token.sc}");

            return builder.ToString();
        }

        protected override void GetToken()
        {
            base.GetToken();

            if (token.Type == TokenType.Special)
            {
                SpecialToken st = token as SpecialToken;
                if (st.Token == ';')
                {
                    // we don't care about comments, keep scanning and return a real token
                    token.Type = tokenSemicolon;
                    do
                    {
                        scanner.Next();
                    }
                    while (scanner.curr != '\n');
                    scanner.Next();
                    GetToken();
                }
                else if (st.Token == '$')
                {
                    token.Type = tokenControl;
                }
                else if (st.Token == '(')
                {
                    token.Type = tokenOpenParentheses;
                }
                else if (st.Token == ')')
                {
                    token.Type = tokenCloseParentheses;
                }
                else if (st.Token == '.')
                {
                    token.Type = tokenDot;
                }
            }
            else if (token.Type == TokenType.Identifier)
            {
                WordToken wt = token as WordToken;
                if (wt.Word == "INCLUDE")
                {
                    token.Type = tokenInclude;
                }
                else if (wt.Word == "ORIGIN")
                {
                    token.Type = tokenOrigin;
                }
                else if (wt.Word == "TTL")
                {
                    token.Type = tokenTTL;
                }
            }
        }
    }
}