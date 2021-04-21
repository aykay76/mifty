using System;
using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class Server
    {
        State state = new State();

        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            Socket udp = state.Udp;

            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            int messageLength = udp.EndReceiveFrom(asyncResult, ref remoteEndpoint);

            // take a copy of the buffer and start receiving again ASAP to service another customer
            byte[] message = new byte[messageLength];
            Array.Copy(state.Buffer, message, messageLength);
            
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);

            // TODO: start decoding the message
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
            for (int i = 0; i < messageLength; i++)
            {
                // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                Console.Write($"{message[i]:X2} ");
            }
            Console.WriteLine();
        }

        public void Start()
        {
            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Parse("172.22.160.1"), 53));

            state.Udp = udp;

            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public void Stop()
        {
            state.Udp.Close();
        }
    }
}