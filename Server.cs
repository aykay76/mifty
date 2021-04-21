using System;
using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class Server
    {
        State state = new State();

        public static void ReceiveResponseCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            int messageLength = state.UdpOut.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            byte[] message = new byte[messageLength];
            Array.Copy(state.ResponseBuffer, message, messageLength);
            Console.WriteLine("Response received from forwarder:");
            for (int i = 0; i < messageLength; i++)
            {
                // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                Console.Write($"{message[i]:X2} ");
            }
            Console.WriteLine();

            // send response to client
            Console.WriteLine("Sending back to client");
            int sent = state.Udp.SendTo(message, 0, messageLength, SocketFlags.None, state.RemoteEndpoint);
        }

        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            Socket udp = state.Udp;

            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            int messageLength = udp.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            state.RemoteEndpoint = remoteEndpoint as IPEndPoint;

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
            Console.WriteLine("Message received:");
            for (int i = 0; i < messageLength; i++)
            {
                // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                Console.Write($"{message[i]:X2} ");
            }
            Console.WriteLine();

            // for now for now i'm just going to forward to a known DNS server, see what happens
            int sent = state.UdpOut.SendTo(message, 0, messageLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse("192.168.1.254"), 53));
            Console.WriteLine("I know nothing, forwarding on");

            state.UdpOut.BeginReceiveFrom(state.ResponseBuffer, state.ResponsePosition, state.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), state);

            // for now i'm just going to build a referral for everything to my DNS server, then i can snoop
            // this could be configurable in the future as a flag to passthrough or do stuff
            // byte[] response = new byte[512];
            // response[0] = message[0];
            // response[1] = message[1];
            // response[2] = 0x80 | (Opcode.Status << 3) | 1; response[3] = 0x80 | ResponseCode.Success;
            // response[4] = response[5] = 1; // QD count
            // response[6] = 0; response[7] = 1; // AN count
            // response[8] = 0; response[9] = 0; // NS count
            // response[10] = 0; response[11] = 0; // AR count

            // response[12] = 0; // no name
            // response[13] = 0; response[14] = 2; // A record
            // response[15] = 0; response[16] = 1; // IN class
            // response[17] = response[18] = response[19] = response[20] = 0; // TTL = 0
            // response[21] = 0; response[22] = 4;
            // response[23] = 192;
            // response[24] = 168;
            // response[25] = 1;
            // response[26] = 254;
            // int sent = udp.SendTo(response, 0, 27, SocketFlags.None, remoteEndpoint);

            // Console.WriteLine($"Response sent in {sent} bytes.");
            // for (int i = 0; i < 27; i++)
            // {
            //     // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
            //     Console.Write($"{response[i]:X2} ");
            // }
            // Console.WriteLine();
        }

        public void Start()
        {
            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Parse("172.22.160.1"), 53));

            state.Udp = udp;

            state.UdpOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            state.UdpOut.Bind(new IPEndPoint(IPAddress.Parse("192.168.1.71"), 0));

            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public void Stop()
        {
            state.Udp.Close();
        }
    }
}