using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class Client
    {
        public Server Server { get; set; }
        public IPEndPoint RemoteEndpoint { get; set; }
        public Socket UdpOut { get; set; }
        public byte[] ResponseBuffer { get; set; }
        public int ResponsePosition { get; set; }
        public int IPVersion { get; set; }

        public Client()
        {
            // initialise response buffer
            ResponseBuffer = new byte[512];
            ResponsePosition = 0;

            // assume IPv6
            IPVersion = 6;
        }
    }
}