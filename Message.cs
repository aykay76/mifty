namespace mifty
{
    //                                 1  1  1  1  1  1
    //   0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |                      ID                       |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |                    QDCOUNT                    |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |                    ANCOUNT                    |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |                    NSCOUNT                    |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    // |                    ARCOUNT                    |
    // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+            
    public class Message
    {
        public ushort ID { get; set; }

        public bool Query { get; set; }
        public byte Opcode { get; set; } // only bottom 4 bits are used
        public bool AA { get; set; } // authoritative answer
        public bool TC { get; set; } // truncation due to being more than 512 bytes?
        public bool RD { get; set; } // recursion desired
        public bool RA { get; set; } // recursion available
        public byte Z { get; set; } // reserved, must be zero, only 3 bits
        public byte ResponseCode { get; set; } // only 4 bits

        public ushort QueryCount { get; set; }
        public ushort AnswerCount { get; set; }
        public ushort NameServerCount { get; set; }
        public ushort AdditionalRecordCount { get; set; }

        // if query
        public string QueryName { get; set; }
        public ushort QueryType { get; set; }
        public ushort QueryClass { get; set; }

        // if answer
        public string DomainName { get; set; }
        public ushort ResponseType { get; set; }
        public ushort ResponseClass { get; set; }
        public uint TimeToLive { get; set; }
        public ushort ResponseLength { get; set; }
        public string ResponseData { get; set; }

        // TODO: add methods to serialise/deserialise, or maybe not as it adds overhead?
    }
}