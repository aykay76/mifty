namespace mifty
{
    public static class QueryClass
    {
        public const ushort Internet = 1;
        public const ushort CSNet = 2;
        public const ushort Chaos = 3;
        public const ushort Hesiod = 4;
        public const ushort All = 255;

        public static ushort Parse(string input)
        {
            if (input == "IN") return QueryClass.Internet;
            else if (input == "CS") return QueryClass.CSNet;
            else if (input == "CH") return QueryClass.Chaos;
            else if (input == "HS") return QueryClass.Hesiod;
            return QueryClass.All;
        }
    }
}