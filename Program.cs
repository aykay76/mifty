using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace mifty
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: make this configurable to look in a specific directory, and have an option to load async or not
            dsl.MasterFileParser parser = new dsl.MasterFileParser();
            parser.Parse("example.zone");

            Catalogue catalogue = Catalogue.FromEntryList(parser.Entries);

            Console.WriteLine("Read master file(s) successfully");

            var exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                                  eventArgs.Cancel = true;
                                  exitEvent.Set();
                              };

            NaughtyList naughtyList = NaughtyList.FromFile("dnscrypt-proxy.blacklist.txt");

            // Create the server with config loaded from file
            // TODO: allow config file to be passed on command line
            //       make catalogue and naughty list optional as appropriate
            Server server = new Server();
            server.WithConfig(ServerConfig.FromFile(Environment.CurrentDirectory + "\\windows.json")).WithCatalogue(catalogue).WithNaughtyList(naughtyList).Start();

            // TODO: make this a command line arg with default
            FileSystemWatcher watcher = new FileSystemWatcher(Environment.CurrentDirectory, "*.json");
            watcher.Changed += (o,e) => {
                // wait a bit in case of race condition, it doesn't matter if it takes half a second to reload configuration
                Thread.Sleep(500);
                Console.WriteLine("Configuration changed, restarting server");
                ServerConfig config = ServerConfig.FromFile(Environment.CurrentDirectory + "\\windows.json");
                server.WithConfig(config).Restart();
            };
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Ready to serve...");
            exitEvent.WaitOne();

            Console.WriteLine("Shutting down...");
            server.Stop();
        }
    }
}

