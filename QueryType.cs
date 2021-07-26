namespace mifty
{
    public static class QueryType
    {
        public const ushort A = 1;
        public const ushort NS = 2;
        public const ushort MD = 3;
        public const ushort MF = 4;
        public const ushort CNAME = 5;
        public const ushort SOA = 6;
        public const ushort MB = 7;
        public const ushort MG = 8;
        public const ushort MR = 9;
        public const ushort NULL = 10;
        public const ushort WKS = 11;
        public const ushort PTR = 12;
        public const ushort HINFO = 13;
        public const ushort MINFO = 14;
        public const ushort MX = 15;
        public const ushort TXT = 16;
        public const ushort AAAA = 28; // (0x1C)
        public const ushort AFXR = 252;
        public const ushort MAILB = 253;
        public const ushort MAILA = 254;
        public const ushort All = 255;

        public static ushort Parse(string input)
        {
            if (input == "A") return QueryType.A;
            else if (input == "NS") return QueryType.NS;
            else if (input == "MD") return QueryType.MD;
            else if (input == "CNAME") return QueryType.CNAME;
            else if (input == "SOA") return QueryType.SOA;
            else if (input == "MB") return QueryType.MB;
            else if (input == "MG") return QueryType.MG;
            else if (input == "MR") return QueryType.MR;
            else if (input == "NULL") return QueryType.NULL;
            else if (input == "WKS") return QueryType.WKS;
            else if (input == "PTR") return QueryType.PTR;
            else if (input == "HINFO") return QueryType.HINFO;
            else if (input == "MINFO") return QueryType.MINFO;
            else if (input == "MX") return QueryType.MX;
            else if (input == "TXT") return QueryType.TXT;
            else if (input == "AAAA") return QueryType.AAAA;
            else if (input == "AFXR") return QueryType.AFXR;
            else if (input == "MAILB") return QueryType.MAILB;
            else if (input == "MAILA") return QueryType.MAILA;
            
            return QueryType.All;
        }
    }
}