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

            Server server = new Server();
            server.Start();

            Console.WriteLine("Hello World!");
            exitEvent.WaitOne();

            server.Stop();
        }
    }
}
