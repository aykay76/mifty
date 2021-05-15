using System;
using System.IO;
using System.Text;

namespace dsl
{
    public class MasterFileParser : Parser
    {
        public string StartingOrigin { get; set; }
        static int tokenSemicolon = 16;
        static int tokenControl = 17;
        static int tokenOpenParentheses = 18;
        static int tokenCloseParentheses = 19;
        static int tokenOrigin = 20;
        static int tokenInclude = 21;
        static int tokenTTL = 22;
        static int tokenDot = 23;
        static int tokenInternet = 24;
        static int tokenCSNET = 25;
        static int tokenChaos = 26;
        static int tokenHesiod = 27;
        static int tokenTransfer = 28;
        static int tokenMailbox = 29;
        static int tokenMailAgent = 30;
        static int tokenHostAddress = 31;
        static int tokenNameServer = 32;
        static int tokenMailDestination = 33;
        static int tokenMailForwarder = 34;
        static int tokenCanonicalName = 35;
        static int tokenAuthority = 36;
        static int tokenWellKnownService = 37;
        static int tokenPointer = 38;
        static int tokenHostInfo = 39;
        static int tokenMailboxInfo = 40;
        static int tokenMailExchange = 41;
        static int tokenText = 42;
        static int tokenHostIPv6Address = 43;
        static int tokenColon = 44;
        static int tokenAt = 45;

        static readonly int[] tokensQueryClass = { tokenInternet, tokenCSNET, tokenChaos, tokenHesiod };
        static readonly int[] tokensQueryType = { tokenTransfer, tokenMailbox, tokenMailAgent, tokenHostAddress, tokenNameServer, tokenMailDestination, tokenMailForwarder, tokenCanonicalName, tokenAuthority, tokenWellKnownService, tokenPointer, tokenHostInfo, tokenMailboxInfo, tokenMailExchange, tokenText, tokenHostIPv6Address };

        public MasterFileParser()
        {
            StartingOrigin = string.Empty;
        }
        
