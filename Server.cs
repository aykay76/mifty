using System;
using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class Server
    {
        ServerConfig config = null;
        State state = new State();

        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            Socket udp = state.Udp;
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                int messageLength = udp.EndReceiveFrom(asyncResult, ref remoteEndpoint);

                // take a copy of the buffer and start receiving again ASAP to service another customer
                byte[] bytes = new byte[messageLength];
                Array.Copy(state.Buffer, bytes, messageLength);
                Message message = new Message(bytes);

                Client client = new Client();
                client.Server = state.Server;
                client.RemoteEndpoint = remoteEndpoint as IPEndPoint;
                state.Clients[message.ID] = client;

                if (state.Server.config.LogLevel >= LogLevel.Info)
                {
                    Console.WriteLine($"[INFO] Request received from {client.RemoteEndpoint.Address.ToString()}:{client.RemoteEndpoint.Port}; {message.Queries[0].Name}");
                }

                if (state.Server.config.LogLevel >= LogLevel.Debug)
                {
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

                client.UdpOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                client.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(state.Server.config.ResolverAddress), 0));

                // for now for now i'm just going to forward to a known DNS server, see what happens
                int sent = client.UdpOut.SendTo(bytes, 0, messageLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse(state.Server.config.Forwarder), 53));

                client.UdpOut.BeginReceiveFrom(client.ResponseBuffer, client.ResponsePosition, client.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), client);
            }
            catch (SocketException)
            {

            }

            // receive another request
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public static void ReceiveResponseCallback(IAsyncResult asyncResult)
        {
            Client client = asyncResult.AsyncState as Client;

            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            int messageLength = client.UdpOut.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            byte[] bytes = new byte[messageLength];
            Array.Copy(client.ResponseBuffer, bytes, messageLength);

            Message message = new Message(bytes);
            IPEndPoint forwarder = remoteEndpoint as IPEndPoint;

            if (client.Server.config.LogLevel >= LogLevel.Info)
            {
                Console.WriteLine($"Response received from: {forwarder.Address.ToString()}:{forwarder.Port}");
            }

            if (client.Server.config.LogLevel >= LogLevel.Debug)
            {
                for (int i = 0; i < messageLength; i++)
                {
                    // TODO: link the response with the original request through state somehow
                    // TODO: when decoding labels, don't forget pointers - per section 4.1.4 of RFC1035
                    Console.Write($"{bytes[i]:X2} ");
                    if (i % 16 == 15) Console.WriteLine();
                }
                Console.WriteLine();
            }

            // find the right client
            int sent = client.Server.state.Udp.SendTo(bytes, 0, messageLength, SocketFlags.None, client.RemoteEndpoint);
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
            // state.UdpOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // state.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(config.ResolverAddress), 0));

            // and begin...
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public void Stop()
        {
            state.Udp.Close();
            // state.UdpOut.Close();
        }
    }
}