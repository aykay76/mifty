using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Prometheus;

// TODO: ensure compliance with RFC especially around TTL and other aspects

namespace mifty
{
    public class Server
    {
        private static readonly Counter totalRequestCounter = Metrics.CreateCounter("mifty_requests_total", "Total number of requests");
        private static readonly Counter blockedRequestCounter = Metrics.CreateCounter("mifty_requests_blocked", "Number of requests blocked by naughty list");

        ServerConfig config = null;
        NaughtyList naughtyList;
        Catalogue catalogue;

        // TODO: group these into a listening endpoint object that can be reused
        private Socket UdpV6;
        private IAsyncResult ar6;
        private byte[] BufferV6;
        private int PositionV6;

        private Socket UdpV4;
        private IAsyncResult ar4;
        private byte[] BufferV4;
        private int PositionV4;


        // TODO: I might replace this with a more rich object than just endpoint if I need to store more information
        //       for example, add timestamp so that I can measure latency
        //       requests per client
        //       throttling 
        public Dictionary<ushort, Client> Clients { get; set; }

        public Server()
        {
            BufferV6 = new byte[512];
            PositionV6 = 0;
            BufferV4 = new byte[512];
            PositionV4 = 0;

            Clients = new Dictionary<ushort, Client>();
        }

        public static void ReceiveCallbackV6(IAsyncResult asyncResult)
        {
            Server server = (Server)asyncResult.AsyncState;
            // if (asyncResult == server.ar6)
            // {
            server.CommonCallback(asyncResult, 6);
            // }
        }

        public static void ReceiveCallbackV4(IAsyncResult asyncResult)
        {
            Server server = (Server)asyncResult.AsyncState;
            // if (asyncResult == server.ar4)
            // {
            server.CommonCallback(asyncResult, 4);
            // }
        }

        protected void FindAnswers(Query query, Message message)
        {
            List<Answer> answers = catalogue.FindEntry(query);

            if (answers == null) return;

            foreach (Answer answer in answers)
            {
                // add answer, whatever it is
                message.AddAnswer(answer);

                // if it's a CNAME recursively check
                if (answer.Type == QueryType.CNAME)
                {
                    Query newQuery = new Query();
                    newQuery.Class = query.Class;
                    newQuery.Type = query.Type;
                    newQuery.Name = answer.DataString;
                    FindAnswers(newQuery, message);
                }
            }
        }

