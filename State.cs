using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class State
    {
        public Socket Udp { get; set; }
        public byte[] Buffer { get; set; }
        public int Position { get; set; }
        public IPEndPoint RemoteEndpoint { get; set; }

        public State()
        {
            Buffer = new byte[512];
            Position = 0;
        }
    }
}