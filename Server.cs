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

            if (state.Server.config.LogLevel == LogLevel.Debug)
            {
                Console.WriteLine("Response received from forwarder:");
                for (int i = 0; i < messageLength; i++)
                {
                    // TODO: link the response with the original request through state somehow
                    // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                    Console.Write($"{bytes[i]:X2} ");
                    if (i % 16 == 15) Console.WriteLine();
                }
                Console.WriteLine();
            }

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

            if (state.Server.config.LogLevel == LogLevel.Debug)
            {
                Console.WriteLine("Message received:");
                for (int i = 0; i < messageLength; i++)
                {
                    // TODO: decide what i'm going to do here
                    // TODO: add configurable log levels
                    // TODO: decode names and the other fields, output nice logs
                    // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                    Console.Write($"{bytes[i]:X2} ");
                    if (i % 16 == 15) Console.WriteLine();
                }
                Console.WriteLine();
            }

            // for now for now i'm just going to forward to a known DNS server, see what happens
            int sent = state.UdpOut.SendTo(bytes, 0, messageLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse(state.Server.config.Forwarder), 53));

            state.UdpOut.BeginReceiveFrom(state.ResponseBuffer, state.ResponsePosition, state.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), state);
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