using System.Text;

namespace dsl
{
    public class StringToken : Token
    {
        public string Value { get; set; }

        public StringToken()
        {
            Type = TokenType.String;
        }

        public static StringToken GetToken(Scanner scanner)
        {
            StringToken t = new StringToken();
            StringBuilder sb = new StringBuilder();
            t.sc = scanner.col;
            t.sr = scanner.row;

            // lose the opening quotes
            scanner.Next();

            // TODO: cater for escaped quote characters
            while (scanner.currType != CharType.Quote)
            {
                sb.Append(scanner.curr);
                t.ec = scanner.col;
                t.er = scanner.row;
                scanner.Next();
            }

            // lose the closing quotes
            scanner.Next();

            t.Value = sb.ToString();

            return t;
        }

        public override string ToString()
        {
            return $"StringToken: {Value}";
        }
    }
}