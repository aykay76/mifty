using System.Text;

namespace dsl
{
    public class WordToken : Token
    {
        public string Word { get ;set; }

        public static WordToken GetToken(Scanner scanner)
        {
            WordToken t = new WordToken();
            t.sc = scanner.col;
            t.sr = scanner.row;
            StringBuilder sb = new StringBuilder();
            while (scanner.currType == CharType.Alpha || scanner.currType == CharType.Numeric)
            {
                sb.Append(scanner.curr);
                scanner.Next();
            }
            t.Word = sb.ToString();
            t.ec = scanner.col;
            t.er = scanner.row;

            t.Type = TokenType.Identifier;

            return t;
        }

        public override string ToString()
        {
            return $"WordToken: {Word}";
        }
    }
}