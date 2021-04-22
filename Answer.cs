namespace mifty
{
    public class Answer
    {
        public string Name { get; set; }
        public ushort Type { get; set; }
        public ushort Class { get; set; }
        public uint TimeToLive { get; set; }
        public ushort Length { get; set; }
        public int DataPos { get; set; }
    }
}