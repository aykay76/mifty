using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class State
    {
        // TODO: rename some of these variables
        public Socket Udp { get; set; }
        public Socket UdpOut { get; set; }
        public byte[] Buffer { get; set; }
        public int Position { get; set; }
        public byte[] ResponseBuffer { get; set; }
        public int ResponsePosition { get; set; }
        public IPEndPoint RemoteEndpoint { get; set; }

        // TODO: add some state variables to match requests and responses

        public State()
        {
            Buffer = new byte[512];
            Position = 0;

            ResponseBuffer = new byte[512];
            ResponsePosition = 0;
        }
    }
}