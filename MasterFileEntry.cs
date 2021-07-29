namespace mifty
{
    // TODO: get rid of this and just read "Answer" entries from the zone files - that's basically what they are!
    public class MasterFileEntry
    {
        public string Owner { get; set; }
        public int TTL { get; set; }
        public ushort Class { get; set; }
        public ushort Type { get; set; }
        public ushort RDLength { get; set; }
        public string Data { get; set; }
        public byte[] DataBytes { get; set; }

        // if MX - yes, should have subclasses ;)
        public ushort Priority { get; set; }

        // if SOA
        public string NameServer { get; set; }
        public string Responsible { get; set; }
        public int SerialNumber { get; set; }
        public int RefreshInterval { get; set; }
        public int RetryInterval { get; set; }
        public int ExpiryTimeout { get; set; }
        public int MinimumTTL { get; set; }

        // if WKS
        public string Address { get; set; }
        public string Protocol { get; set; }
        // TODO: add bitfield for port numbers

    }
}