namespace dsl
{
    public class TokenType
    {
        public static readonly int EndOfFile = 0;
        public static readonly int Identifier = 1;
        public static readonly int ReservedKeyword = 2;
        public static readonly int Numeric = 3;
        public static readonly int String = 4;
        public static readonly int Special = 5;
        public static readonly int LineMarker = 6;
        public static readonly int Whitespace = 7;
    }
}