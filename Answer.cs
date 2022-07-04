namespace mifty
{
    public class Answer
    {
        // TODO: either include the additional fields for SOA/WKS etc. here or use polymorphism to represent different record types
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