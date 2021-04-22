using System;
using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class Server
    {
        ServerConfig config = null;
        State state = new State();

        public static void ReceiveResponseCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            int messageLength = state.UdpOut.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            byte[] bytes = new byte[messageLength];
            Array.Copy(state.ResponseBuffer, bytes, messageLength);
            Console.WriteLine("Response received from forwarder:");
            for (int i = 0; i < messageLength; i++)
            {
                // TODO: link the response with the original request through state somehow
                // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                Console.Write($"{bytes[i]:X2} ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }
            Console.WriteLine();

            // send response to client
            Console.WriteLine("Sending back to client");

            Message message = new Message(bytes);

            // find the right client
            int sent = state.Udp.SendTo(bytes, 0, messageLength, SocketFlags.None, state.Clients[message.ID]);
        }

        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            Socket udp = state.Udp;

            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            int messageLength = udp.EndReceiveFrom(asyncResult, ref remoteEndpoint);

            // take a copy of the buffer and start receiving again ASAP to service another customer
            byte[] bytes = new byte[messageLength];
            Array.Copy(state.Buffer, bytes, messageLength);
            Message message = new Message(bytes);

            state.Clients[message.ID] = remoteEndpoint as IPEndPoint;

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
                // TODO: decide what i'm going to do here
                // TODO: add configurable log levels
                // TODO: decode names and the other fields, output nice logs
                // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                Console.Write($"{bytes[i]:X2} ");
            }
            Console.WriteLine();

            // for now for now i'm just going to forward to a known DNS server, see what happens
            int sent = state.UdpOut.SendTo(bytes, 0, messageLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse(state.Server.config.Forwarder), 53));
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

        public Server WithConfig(ServerConfig serverConfig)
        {
            config = serverConfig;
            return this;
        }

        public void Start()
        {
            // TODO: add TCP support

            // create a socket that will accept requests from the "client network"
            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Parse(config.ServerAddress), config.ServerPort));

            state.Server = this;
            state.Udp = udp;

            // create a socket that will be used to forward requests on
            state.UdpOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            state.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(config.ResolverAddress), 0));

            // and begin...
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public void Stop()
        {
            state.Udp.Close();
            state.UdpOut.Close();
        }
    }
}