        protected void ProcessQuery(Message message, int ipVersion, IPEndPoint remoteIpEndpoint)
        {
            for (int q = 0; q < message.QueryCount; q++)
            {
                // TODO: re-factor this
                if (config.LogLevel >= LogLevel.Info)
                {
                    Console.WriteLine($"[INFO] {message.ID} Request received from {remoteIpEndpoint.Address.ToString()}:{remoteIpEndpoint.Port}; {message.Queries[q].Name}");
                }

                if (config.LogLevel >= LogLevel.Trace)
                {
                    Console.WriteLine("[TRACE] Checking naughty list, just once 😉");
                }

                if (naughtyList != null && naughtyList.Match(message.Queries[q].Name))
                {
                    blockedRequestCounter.Inc();

                    if (config.LogLevel >= LogLevel.Info)
                    {
                        Console.WriteLine($"[INFO] Not forwarding or responding to {message.Queries[q].Name} - it's on the naughty list! 😯");
                    }
                }
                else
                {
                    if (config.LogLevel >= LogLevel.Trace)
                    {
                        Console.WriteLine("[TRACE] Creating client and sending request to forwarder");
                    }

                    // do i have a match in my catalogue?
                    FindAnswers(message.Queries[q], message);
                    
                    if (message.AnswerCount == 0)
                    {
                        // TODO: check cache - I may not need to go to the network at all

                        // create a new client
                        Client client = new Client();
                        client.Server = this;
                        client.IPVersion = ipVersion;
                        client.RemoteEndpoint = remoteIpEndpoint;
                        Clients[message.ID] = client;

                        // make IPv4/IPv6 configurable or automatic based on config options
                        if (ipVersion == 6)
                        {
                            client.UdpOut = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                            client.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(config.ResolverAddressV6), 0));
                        }
                        else
                        {
                            client.UdpOut = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            client.UdpOut.Bind(new IPEndPoint(IPAddress.Parse(config.ResolverAddressV4), 0));
                        }

                        if (ipVersion == 6)
                        {
                            foreach (string forwarder in config.ForwardersV6)
                            {
                                EndPoint dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                                int sent = client.UdpOut.SendTo(message.Bytes, 0, message.Bytes.Length, SocketFlags.None, new IPEndPoint(IPAddress.Parse(forwarder), 53));
                                client.UdpOut.BeginReceiveFrom(client.ResponseBuffer, client.ResponsePosition, client.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), client);
                            }
                        }
                        else
                        {
                            foreach (string forwarder in config.ForwardersV4)
                            {
                                EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
                                int sent = client.UdpOut.SendTo(message.Bytes, 0, message.Bytes.Length, SocketFlags.None, new IPEndPoint(IPAddress.Parse(forwarder), 53));
                                client.UdpOut.BeginReceiveFrom(client.ResponseBuffer, client.ResponsePosition, client.ResponseBuffer.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveResponseCallback), client);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("I have authority, need to construct response");

                        for (int i = 0; i < message.Bytes.Length; i++)
                        {
                            // TODO: decode names and the other fields, output nice logs
                            Console.Write($"{message.Bytes[i]:X2} ");
                            if (i % 16 == 15) Console.WriteLine();
                        }
                        Console.WriteLine();
                        Console.WriteLine($"Total bytes: {message.Bytes.Length}");

                        // send straight back to requester
                        int sent = 0;
                        if (ipVersion == 6)
                        {
                            sent = UdpV6.SendTo(message.Bytes, 0, message.Bytes.Length, SocketFlags.None, remoteIpEndpoint);
                        }
                        else
                        {
                            sent = UdpV4.SendTo(message.Bytes, 0, message.Bytes.Length, SocketFlags.None, remoteIpEndpoint);
                        }
                    }
                }
            }
        }

        protected string ZoneOf(string name)
        {
            int index = name.IndexOf('.');
            if (index == -1) return string.Empty;

            return name.Substring(index + 1);
        }

        protected void ProcessUpdate(Message message)
        {
            // see 3.1.2 of RFC 2136
            if (message.ZoneCount != 1 || message.Zones[0].Type != QueryType.SOA)
            {
                message.ResponseCode = ResponseCode.FormatError;
                return;
            }

            // TODO: if i'm a slave (not yet implemented masters and slaves) need to forward to master for zone - assuming master for now
            // TODO: to do this and to handle persistent storage requirement before responding - I need to make this async and build some
            // TODO: kind of context object that can keep track of in-flight requests

            Catalogue c = catalogue.FindFQDN(message.Zones[0].Name);

            // 3.2 Check pre-requisites
            // see section 3.2.5 of RFC 2136
            foreach (Answer requisite in message.Prerequisites)
            {
                if (requisite.TimeToLive != 0)
                {
                    message.ResponseCode = ResponseCode.FormatError;
                    return;
                }

                if (ZoneOf(requisite.Name) != message.Zones[0].Name)
                {
                    message.ResponseCode = ResponseCode.NotZone;
                    return;
                }

                if (requisite.Class == QueryClass.All)
                {
                    if (requisite.Length != 0)
                    {
                        message.ResponseCode = ResponseCode.FormatError;
                        return;
                    }

                    if (requisite.Type == QueryType.All)
                    {
                        message.ResponseCode = ResponseCode.NameError;
                        return;
                    }
                    else
                    {
                        // TODO: if (!zone_rrset<rr.name, rr.type>)
                        message.ResponseCode = ResponseCode.NXRRSET;
                        return;
                    }
                }

                if (requisite.Class == QueryClass.None)
                {
                    if (requisite.Length != 0)
                    {
                        message.ResponseCode = ResponseCode.FormatError;
                        return;
                    }

                    if (requisite.Type == QueryType.All)
                    {
                        // TODO:if (zone_name<rr.name>)
                        message.ResponseCode = ResponseCode.YXDOMAIN;
                        return;
                    }
                    else
                    {
                        // TODO: if (!zone_rrset<rr.name, rr.type>)
                        message.ResponseCode = ResponseCode.YXRRSET;
                        return;
                    }
                }

                if (requisite.Class == message.Zones[0].Class)
                {
                    // TODO: add to some temp structure for below check that all supplied rrsets are valid
                }
                else
                {
                    message.ResponseCode = ResponseCode.FormatError;
                    return;
                }
            }

            // TODO: check prerequisites match
            // for rrset in temp
            //     if (zone_rrset<rrset.name, rrset.type> != rrset)
            //             return (NXRRSET)

            // 3.3 Check permissions
            // TODO: implement permissions!! :)

            // 3.4.1 Pre-scan to check formats etc.
            foreach (Answer update in message.Updates)
            {
                if (ZoneOf(update.Name) != message.Zones[0].Name)
                {
                    message.ResponseCode = ResponseCode.NotZone;
                    return;
                }

                if (update.Class == message.Zones[0].Class)
                {
                    if (update.Type == QueryType.All || update.Type == QueryType.AFXR || update.Type == QueryType.MAILA || update.Type == QueryType.MAILB)
                    {
                        message.ResponseCode = ResponseCode.FormatError;
                        return;
                    }
                }
                else if (update.Class == QueryClass.All)
                {
                    if (update.TimeToLive != 0 || update.Length != 0 || (update.Type == QueryType.AFXR || update.Type == QueryType.MAILA || update.Type == QueryType.MAILB))
                    {
                        message.ResponseCode = ResponseCode.FormatError;
                        return;
                    }
                }
                else if (update.Class == QueryClass.None)
                {
                    if (update.TimeToLive != 0 || (update.Type == QueryType.All || update.Type == QueryType.AFXR || update.Type == QueryType.MAILA || update.Type == QueryType.MAILB))
                    {
                        message.ResponseCode = ResponseCode.FormatError;
                        return;
                    }
                }
                else
                {
                    message.ResponseCode = ResponseCode.FormatError;
                    return;
                }
            }

            // TODO: 3.4.2 apply updates
            // [rr] for rr in updates
            // TODO: get the Catalogue child entry that matches the zone[0] in the update message
            // that's the zone we're operating in, and will speed up the below FindEntryX calls

            foreach (Answer rr in message.Updates)
            {
                //     if (rr.class == zclass)
                if (rr.Class == message.Zones[0].Class)
                {
                    //             if (rr.type == CNAME)
                    //                 if (zone_rrset<rr.name, ~CNAME>)
                    //                     next [rr]
                    //             elsif (zone_rrset<rr.name, CNAME>)
                    //                 next [rr]
                    var cnames = c.FindEntry(rr.Class, QueryType.CNAME, rr.Name);
                    if (rr.Type == QueryType.CNAME)
                    {
                        // not sure if the rr.name is FQDN - if not, need to add zone name
                        var result = c.FindEntryNotType(rr.Class, rr.Type, rr.Name);
                        if (result.Count > 0)
                        {
                            continue;
                        }
                    }
                    else if (cnames.Count > 0)
                    {
                        continue;
                    }
                    
                    //             if (rr.type == SOA)
                    //                 if (!zone_rrset<rr.name, SOA> ||
                    //                     zone_rr<rr.name, SOA>.serial > rr.soa.serial)
                    //                     next [rr]
                    if (rr.Type == QueryType.SOA)
                    {
                        var soa = c.FindEntry(rr.Class, QueryType.SOA, rr.Name);
                        if (soa.Count == 0 || soa[0].DataString != rr.DataString)
                        {
                            continue;
                        }
                    }

                    //             for zrr in zone_rrset<rr.name, rr.type>
                    //                 if (rr.type == CNAME || rr.type == SOA ||
                    //                     (rr.type == WKS && rr.proto == zrr.proto &&
                    //                     rr.address == zrr.address) ||
                    //                     rr.rdata == zrr.rdata)
                    //                     zrr = rr
                    //                     next [rr]
                    //             zone_rrset<rr.name, rr.type> += rr
                    // TODO: based on above pseudocode work out whether to replace or add to the record set
                    // thism ay not beed the restructure at the top of Catalogue but I will need to add
                    // methods to add or replace entries
                }
                else if (rr.Class == QueryClass.All)
                {
                    //    3.4.2.3. For any Update RR whose CLASS is ANY and whose TYPE is ANY,
                    //    all Zone RRs with the same NAME are deleted, unless the NAME is the
                    //    same as ZNAME in which case only those RRs whose TYPE is other than
                    //    SOA or NS are deleted.  For any Update RR whose CLASS is ANY and
                    //    whose TYPE is not ANY all Zone RRs with the same NAME and TYPE are
                    //    deleted, unless the NAME is the same as ZNAME in which case neither
                    //    SOA or NS RRs will be deleted.
                    if (rr.Type == QueryType.All)
                    {
                    }
                    else if (rr.Name == message.Zones[0].Name && (rr.Type == QueryType.SOA || rr.Type == QueryType .NS))
                    {
                        continue;
                    }
                    else
                    {
                        // TODO: null/remove the matching zone rrset
                    }

                    //     elsif (rr.class == ANY)
                    //             if (rr.type == ANY)
                    //                 if (rr.name == zname)
                    //                     zone_rrset<rr.name, ~(SOA|NS)> = Nil
                    //                 else
                    //                     zone_rrset<rr.name, *> = Nil
                    //             elsif (rr.name == zname &&
                    //                 (rr.type == SOA || rr.type == NS))
                    //                 next [rr]
                    //             else
                    //                 zone_rrset<rr.name, rr.type> = Nil
                }
                else if (rr.Class == QueryClass.None)
                {
                    //     elsif (rr.class == NONE)
                    //             if (rr.type == SOA)
                    //                 next [rr]
                    //             if (rr.type == NS && zone_rrset<rr.name, NS> == rr)
                    //                 next [rr]
                    //             zone_rr<rr.name, rr.type, rr.data> = Nil

                }
            }
            // return (NOERROR)
            
            // TODO: construct and send response
        }

        public void CommonCallback(IAsyncResult asyncResult, int ipVersion)
        {
            EndPoint dummyEndpoint = null;
            EndPoint remoteEndpoint = null;

            if (ipVersion == 6)
            {
                dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                remoteEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            }
            else
            {
                dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
                remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            }

            try
            {
                int messageLength = 0;
                byte[] bytes = null;

                // take a copy of the buffer and start receiving again ASAP to service another customer
                if (ipVersion == 6)
                {
                    messageLength = UdpV6.EndReceiveFrom(asyncResult, ref remoteEndpoint);
                    bytes = new byte[messageLength];
                    Array.Copy(BufferV6, bytes, messageLength);
                }
                else
                {
                    messageLength = UdpV4.EndReceiveFrom(asyncResult, ref remoteEndpoint);
                    bytes = new byte[messageLength];
                    Array.Copy(BufferV4, bytes, messageLength);
                }

                Message message = new Message(bytes);
                IPEndPoint remoteIpEndpoint = remoteEndpoint as IPEndPoint;

                if (message.Opcode == Opcode.StandardQuery)
                {
                    ProcessQuery(message, ipVersion, remoteIpEndpoint);
                }
                else if (message.Opcode == Opcode.Update)
                {
                    ProcessUpdate(message);
                }
                else
                {
                    message.ResponseCode = ResponseCode.NotImplemented;
                }

                totalRequestCounter.Inc();

                if (config.LogLevel >= LogLevel.Debug)
                {
                    for (int i = 0; i < messageLength; i++)
                    {
                        Console.Write($"{bytes[i]:X2} ");
                        if (i % 16 == 15) Console.WriteLine();
                    }
                    Console.WriteLine();
                    Console.WriteLine($"Total bytes: {messageLength}");
                }
            }
            catch (ArgumentException ex)
            {
                if (config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine("[DEBUG] Threading issue, server restarted due to configuration change?");
                    Console.WriteLine(ex.ToString());
                }
            }
            catch (ObjectDisposedException)
            {
                if (config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine("[DEBUG] Object disposed, server restarted due to configuration change?");
                }
            }
            catch (SocketException ex)
            {
                if (config.LogLevel >= LogLevel.Debug)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            // receive another request
            if (config.LogLevel >= LogLevel.Trace)
            {
                Console.WriteLine("[TRACE] Preparing to receive again...");
            }

            int retryCount = 5;
            int retryTime = 1000;
            while (retryCount > 0)
            {
                try
                {
                    if (ipVersion == 6)
                    {
                        ar6 = UdpV6.BeginReceiveFrom(BufferV6, PositionV6, BufferV6.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallbackV6), this);
                    }
                    else
                    {
                        ar4 = UdpV4.BeginReceiveFrom(BufferV4, PositionV4, BufferV4.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallbackV4), this);
                    }

                    // no need to retry if above doesn't fail
                    retryCount = 0;
                }
                catch (SocketException ex)
                {
                    if (config.LogLevel >= LogLevel.Debug)
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

            Message message = new Message(bytes);
            IPEndPoint forwarder = remoteEndpoint as IPEndPoint;

            if (client.Server.config.LogLevel >= LogLevel.Info)
            {
                Console.WriteLine($"[INFO] {message.ID} Response received from: {forwarder.Address.ToString()}:{forwarder.Port}");
            }

            // TODO: implement caching, being sure to respect TTL etc.
            //       when caching responses, remember that the ID will change on a subsequent request
            //       need to easily retrieve from the cache based on query class and type
            // TODO: also need a mechanism to count down from TTL and remove from cache when stale

            if (client.Server.config.LogLevel >= LogLevel.Debug)
            {
                for (int i = 0; i < messageLength; i++)
                {
                    Console.Write($"{bytes[i]:X2} ");
                    if (i % 16 == 15) Console.WriteLine();
                }
                Console.WriteLine();
            }

            // find the right client
            int sent = 0;
            if (client.IPVersion == 6)
            {
                sent = client.Server.UdpV6.SendTo(bytes, 0, messageLength, SocketFlags.None, client.RemoteEndpoint);
            }
            else
            {
                sent = client.Server.UdpV4.SendTo(bytes, 0, messageLength, SocketFlags.None, client.RemoteEndpoint);
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

            // make v4/v6 automatic based on configuration
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    byte[] inValue = new byte[] { 0, 0, 0, 0 }; // == false
                    UdpV6.IOControl(-1744830452, inValue, null);
                }
                
                // and begin...
                EndPoint dummyEndpoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                ar6 = UdpV6.BeginReceiveFrom(BufferV6, PositionV6, BufferV6.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallbackV6), this);
            }

            if (!string.IsNullOrEmpty(config.ListenAddressV4))
            {
                Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udp.Bind(new IPEndPoint(IPAddress.Parse(config.ListenAddressV4), config.ListenPort));
                UdpV4 = udp;

                // Set the SIO_UDP_CONNRESET ioctl to true for this UDP socket. If this UDP socket
                //    ever sends a UDP packet to a remote destination that exists but there is
                //    no socket to receive the packet, an ICMP port unreachable message is returned
                //    to the sender. By default, when this is received the next operation on the
                //    UDP socket that send the packet will receive a SocketException. The native
                //    (Winsock) error that is received is WSAECONNRESET (10054). Since we don't want
                //    to wrap each UDP socket operation in a try/except, we'll disable this error
                //    for the socket with this ioctl call. IOControl is analogous to the WSAIoctl method of Winsock 2
                // Credit: https://www.winsocketdotnetworkprogramming.com/clientserversocketnetworkcommunication8.html
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    byte[] inValue = new byte[] { 0, 0, 0, 0 }; // == false
                    UdpV4.IOControl(-1744830452, inValue, null);
                }

                // and begin...
                EndPoint dummyEndpoint = new IPEndPoint(IPAddress.Any, 0);
                ar4 = UdpV4.BeginReceiveFrom(BufferV4, PositionV4, BufferV4.Length, SocketFlags.None, ref dummyEndpoint, new AsyncCallback(ReceiveCallbackV4), this);
            }
        }

        public void Stop()
        {
            if (UdpV6 != null)
            {
                UdpV6.Close();
            }

            if (UdpV4 != null)
            {
                UdpV4.Close();
            }

            foreach (Client client in Clients.Values)
            {
                client.UdpOut.Close();
            }
        }
    }
}