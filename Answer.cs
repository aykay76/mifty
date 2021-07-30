namespace mifty
{
    public class Answer
    {
        public string Name { get; set; }
        public ushort Type { get; set; }
        public ushort Class { get; set; }
        public uint TimeToLive { get; set; }
        public ushort Length { get; set; }
        public byte[] Data { get; set; }

        // TODO: maybe just change the code to keep looking at Data byte array, strings are more convenient right now though
        // keeping names in string format for CNAME resolution etc.
        public string DataString { get; set; }
    }
}