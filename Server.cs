using System;
using System.Net;
using System.Net.Sockets;

namespace mifty
{
    public class Server
    {
        State state = new State();

        public static void ReceiveCallback(IAsyncResult asyncResult)
        {
            State state = (State)asyncResult.AsyncState;
            Socket udp = state.Udp;

            EndPoint remoteEndpoint = null;
            int messageLength = udp.EndReceiveFrom(asyncResult, ref remoteEndpoint);
            // TODO: start decoding the message

        }

        public void Start()
        {
            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Any, 53));

            state.Udp = udp;

            EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            udp.BeginReceiveFrom(state.Buffer, state.Position, state.Buffer.Length, SocketFlags.None, ref remoteEndpoint, new AsyncCallback(ReceiveCallback), state);
        }

        public void Stop()
        {
            state.Udp.Close();
        }
    }
}