namespace dsl
{
    public class EndOfFileToken : Token
    {
        public EndOfFileToken()
        {
            Type = TokenType.EndOfFile;
        }

        public override string ToString()
        {
            return "EOF";
        }
    }
}