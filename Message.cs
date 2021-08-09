using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace mifty
{
    // TODO: check updates in RFC 6895 - the header bits change slightly
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
        public List<Answer> Authority { get; set; }
        public List<Answer> AdditionalRecords { get; set; }

        private byte[] bytes;

        public byte[] Bytes {
            get { return bytes; }
        }

        // Query format
        //                                 1  1  1  1  1  1
        //   0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                                               |
        // /                     QNAME                     /
        // /                                               /
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                     QTYPE                     |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                     QCLASS                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

        // Answer format
        //                                 1  1  1  1  1  1
        //   0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                                               |
        // /                                               /
        // /                      NAME                     /
        // |                                               |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                      TYPE                     |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                     CLASS                     |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                      TTL                      |
        // |                                               |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        // |                   RDLENGTH                    |
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
        // /                     RDATA                     /
        // /                                               /
        // +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        private Answer ParseAnswer(ref int i)
        {
            Answer answer = new Answer();
            answer.Name = ParseName(ref i);
            answer.Type = (ushort)((ushort)(bytes[i++] << 8) | (ushort)bytes[i++]);
            answer.Class = (ushort)((ushort)bytes[i++] << 8 | (ushort)bytes[i++]);
            answer.TimeToLive = (uint)bytes[i++] << 24 | (uint)bytes[i++] << 16 | (uint)bytes[i++] << 8 | (uint)bytes[i++];
            answer.Length = (ushort)((ushort)bytes[i++] << 8 | (ushort)bytes[i++]);
            answer.Data = new byte[answer.Length];
            Array.Copy(bytes, i, answer.Data, 0, answer.Length);
            i += answer.Length;
            return answer;
        }

        private byte[] EncodeName(string name)
        {
            // TODO: do something clever with "pointers" to reduce data on the wire
            // for now just do a simple encoding of the name
            int length = 0;
            string[] parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                length++; // add a byte for the length
                length += part.Length; // add enough bytes for the name part
            }
            length++; // add a byte for the terminating zero length

            int i = 0;
            byte[] bytes = new byte[length];
            foreach (string part in parts)
            {
                bytes[i++] = (byte)part.Length;
                foreach (char c in part)
                {
                    bytes[i++] = (byte)c;
                }
            }
            bytes[i] = 0;

            return bytes;
        }

        public void AddAnswer(Answer answer)
        {
            // convert the entry to a byte array - I think this is the right place to do it because we have visibility of the whole message to see if we can use pointers in the name encoding
            byte[] encodedName = EncodeName(answer.Name);

            AnswerCount++;

            int answerLength = encodedName.Length + 10 + answer.Data.Length;
            
            // copy the initial queries etc.
            int pos = 0;
            byte[] newBytes = new byte[bytes.Length + answerLength];
            Array.Copy(bytes, 0, newBytes, pos, bytes.Length);
            pos += bytes.Length;

            // this is not a query, and i'm authoritative
            newBytes[2] |= 0x84;

            // update the answer count
            newBytes[6] = (byte)(AnswerCount >> 8);
            newBytes[7] = (byte)AnswerCount;

            // NAME
            Array.Copy(encodedName, 0, newBytes, pos, encodedName.Length);
            pos += encodedName.Length;

            // TYPE
            newBytes[pos++] = (byte)(answer.Type >> 8);
            newBytes[pos++] = (byte)answer.Type;

            // CLASS
            newBytes[pos++] = (byte)(answer.Class >> 8);
            newBytes[pos++] = (byte)answer.Class;

            // TTL
            newBytes[pos++] = (byte)(answer.TimeToLive >> 24);
            newBytes[pos++] = (byte)(answer.TimeToLive >> 16);
            newBytes[pos++] = (byte)(answer.TimeToLive >> 8);
            newBytes[pos++] = (byte)answer.TimeToLive;

            // RDLENGTH
            newBytes[pos++] = (byte)(answer.Data.Length >> 8);
            newBytes[pos++] = (byte)answer.Data.Length;

            // RDATA
            Array.Copy(answer.Data, 0, newBytes, pos, answer.Data.Length);

            // swap refs
            bytes = newBytes;
        }

        public static Message FromFile(string filename)
        {
            string rawContent = File.ReadAllText(filename);
            rawContent = rawContent.Replace("\r\n", "").Replace(" ", "");

            byte[] bytes = new byte[rawContent.Length / 2];

            for (int i = 0; i < rawContent.Length; i += 2)
            {
                byte b = Convert.ToByte(rawContent.Substring(i, 2), 16);
                bytes[i / 2] = b;
            }

            Message message = new Message(bytes);
            return message;
        }

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

            QueryCount = (ushort)((ushort)(bytes[4] << 8) | (ushort)bytes[5]);
            AnswerCount = (ushort)((ushort)(bytes[6] << 8) | (ushort)bytes[7]);
            NameServerCount = (ushort)((ushort)(bytes[8] << 8) | (ushort)bytes[9]);
            AdditionalRecordCount = (ushort)((ushort)(bytes[10] << 8) | (ushort)bytes[11]);

            int i = 12;
            Queries = new List<Query>();
            for (int q = 0; q < QueryCount; q++)
            {
                Query query = new Query();
                query.Name = ParseName(ref i);
                query.Type = (ushort)((ushort)(bytes[i++] << 8) | (ushort)bytes[i++]);
                query.Class = (ushort)((ushort)(bytes[i++] << 8) | (ushort)bytes[i++]);
                Queries.Add(query);
            }

            Answers = new List<Answer>();
            for (int a = 0; a < AnswerCount; a++)
            {
                Answer answer = ParseAnswer(ref i);
                Answers.Add(answer);
            }

            Authority = new List<Answer>();
            for (int a = 0; a < NameServerCount; a++)
            {
                Answer answer = ParseAnswer(ref i);
                Answers.Add(answer);
            }

            AdditionalRecords = new List<Answer>();
            for (int a = 0; a < AdditionalRecordCount; a++)
            {
                Answer answer = ParseAnswer(ref i);
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
                    int j = ((partLength & 0x3f) << 8) | bytes[i++];

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