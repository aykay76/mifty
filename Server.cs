using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mifty
{
    public class Server
    {
        ServerConfig config = null;
        NaughtyList naughtyList;
        Catalogue catalogue;
        public Socket UdpV6 { get; set; }
        public Socket UdpOut { get; set; }
        public byte[] BufferV6 { get; set; }
        public int PositionV6 { get; set; }
        public byte[] ResponseBufferV6 { get; set; }
        public int ResponsePositionV6 { get; set; }

        // TODO: I might replace this with a more rich object than just endpoint if I need to store more information
        //       for example, add timestamp so that I can measure latency
        //       requests per client
        //       throttling 
        public Dictionary<ushort, Client> Clients { get; set; }

        public Server()
        {
            BufferV6 = new byte[512];
            PositionV6 = 0;

            ResponseBufferV6 = new byte[512];
            ResponsePositionV6 = 0;

            Clients = new Dictionary<ushort, Client>();
        }

        // TODO: refactor to have v4 callback and v6 callback that will call a common function to process the incoming request
        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            Server server = (Server)asyncResult.AsyncState;
            EndPoint dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            try
            {
                int messageLength = server.UdpV6.EndReceiveFrom(asyncResult, ref remoteEndpoint);

                // take a copy of the buffer and start receiving again ASAP to service another customer
                byte[] bytes = new byte[messageLength];
                Array.Copy(server.BufferV6, bytes, messageLength);

                if (server.config.LogLevel >= LogLevel.Debug)
                {
                    for (int i = 0; i < messageLength; i++)
                    {
                        // TODO: decode names and the other fields, output nice logs
                        Console.Write($"{bytes[i]:X2} ");
                        if (i % 16 == 15) Console.WriteLine();
                    }
                    Console.WriteLine();
                }

                Message message = new Message(bytes);

                // TODO: do some basic checks like does the message contain at least one query?
                IPEndPoint remoteIpEndpoint = remoteEndpoint as IPEndPoint;

                if (server.config.LogLevel >= LogLevel.Info)
                {
                    Console.WriteLine($"[INFO] Request received from {remoteIpEndpoint.Address.ToString()}:{remoteIpEndpoint.Port}; {message.Queries[0].Name}");
                }
                if (server.config.LogLevel >= LogLevel.Trace)
                {
                    Console.WriteLine("[TRACE] Checking naughty list, just once");
                }

                if (server.naughtyList != null && server.naughtyList.Contains(message.Queries[0].Name))
                {
                    if (server.config.LogLevel >= LogLevel.Info)
                    {
                        Console.WriteLine($"[INFO] Not forwarding or responding to {message.Queries[0].Name} - it's on the naughty list!");
                    }
                }
                else
                {
                    if (server.config.LogLevel >= LogLevel.Trace)
                    {
                        Console.WriteLine("[TRACE] Creating client and sending request to forwarder");
                    }

                    // create a new client
                    Client client = new Client();
                    client.Server = server;
                    client.RemoteEndpoint = remoteIpEndpoint;
                    server.Clients[message.ID] = client;

                    // TODO: make IPv4/IPv6 configurable or automatic based on config options
                    client.UdpOut = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    client.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(server.config.ResolverAddressV6), 0));

                    // for now for now i'm just going to forward to a known DNS server, see what happens
                    // TODO: if there are multiple do we send to all or just one at a time?
                    foreach (string forwarder in server.config.ForwardersV6)
                    {
                        int sent = client.UdpOut.SendTo(bytes, 0, messageLength, SocketFlags.None, new IPEndPoint(IPAddress.Parse(forwarder), 53));
                        client.UdpOut.BeginReceiveFrom(client.ResponseBuffer, client.ResponsePosition, client.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), client);
                    }
                }
            }
            catch (ArgumentException)
            {
                if (server.config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine("[DEBUG] Threading issue, server restarted due to configuration change?");
                }
            }
            catch (ObjectDisposedException)
            {
                if (server.config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine("[DEBUG] Object disposed, server restarted due to configuration change?");
                }
            }
            catch (SocketException ex)
            {
                if (server.config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            // receive another request
            if (server.config.LogLevel >= LogLevel.Trace)
            {
                Console.WriteLine("[TRACE] Preparing to receive again...");
            }

            int retryCount = 5;
            int retryTime = 1000;
            while (retryCount >= 0)
            {
                try
                {
                    server.UdpV6.BeginReceiveFrom(server.BufferV6, server.PositionV6, server.BufferV6.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), server);

                    // no need to retry if above doesn't fail
                    retryCount = 0;
                }
                catch (SocketException ex)
                {
                    if (server.config.LogLevel >= LogLevel.Debug)
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

            EndPoint remoteEndpoint = null;
            if (client.IPVersion == 6)
            {
                remoteEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            }
            else
            {
                remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            }
            int messageLength = client.UdpOut.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            byte[] bytes = new byte[messageLength];
            Array.Copy(client.ResponseBuffer, bytes, messageLength);

            if (client.Server.config.LogLevel >= LogLevel.Debug)
            {
                for (int i = 0; i < messageLength; i++)
                {
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
            int sent = 0;
            if (client.IPVersion == 6)
            {
                sent = client.Server.UdpV6.SendTo(bytes, 0, messageLength, SocketFlags.None, client.RemoteEndpoint);
            }
            else
            {
                // TODO: send back to v4 UDP socket
            }
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

        public Server WithCatalogue(Catalogue catalogue)
        {
            this.catalogue = catalogue;
            return this;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Start()
        {
            // TODO: add TCP support? maybe for future if doing zone transfers, otherwise it probably isn't needed

            // TODO: need to decide whether to include this as an option.
            //       On the one hand opening a new socket for every request could lead to port exhaustion on a busy network
            //       On the other hand, using one connection the requests need to be synchronised to avoid buffer overlaps
            //       On the third hand I wish I had, I could have a pool of connections (configurable) which could be utilised based on some busy state flag being clear?
            // create a socket that will accept requests from the "client network"

            // TODO: make v4/v6 automatic based on configuration
            if (!string.IsNullOrEmpty(config.ListenAddressV6))
            {
                Socket udp = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                udp.Bind(new IPEndPoint(IPAddress.Parse(config.ListenAddressV6), config.ListenPort));
                UdpV6 = udp;

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
                udp.IOControl(-1744830452, inValue, null);

                // and begin...
                EndPoint dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                udp.BeginReceiveFrom(BufferV6, PositionV6, BufferV6.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallback), this);
            }
        }

        public void Stop()
        {
            if (UdpV6 != null)
            {
                UdpV6.Close();
            }

            foreach (Client client in Clients.Values)
            {
                client.UdpOut.Close();
            }
        }
    }
}