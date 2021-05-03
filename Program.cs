﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace mifty
{
    class Program
    {
        static void Main(string[] args)
        {
            Message message = Message.FromFile("badanswer.txt");

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

            // TODO: add a server config and associated command line arguments
            // for what this thing will do - addresses to bind to etc.

            // TODO: configure secondary forwarder for greater resilience, and secondary server address if this is a multi-homed server

            Server server = new Server();
            server.WithConfig(new ServerConfig {
                // ListenAddress = "172.22.160.1",
                ListenAddress = "::1",
                ResolverAddress = "2a00:23c4:33a4:101:2153:d290:8d93:b7e6",
                ListenPort = 53,
                Forwarder = "fe80::e675:dcff:fea5:3ada%4",
                LogLevel = LogLevel.Trace
            })
            .WithNaughtyList(naughtyList)
            .Start();

            Console.WriteLine("Hello World!");
            exitEvent.WaitOne();

            server.Stop();
        }
    }
}

