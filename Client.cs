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

        public Client()
        {
            ResponseBuffer = new byte[512];
            ResponsePosition = 0;
        }
    }
}