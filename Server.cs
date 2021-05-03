using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mifty
{
    public class Server
    {
        ServerConfig config = null;
        State state = new State();
        NaughtyList naughtyList;

        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            try
            {
                int messageLength = state.Udp.EndReceiveFrom(asyncResult, ref remoteEndpoint);

                // take a copy of the buffer and start receiving again ASAP to service another customer
                byte[] bytes = new byte[messageLength];
                Array.Copy(state.Buffer, bytes, messageLength);

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

                Message message = new Message(bytes);

                // TODO: do some basic checks like does the message contain at least one query?
                IPEndPoint remoteIpEndpoint = remoteEndpoint as IPEndPoint;

                if (state.Server.config.LogLevel >= LogLevel.Info)
                {
                    Console.WriteLine($"[INFO] Request received from {remoteIpEndpoint.Address.ToString()}:{remoteIpEndpoint.Port}; {message.Queries[0].Name}");
                }
                if (state.Server.config.LogLevel >= LogLevel.Trace)
                {
                    Console.WriteLine("[TRACE] Checking naughty list, just once");
                }

                if (state.Server.naughtyList.Contains(message.Queries[0].Name))
                {
                    if (state.Server.config.LogLevel >= LogLevel.Info)
                    {
                        Console.WriteLine($"[INFO] Not forwarding or responding to {message.Queries[0].Name} - it's on the naughty list!");
                    }
                }
                else
                {
                    if (state.Server.config.LogLevel >= LogLevel.Trace)
                    {
                        Console.WriteLine("[TRACE] Creating client and sending request to forwarder");
                    }

                    Client client = new Client();
                    client.Server = state.Server;
                    client.RemoteEndpoint = remoteIpEndpoint;
                    state.Clients[message.ID] = client;

                    client.UdpOut = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    client.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(state.Server.config.ResolverAddress), 0));

                    // for now for now i'm just going to forward to a known DNS server, see what happens
                    int sent = client.UdpOut.SendTo(bytes, 0, messageLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse(state.Server.config.Forwarder), 53));
                    client.UdpOut.BeginReceiveFrom(client.ResponseBuffer, client.ResponsePosition, client.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), client);
                }
            }
            catch (SocketException ex)
            {
                if (state.Server.config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            // receive another request
            if (state.Server.config.LogLevel >= LogLevel.Trace)
            {
                Console.WriteLine("[TRACE] Preparing to receive again...");
            }

            int retryCount = 5;
            int retryTime = 1000;
            while (retryCount > 0)
            {
                try
                {
                    state.Udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);

                    // no need to retry if above doesn't fail
                    retryCount = 0;
                }
                catch (SocketException ex)
                {
                    if (state.Server.config.LogLevel >= LogLevel.Debug)
                    {
                        Console.WriteLine($"[DEBUG] Something went wrong, retrying in {retryTime}ms.");
                        Console.WriteLine(ex.ToString());
                    }
                    retryCount--;
                    retryTime *= 2;
                }

                Thread.Sleep(retryTime);
            }

            if (retryCount < 0)
            {
                // TODO: something went badly wrong, shutdown
            }
        }

        public static void ReceiveResponseCallback(IAsyncResult asyncResult)
        {
            Client client = asyncResult.AsyncState as Client;

            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            int messageLength = client.UdpOut.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            byte[] bytes = new byte[messageLength];
            Array.Copy(client.ResponseBuffer, bytes, messageLength);

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

            Message message = new Message(bytes);
            IPEndPoint forwarder = remoteEndpoint as IPEndPoint;

            if (client.Server.config.LogLevel >= LogLevel.Info)
            {
                Console.WriteLine($"Response received from: {forwarder.Address.ToString()}:{forwarder.Port}");
            }

            // find the right client
            int sent = client.Server.state.Udp.SendTo(bytes, 0, messageLength, SocketFlags.None, client.RemoteEndpoint);
        }

        public Server WithConfig(ServerConfig serverConfig)
        {
            config = serverConfig;
            return this;
        }

        public Server WithNaughtyList(NaughtyList naughtyList)
        {
            this.naughtyList = naughtyList;
            return this;
        }

        // TODO: add restart method for if config changes we can re-listen without completely restarting the service

        public void Start()
        {
            // TODO: add TCP support

            // create a socket that will accept requests from the "client network"
            Socket udp = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Parse(config.ListenAddress), config.ListenPort));

            // Set the SIO_UDP_CONNRESET ioctl to true for this UDP socket. If this UDP socket
            //    ever sends a UDP packet to a remote destination that exists but there is
            //    no socket to receive the packet, an ICMP port unreachable message is returned
            //    to the sender. By default, when this is received the next operation on the
            //    UDP socket that send the packet will receive a SocketException. The native
            //    (Winsock) error that is received is WSAECONNRESET (10054). Since we don't want
            //    to wrap each UDP socket operation in a try/except, we'll disable this error
            //    for the socket with this ioctl call. IOControl is analogous to the WSAIoctl method of Winsock 2
            // Credit: https://www.winsocketdotnetworkprogramming.com/clientserversocketnetworkcommunication8.html
            byte[] inValue = new byte[] { 0, 0, 0, 0 }; // == false
            byte[] outValue = new byte[] { 0, 0, 0, 0 }; // initialize to 0
            udp.IOControl(-1744830452, inValue, null);

            state.Server = this;
            state.Udp = udp;

            // TODO: need to decide whether to include this as an option.
            //       On the one hand opening a new socket for every request could lead to port exhaustion on a busy network
            //       On the other hand, using one connection the requests need to be synchronised to avoid buffer overlaps
            //       On the third hand I wish I had, I could have a pool of connections (configurable) which could be utilised based on some busy state flag being clear?
            // create a socket that will be used to forward requests on
            // state.UdpOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // state.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(config.ResolverAddress), 0));

            // and begin...
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public void Stop()
        {
            state.Udp.Close();
            // state.UdpOut.Close();

            foreach (Client client in state.Clients.Values)
            {
                client.UdpOut.Close();
            }
        }
    }
}