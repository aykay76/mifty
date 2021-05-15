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

            // TODO: decide what special tokens will look like - this will be language specific so should be higher level IMO then the tokeniser can be unbiased

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