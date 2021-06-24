using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class State
    {
        // TODO: rename some of these variables
        public Socket UdpV6 { get; set; }
        public Socket UdpOut { get; set; }
        public byte[] Buffer { get; set; }
        public int Position { get; set; }
        public byte[] ResponseBuffer { get; set; }
        public int ResponsePosition { get; set; }
        public Server Server { get; set; }

        // TODO: I might replace this with a more rich object than just endpoint if I need to store more information
        //       for example, add timestamp so that I can measure latency
        //       requests per client
        //       throttling
        public Dictionary<ushort, Client> Clients { get; set; }

        public State()
        {
            Buffer = new byte[512];
            Position = 0;

            ResponseBuffer = new byte[512];
            ResponsePosition = 0;

            Clients = new Dictionary<ushort, Client>();
        }
    }
}