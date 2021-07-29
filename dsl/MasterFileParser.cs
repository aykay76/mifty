using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using mifty;

namespace dsl
{
    public class MasterFileParser : Parser
    {
        static int tokenSemicolon = 16;
        static int tokenControl = 17;
        // static int tokenOpenParentheses = 18;
        // static int tokenCloseParentheses = 19;
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

        string origin = string.Empty;
        int ttl = 3600;
        string owner = string.Empty;

        public List<Answer> Answers { get; set; }

        public MasterFileParser()
        {
            Answers = new List<Answer>();
        }
        
        public override void Parse(string filename)
        {
            FileStream fs = File.OpenRead(filename);
            StreamReader sr = new StreamReader(fs);
            scanner = new Scanner(sr);

            // Getting the first token will skip whitespace, so I will have either a comment, a RR or a control block
            GetToken();

            do
            {
                if (token.Type == tokenControl)
                {
                    // get the next token which should be a string containing
                    // ORIGIN, INCLUDE or TTL
                    GetToken();

                    if (token.Type == tokenOrigin)
                    {
                        origin = ParseOrigin();
                        owner = origin;
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
                    else if (token.Type == tokenInclude)
                    {
                        GetToken();

                        // TODO: finish this

                        // will it be a filename, need to process dots and slashes??

                        // there could be a domain name - how to know?

                        // there could be a comment - how to know?
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
            GetToken();

            return ParseDomainName();
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

            return builder.ToString();
        }

        // TODO: this doesn't belong here and isn't optimal.. may need a rethink
        private byte[] EncodeName(string name)
        {
            // TODO: do something clever with "pointers" to reduce data on the wire
            // for now just do a simple encoding of the name
            int length = 0;
            string[] parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                length++; // add a byte for the length
                length += part.Length; // add enough bytes for the name part
            }
            length++; // add a byte for the terminating zero length

            int i = 0;
            byte[] bytes = new byte[length];
            foreach (string part in parts)
            {
                bytes[i++] = (byte)part.Length;
                foreach (char c in part)
                {
                    bytes[i++] = (byte)c;
                }
            }
            bytes[i] = 0;

            return bytes;
        }

        protected void ParseResourceRecord()
        {
            bool haveType = false;

            Answer answer = new Answer();

            while (haveType == false && token.Type != TokenType.EndOfFile)
            {
                if (token.Type == TokenType.Numeric)
                {
                    // most likely a <rr> beginning with TTL
                    //     [<TTL>] [<class>] <type> <RDATA>
                    NumberToken nt = token as NumberToken;
                    answer.TimeToLive = (uint)nt.Value;

                    GetToken();
                }
                else if (token.IsInList(tokensQueryClass))
                {
                    // most likely a <rr> beginning with class
                    //     [<class>] [<TTL>] <type> <RDATA>
                    WordToken wt = token as WordToken;
                    answer.Class = QueryClass.Parse(wt.Word);

                    GetToken();
                }
                else if (token.IsInList(tokensQueryType))
                {
                    haveType = true;

                    WordToken wt = token as WordToken;
                    answer.Type = QueryType.Parse(wt.Word);

                    // prepare to parse the data
                    if (token.Type == tokenCanonicalName)
                    {
                        // name string - probably fqdn
                        GetToken();

                        string name = ParseDomainName();
                        if (name[answer.Data.Length - 1] != '.')
                        {
                            name += ".";
                            name += origin;
                        }
                        answer.Data = EncodeName(name);
                    }
                    else if (token.Type == tokenHostInfo)
                    {
                        // TODO: finish this
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
                        ushort priority = (ushort)nt.Value;

                        GetToken();

                        string name = ParseDomainName();
                        if (name[answer.Data.Length - 1] != '.')
                        {
                            name += ".";
                            name += origin;
                        }
                        answer.Data = EncodeName(name);

                        // stuff the priority into the data bytes
                        byte[] temp = new byte[answer.Data.Length + 2];
                        temp[0] = (byte)(priority >> 8);
                        temp[1] = (byte)priority;
                        Array.Copy(answer.Data, 0, temp, 2, answer.Data.Length);
                        answer.Data = temp;
                        answer.Length = (ushort)answer.Data.Length;
                    }
                    else if (token.Type == tokenNameServer)
                    {
                        GetToken();
                        string name = ParseDomainName();
                        if (name[answer.Data.Length - 1] != '.')
                        {
                            name += ".";
                            name += origin;
                        }
                        answer.Data = EncodeName(name);
                        answer.Length = (ushort)answer.Data.Length;
                    }
                    else if (token.Type == tokenPointer)
                    {
                        GetToken();
                        string name = ParseDomainName();
                        if (name[answer.Data.Length - 1] != '.')
                        {
                            name += ".";
                            name += origin;
                        }
                        answer.Data = EncodeName(name);
                        answer.Length = (ushort)answer.Data.Length;
                    }
                    else if (token.Type == tokenAuthority)
                    {
                        // name server
                        GetToken();

                        // TODO: if token is open parentheses then loop until close parentheses (and stop scanner from swallowing parentheses)
                        string nameServer = ParseDomainName();
                        if (nameServer[nameServer.Length - 1] != '.')
                        {
                            nameServer += ".";
                            nameServer += origin;
                        }

                        // mailbox of responsible person
                        string responsible = ParseDomainName();
                        if (responsible[responsible.Length - 1] != '.')
                        {
                            responsible += ".";
                            responsible += origin;
                        }

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
                        int minimumTTL = (int)nt.Value;

                        GetToken();

                        // TODO: add binary representation to answer.Data
                    }
                    else if (token.Type == tokenText)
                    {
                        GetToken();
                        
                        // TODO: this is wrong and probably needs additional parsing
                        // see RFC 1035, page 35:
                        // <character-string> is expressed in one or two ways: as a contiguous set
                        // of characters without interior spaces, or as a string beginning with a "
                        // and ending with a ".  Inside a " delimited string any character can
                        // occur, except for a " itself, which must be quoted using \ (back slash).

                        answer.Data = System.Text.Encoding.UTF8.GetBytes(((WordToken)token).Word);
                        answer.Length = (ushort)answer.Data.Length;

                        GetToken();
                    }
                    else if (token.Type == tokenHostAddress)
                    {
                        GetToken();
                        answer.Length = 4;
                        answer.Data = ParseIPv4AddressBytes();
                    }
                    else if (token.Type == tokenHostIPv6Address)
                    {
                        GetToken();
                        answer.Length = 16;
                        answer.Data = ParseIPv6AddressBytes();
                    }
                    else if (token.Type == tokenWellKnownService)
                    {
                        // address
                        GetToken();

                        string address = ParseIPv4Address();

                        string protocol = ((WordToken)token).Word;

                        // TODO: open parentheses, loop until close
                    }
                }
                else
                {
                    // most likely a <rr> beginning with <owner>
                    //     <domain-name> [<class>] [<TTL>] <type> <RDATA>
                    answer.Name = ParseDomainName();
                    if (answer.Name != "@")
                    {
                        // set default owner in case we reach another record that doesn't have an owner
                        // see section 5.1
                        owner = answer.Name;
                    }
                }
            }

            if (answer.Name == null)
            {
                answer.Name = owner;
            }

            if (answer.Name == "@")
            {
                answer.Name = origin;
            }
            else if (answer.Name.EndsWith(".") == false)
            {
                answer.Name += "." + origin;
            }

            // add to the running list of entries found
            Answers.Add(answer);

            // Console.WriteLine($"Parsed resource record: {entry.Owner}\t{entry.Type}\t{entry.Class}\t{entry.Data}");
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

        protected byte[] ParseIPv4AddressBytes()
        {
            int pos = 0;
            byte[] bytes = new byte[4];

            // first octet
            bytes[pos++] = (byte)((NumberToken)token).Value;

            for (int i = 0; i < 3; i++)
            {
                // get dot
                GetToken();

                // next octet as a number
                GetToken();
                bytes[pos++] = (byte)((NumberToken)token).Value;
            }

            GetToken();

            return bytes;
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

        protected byte[] ParseIPv6AddressBytes()
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

            IPAddress address = IPAddress.Parse(builder.ToString());
            return address.GetAddressBytes();
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