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
            // ok, in the spirit of this project I grant that this parsing approach is not the fastest but it's one time to load the file(s)
            // and it provides robustness to the process - I will optimise the in-memory representation of the master zone records so that
            // this can be as fast as possible (maybe not quite faster than light but hopefully fast enough)
            dsl.MasterFileParser parser = new dsl.MasterFileParser();
            parser.Parse("example.zone");

            Catalogue catalogue = Catalogue.FromEntryList(parser.Entries);

            Console.WriteLine("read master file successfully");

            // Message message = Message.FromFile("badanswer.txt");

            // temporary, i want to do some analysis of the block list to see how evenly distributed the entries are
            // Dictionary<char, Dictionary<char, int>> frequencies = new Dictionary<char, Dictionary<char, int>>();
            // StreamReader reader = new StreamReader("dnscrypt-proxy.blacklist.txt");
            // string line = string.Empty;
            // while ((line = reader.ReadLine()) != null)
            // {

            //     if (frequencies.ContainsKey(line[0]) == false)
            //     {
            //         frequencies.Add(line[0], new Dictionary<char, int>());
            //     }

            //     if (frequencies[line[0]].ContainsKey(line[1]))
            //     {
            //         frequencies[line[0]][line[1]]++;
            //     }
            //     else
            //     {
            //         frequencies[line[0]].Add(line[1], 1);
            //     }
            // }

            // foreach (KeyValuePair<char, Dictionary<char, int>> kvp in frequencies)
            // {
            //     foreach (KeyValuePair<char, int> innerKvp in kvp.Value)
            //     {
            //         Console.WriteLine($"{kvp.Key} => {innerKvp.Key} => {innerKvp.Value}");
            //     }
            // }
            // return;

            // ok, so loading into a rough tree separating the first two characters as the first and second level branches isn't very balanced, but it's possibly fast enough
            // can look at optimising this later with a balanced tree for better speed, although to be fair this processes all 280,000 rows pretty quickly anyway
            // TODO: create a tree structure to hold the domains in the block list for a quicker decision on whether to block

            var exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => {
                                  eventArgs.Cancel = true;
                                  exitEvent.Set();
                              };

            NaughtyList naughtyList = NaughtyList.FromFile("dnscrypt-proxy.blacklist.txt");

            // TODO: configure secondary forwarder for greater resilience, and secondary server address if this is a multi-homed server
Console.WriteLine();
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

            Console.WriteLine("Hello World!");
            exitEvent.WaitOne();

            server.Stop();
        }
    }
}

