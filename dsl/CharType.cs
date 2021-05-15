namespace dsl
{
    public class CharType : Enumeration
    {
        public static readonly CharType Whitespace = new CharType(1, "Whitespace");
        public static readonly CharType Alpha = new CharType(2, "Alpha");
        public static readonly CharType Numeric = new CharType(3, "Numeric");
        public static readonly CharType Quote = new CharType(4, "Quote");
        public static readonly CharType Special = new CharType(5, "Other");
        public static readonly CharType EOF = new CharType(6, "EOF");

        public CharType(int id, string name)
        : base(id, name)
        {
        }
    }
}