        public override void Parse(string filename)
        {
            FileStream fs = File.OpenRead(filename);
            StreamReader sr = new StreamReader(fs);
            scanner = new Scanner(sr);

            string origin = StartingOrigin;
            int ttl = 3600;

            // I think I can treat this like any other DSL
            // The following entries are defined according to RFC1035
            //     <blank>[<comment>]
            //     $ORIGIN <domain-name> [<comment>]
            //     $INCLUDE <file-name> [<domain-name>] [<comment>]
            //     $TTL <number> [<comment>] ** This is missing from the RFC, maybe came in a later update?
            //     <domain-name><rr> [<comment>]
            //     <blank><rr> [<comment>]
            // <rr> contents take one of the following forms:
            //     [<owner>] [<TTL>] [<class>] <type> <RDATA>
            //     [<owner>] [<class>] [<TTL>] <type> <RDATA>

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
                    else if (token.Type == tokenTTL)
                    {
                        GetToken();

                        if (token.Type == TokenType.Numeric)
                        {
                            NumberToken nt = token as NumberToken;
                            ttl = (int)nt.Value;
                        }

                        GetToken();
                    }
                }
                else
                {
                    ParseResourceRecord();
                }
            }
            while (!(token.Type == TokenType.EndOfFile));
        }

        protected string ParseOrigin()
        {
            // $ORIGIN is followed by a domain name and an optional comment (which will be swallowed by tokeniser)
            GetToken();

            return ParseDomainName();
        }

        protected string ParseDomainName()
        {
            StringBuilder builder = new StringBuilder();
            do
            {
                // TODO: handle numerics slightly differently in case it's part of a domain name or IP address
                if (token.Type == TokenType.Identifier)
                {
                    WordToken wt = token as WordToken;
                    builder.Append(wt.Word);
                }
                else if (token.Type == tokenAt)
                {
                    builder.Append("@");
                }
                else if (token.Type == tokenDot)
                {
                    builder.Append(".");
                }

                GetToken(false);
            }
            while (token.Type != TokenType.Whitespace && token.Type != TokenType.EndOfFile && token.Type != TokenType.Numeric && token.Type != tokenControl && !token.IsInList(tokensQueryClass) && !token.IsInList(tokensQueryType));

            // skip the rest of the whitespace to have the next token ready
            GetToken();

            Console.WriteLine($"Parsed domain name: {builder.ToString()}");

            return builder.ToString();
        }

        protected void ParseResourceRecord()
        {
            bool haveType = false;

            int timeToLive = 0;
            string owner = string.Empty;
            string @class = string.Empty;
            string type = string.Empty;
            string data = string.Empty;

            while (haveType == false && token.Type != TokenType.EndOfFile)
            {
                if (token.Type == TokenType.Numeric)
                {
                    // most likely a <rr> beginning with TTL
                    //     [<TTL>] [<class>] <type> <RDATA>
                    NumberToken nt = token as NumberToken;
                    timeToLive = (int)nt.Value;

                    GetToken();
                }
                else if (token.IsInList(tokensQueryClass))
                {
                    // most likely a <rr> beginning with class
                    //     [<class>] [<TTL>] <type> <RDATA>
                    WordToken wt = token as WordToken;
                    @class = wt.Word;

                    GetToken();
                }
                else if (token.IsInList(tokensQueryType))
                {
                    haveType = true;

                    WordToken wt = token as WordToken;
                    type = wt.Word;

                    // prepare to parse the data
                    if (token.Type == tokenCanonicalName)
                    {
                        // name string - probably fqdn
                        GetToken();

                        data = ParseDomainName();
                    }
                    else if (token.Type == tokenHostInfo)
                    {
                        // CPU string
                        GetToken();

                        // OS string
                        GetToken();

                        GetToken();
                    }
                    else if (token.Type == tokenMailExchange)
                    {
                        GetToken();

                        NumberToken nt = token as NumberToken;
                        int priority = (int)nt.Value;

                        GetToken();

                        data = ParseDomainName();
                    }
                    else if (token.Type == tokenNameServer)
                    {
                        GetToken();
                        data = ParseDomainName();
                    }
                    else if (token.Type == tokenPointer)
                    {
                        GetToken();
                        data = ParseDomainName();
                    }
                    else if (token.Type == tokenAuthority)
                    {
                        // name server
                        GetToken();
                        ParseDomainName();

                        // mailbox of responsible person
                        ParseDomainName();

                        // serial number
                        NumberToken nt = token as NumberToken;
                        int serialNumber = (int)nt.Value;

                        // refresh interval
                        GetToken();
                        nt = token as NumberToken;
                        int refreshInterval = (int)nt.Value;

                        // retry interval
                        GetToken();
                        nt = token as NumberToken;
                        int retryInterval = (int)nt.Value;

                        // expiry timeout
                        GetToken();
                        nt = token as NumberToken;
                        int expiryTimeout = (int)nt.Value;

                        // minimum ttl
                        GetToken();
                        nt = token as NumberToken;
                        int minimumTtl = (int)nt.Value;

                        GetToken();
                    }
                    else if (token.Type == tokenText)
                    {
                        GetToken();
                        
                        // TODO: this is wrong and probably needs additional parsing
                        data = ((WordToken)token).Word;

                        GetToken();
                    }
                    else if (token.Type == tokenHostAddress)
                    {
                        GetToken();
                        data = ParseIPv4Address();
                    }
                    else if (token.Type == tokenHostIPv6Address)
                    {
                        GetToken();
                        data = ParseIPv6Address();
                    }
                    else if (token.Type == tokenWellKnownService)
                    {
                        // address
                        GetToken();

                        string address = ParseIPv4Address();

                        string protocol = ((WordToken)token).Word;
                    }
                }
                else
                {
                    // most likely a <rr> beginning with <owner>
                    //     <domain-name> [<class>] [<TTL>] <type> <RDATA>
                    owner = ParseDomainName();
                }
            }

            Console.WriteLine($"Parsed resource record: {type}");
        }

        protected string ParseIPv4Address()
        {
            StringBuilder builder = new StringBuilder();

            // first octet
            int octet = (int)((NumberToken)token).Value;
            builder.Append(octet);

            for (int i = 0; i < 3; i++)
            {
                // get dot
                GetToken();
                builder.Append(".");

                // next octet as a number
                GetToken();
                octet = (int)((NumberToken)token).Value;
                builder.Append(octet);
            }

            GetToken();

            return builder.ToString();
        }

        protected string ParseIPv6Address()
        {
            StringBuilder builder = new StringBuilder();

            while (token.Type == TokenType.Numeric || token.Type == tokenColon || token.Type == TokenType.Identifier)
            {
                // alpha segments will be tagged as identifiers
                if (token.Type == TokenType.Identifier)
                {
                    builder.Append(((WordToken)token).Word);
                }
                else if (token.Type == TokenType.Numeric)
                {
                    builder.Append((int)((NumberToken)token).Value);
                }
                else
                {
                    builder.Append(":");
                }

                // get next number, colon or bust
                GetToken(false);
            }

            GetToken();

            return builder.ToString();
        }

        protected override void GetToken(bool skipWhitespace = true)
        {
            base.GetToken(skipWhitespace);

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
                    GetToken(skipWhitespace);
                }
                else if (st.Token == '$')
                {
                    token.Type = tokenControl;
                }
                else if (st.Token == '(')
                {
                    // TODO: I think don't ignore these, but i need to know when they will come
                    // so far SOA and WKS records, but if they could come anywhere then
                    // i need to cater for that
                    // WKS is particularly problematic because it isn't fixed length fields

                    //token.Type = tokenOpenParentheses;
                    // i think just ignore parentheses
                    GetToken(skipWhitespace);
                }
                else if (st.Token == ')')
                {
                    // token.Type = tokenCloseParentheses;
                    GetToken(skipWhitespace);
                }
                else if (st.Token == '.')
                {
                    token.Type = tokenDot;
                }
                else if (st.Token == ':')
                {
                    token.Type = tokenColon;
                }
                else if (st.Token == '@')
                {
                    token.Type = tokenAt;
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
                else if (wt.Word == "IN")
                {
                    token.Type = tokenInternet;
                }
                else if (wt.Word == "CS")
                {
                    token.Type = tokenCSNET;
                }
                else if (wt.Word == "CH")
                {
                    token.Type = tokenChaos;
                }
                else if (wt.Word == "HS")
                {
                    token.Type = tokenHesiod;
                }
                else if (wt.Word == "AFXR")
                {
                    token.Type = tokenTransfer;
                }
                else if (wt.Word == "MAILB")
                {
                    token.Type = tokenMailbox;
                }
                else if (wt.Word == "MAILA")
                {
                    token.Type = tokenMailAgent;
                }
                else if (wt.Word == "A")
                {
                    token.Type = tokenHostAddress;
                }
                else if (wt.Word == "AAAA")
                {
                    token.Type = tokenHostIPv6Address;
                }
                else if (wt.Word == "NS")
                {
                    token.Type = tokenNameServer;
                }
                else if (wt.Word == "MD")
                {
                    token.Type = tokenMailDestination;
                }
                else if (wt.Word == "MF")
                {
                    token.Type = tokenMailForwarder;
                }
                else if (wt.Word == "CNAME")
                {
                    token.Type = tokenCanonicalName;
                }
                else if (wt.Word == "SOA")
                {
                    token.Type = tokenAuthority;
                }
                else if (wt.Word == "WKS")
                {
                    token.Type = tokenWellKnownService;
                }
                else if (wt.Word == "PTR")
                {
                    token.Type = tokenPointer;
                }
                else if (wt.Word == "HINFO")
                {
                    token.Type = tokenHostInfo;
                }
                else if (wt.Word == "MINFO")
                {
                    token.Type = tokenMailboxInfo;
                }
                else if (wt.Word == "MX")
                {
                    token.Type = tokenMailExchange;
                }
                else if (wt.Word == "TXT")
                {
                    token.Type = tokenText;
                }
            }
        }
    }
}