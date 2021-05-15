using System.IO;

namespace dsl
{
    public class Scanner
    {
        TextReader _reader;
        public char curr;
        public CharType currType;
        public int row = 1;
        public int col = 1;

        public Scanner(TextReader reader)
        {
            _reader = reader;
            currType = CharType.Whitespace;
        }

        public char Next()
        {
            int val = _reader.Read();
            if (val == -1)
            {
                currType = CharType.EOF;
                return (char)0;
            }

            curr = (char)val;
            col++;

            if (curr == ' ' || curr == '\t' || curr == '\r' || curr == '\n')
            {
                currType = CharType.Whitespace;
                if (curr == '\n')
                {
                    row++;
                    col = 1;
                }
            }
            else if (curr >= 'A' && curr <= 'Z')
            {
                currType = CharType.Alpha;
            }
            else if (curr >= 'a' && curr <= 'z')
            {
                currType = CharType.Alpha;
            }
            else if (curr >= '0' && curr <= '9')
            {
                currType = CharType.Numeric;
            }
            else if (curr == '"')
            {
                currType = CharType.Quote;
            }
            else 
            {
                currType = CharType.Special;
            }

            return curr;
        }

        private int SkipWhitespace()
        {
            while (currType == CharType.Whitespace)
            {
                Next();
            }

            return curr;
        }

        // TODO: maintain a list of token types provided by parser so that scanner can remain unbiased

        // TODO: maintain a list of special token types

        public Token GetToken()
        {
            SkipWhitespace();

            if (currType == CharType.Alpha)
            {
                WordToken token = WordToken.GetToken(this);
                // TODO: check for reserved words
                return token;
            }
            else if (currType == CharType.Numeric)
            {
                return NumberToken.GetToken(this);
            }
            else if (currType == CharType.Quote)
            {
                return StringToken.GetToken(this);
            }
            else if (currType == CharType.EOF)
            {
                return new EndOfFileToken();
            }
            else if (currType == CharType.Special)
            {
                SpecialToken token = SpecialToken.GetToken(this);
                // TODO: check for special token types
                return token;
            }
            
            return null;
        }
    }
}