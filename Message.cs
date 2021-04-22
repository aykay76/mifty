using System;
using System.Collections.Generic;
using System.Text;

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

        public List<Query> Queries { get; set; }
        public List<Answer> Answers { get; set; }

        // TODO: add methods to serialise/deserialise, or maybe not as it adds overhead?
        private byte[] bytes;

        public Message(byte[] message)
        {
            bytes = message;
            ID = BitConverter.ToUInt16(bytes, 0);
            Query = (bytes[2] & 0x80) == 0x80;
            Opcode = (byte)((bytes[2] & 120) >> 3);
            AA = (bytes[2] & 4) == 4;
            TC = (bytes[2] & 2) == 2;
            RD = (bytes[2] & 1) == 1;
            RA = (bytes[3] & 0x80) == 0x80;
            ResponseCode = (byte)(bytes[3] & 0xf);

            QueryCount = ((ushort)((ushort)(bytes[4] << 8) | (ushort)bytes[5]));
            AnswerCount = ((ushort)((ushort)(bytes[6] << 8) | (ushort)bytes[7]));
            NameServerCount = ((ushort)((ushort)(bytes[8] << 8) | (ushort)bytes[9]));
            AdditionalRecordCount = ((ushort)((ushort)(bytes[10] << 8) | (ushort)bytes[11]));

            // process the queries
            int i = 12;
            Queries = new List<Query>();
            for (int q = 0; q < QueryCount; q++)
            {
                Query query = new Query();
                query.Name = ParseName(ref i);
                query.Type = ((ushort)((ushort)(bytes[i++] << 8) | (ushort)bytes[i++]));
                query.Class = ((ushort)((ushort)(bytes[i++] << 8) | (ushort)bytes[i++]));
                Queries.Add(query);
            }

            Answers = new List<Answer>();
            for (int a = 0; a < AnswerCount; a++)
            {
                Answer answer = new Answer();

                Answers.Add(answer);
            }
        }

        private string ParseName(ref int i)
        {
            StringBuilder builder = new StringBuilder();

            while (bytes[i] != 0)
            {
                byte partLength = bytes[i++];
                if ((partLength & 0xc0) == 0xc0)
                {
                    // pointer
                    i++;
                    // 
                    int j = bytes[i];
                    return ParseName(ref j);
                }
                else
                {
                    for (int b = 0; b < partLength; b++)
                    {
                        builder.Append((char)bytes[i]);
                        i++;
                    }

                    if (bytes[i] != 0)
                    {
                        builder.Append('.');
                    }
                }
            }

            i++;

            return builder.ToString();
        }
    }
}