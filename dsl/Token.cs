namespace dsl
{
    public class Token
    {
        public int sr, sc, er, ec;
        public int Type { get; set; }

        public override string ToString()
        {
            return "-x-";
        }
    }
}