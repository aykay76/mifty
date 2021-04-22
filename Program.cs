using System;
using System.Threading;

namespace mifty
{
    class Program
    {
        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                                  eventArgs.Cancel = true;
                                  exitEvent.Set();
                              };

            // TODO: add a server config and associated command line arguments
            // for what this thing will do - addresses to bind to etc.
            Server server = new Server();
            server.WithConfig(new ServerConfig {
                ServerAddress = "172.22.160.1",
                ResolverAddress = "192.168.1.71",
                ServerPort = 53,
                Forwarder = "192.168.1.254"
            }).Start();

            Console.WriteLine("Hello World!");
            exitEvent.WaitOne();

            server.Stop();
        }
    }
}
