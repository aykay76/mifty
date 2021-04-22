namespace mifty
{
    public class Answer
    {
        public string DomainName { get; set; }
        public ushort ResponseType { get; set; }
        public ushort ResponseClass { get; set; }
        public uint TimeToLive { get; set; }
        public ushort ResponseLength { get; set; }
        public string ResponseData { get; set; }
    }
}