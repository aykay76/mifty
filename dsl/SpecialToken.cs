using System.Text;

namespace dsl
{
    public class SpecialToken : Token
    {
        public char Token { get; set; }

        public SpecialToken()
        {
            Type = TokenType.Special;
        }

        public static SpecialToken GetToken(Scanner scanner)
        {
            SpecialToken t = new SpecialToken();
            t.sc = scanner.col;
            t.sr = scanner.row;

            t.Token = scanner.curr;
            scanner.Next();
            t.ec = scanner.col;
            t.er = scanner.row;

            return t;
        }

        public override string ToString()
        {
            return $"SpecialToken: {Token}";
        }
    }
}