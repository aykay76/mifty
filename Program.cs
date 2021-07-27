using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Prometheus;

namespace mifty
{
    class Program
    {
        static void ConvertNaughtyList()
        {
            string[] entries = File.ReadAllLines("dnscrypt-proxy.blacklist.txt");
            string[] reversed = new string[entries.Length];
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split('.', StringSplitOptions.RemoveEmptyEntries);
                Array.Reverse(parts);

                for (int p = 0; p < parts.Length; p++)
                {
                    if (p > 0) sb.Append(".");
                    sb.Append(parts[p]);
                }

                reversed[i] = sb.ToString();
                sb.Clear();
            }

            File.WriteAllLines("naughtylist.txt", reversed);
        }

        static void Main(string[] args)
        {
            string configFile = string.Empty;

            // do some command line parsing
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--config" || args[i] == "-c")
                {
                    i++;
                    if (i == args.Length)
                    {
                        Console.WriteLine("You must specify a configuration file using '--config <filename>'");
                        return;
                    }

                    configFile = args[i];
                    if (!Path.IsPathRooted(configFile))
                    {
                        configFile = Environment.CurrentDirectory + Path.DirectorySeparatorChar + configFile;
                        if (!File.Exists(configFile))
                        {
                            Console.WriteLine($"Configuration file ({configFile}) could not be found, try again please.");
                            return;
                        }
                    }
                }
                else if (args[i] == "--help")
                {
                    
                }
            }

            // TODO: make this configurable, can't assume Prometheus on Docker - and certainly DONT hardcode the address!!
            // var metricServer = new MetricServer(hostname: "172.17.112.1", port: 1234);
            // metricServer.Start();

            Console.WriteLine("Getting ready...");
            
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

            NaughtyList naughtyList = null;
            naughtyList = NaughtyList.FromFile("naughtylist.txt");

            // Create the server with config loaded from file
            Server server = new Server();
            server.WithConfig(ServerConfig.FromFile(configFile)).WithCatalogue(catalogue).WithNaughtyList(naughtyList).Start();

            FileSystemWatcher watcher = new FileSystemWatcher(Environment.CurrentDirectory, "*.json");
            watcher.Changed += (o,e) => {
                // wait a bit in case of race condition, it doesn't matter if it takes half a second to reload configuration
                Thread.Sleep(500);
                Console.WriteLine($"Processing change to {e.FullPath}...");
                ServerConfig config = ServerConfig.FromFile(configFile);
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

