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

        public bool IsInList(int[] tokenList)
        {
            if (tokenList == null)
            {
                return false;
            }

            foreach (var t in tokenList)
            {
                if (t == Type) return true;
            }

            return false;
        }
    }
}