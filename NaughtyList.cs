using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace mifty
{
    public class NaughtyList
    {
        // private string[] hosts;
        private Dictionary<char, Dictionary<char, int>> indices;

        public string Label { get; set; }
        public Dictionary<char, List<NaughtyList>> Children { get; set; }

        public NaughtyList()
        {
            indices = new Dictionary<char, Dictionary<char, int>>();
        }

        public NaughtyList AddChild(string label)
        {
            if (Children == null) Children = new Dictionary<char, List<NaughtyList>>();

            NaughtyList child = new NaughtyList();
            child.Label = label;
            if (Children.ContainsKey(label[0]))
            {
                Children[label[0]].Add(child);
            }
            else
            {
                Children.Add(label[0], new List<NaughtyList> { child } );
            }

            return child;
        }

        public NaughtyList ChildMatch(string label)
        {
            if (Children == null) return null;

            if (Children.ContainsKey(label[0]) == false) return null;

            foreach (NaughtyList child in Children[label[0]])
            {
                if (string.Compare(label, child.Label, true) == 0)
                {
                    return child;
                }
            }

            return null;
        }

        public bool Match(string host)
        {
            string[] parts = host.Split('.', System.StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(parts);

            NaughtyList pointer = null;

            for (int i = 0; i < parts.Length; i++)
            {
                bool match = false;

                foreach (NaughtyList child in pointer.Children[parts[i][0]])
                {
                    if (string.Compare(parts[i], child.Label, true) == 0)
                    {
                        pointer = child;
                        match = true;
                    }
                }

                // we didn't get a match on this part so we could be on the host name where a domain is blocked
                // in which case we need to fall back to the domain level
                if (!match)
                {
                    // there was no match at the root level, so no match
                    if (pointer == null) 
                    {
                        return false;
                    }
                    else
                    {
                        // when pointer is pointing to something
                        if (pointer.Children.Count == 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        // TODO: rework this to cater for the fact that we might not be searching against a specific host name, if we have an entire domain on the naughty list
        // we should match anything within that domain
        // I have replaced this with the more sophisticated Match method
        // public bool Contains(string host)
        // {
        //     char c1 = host[0];
        //     char c2 = host[1];

        //     if (indices.ContainsKey(c1))
        //     {
        //         if (indices[c1].ContainsKey(c2))
        //         {
        //             int c = 0;
        //             int i = indices[c1][c2];
        //             do
        //             {
        //                 c = string.Compare(host, hosts[i]);
        //                 if (c == 0) return true;
        //                 i++;
        //             }
        //             while (c > 0 && i < hosts.Length);
        //         }
        //     }

        //     return false;
        // }

        // build a tree structure similar to catalogue for the naughtylist (one could be a subset of the other actually)
        // this is INCREDIBLY slow though... maybe need a conversion process that runs periodically and creates a nicer format for loading quickly.
        public static NaughtyList FromFile(string path)
        {
            Stopwatch sw = new Stopwatch();
            Console.Write("Reading naughty list...");
            NaughtyList naughtyList = new NaughtyList();
            string[] entries = File.ReadAllLines(path);
            sw.Start();
            string tld = string.Empty;
            NaughtyList tldPtr = null;
            for (int i = 0; i < entries.Length; i++)
            {
                string[] parts = entries[i].Split('.', StringSplitOptions.RemoveEmptyEntries);
                // Array.Reverse(parts);

                NaughtyList pointer = naughtyList;
                bool first = true;
                foreach (string part in parts)
                {
                    NaughtyList child = null;

                    // I'm going to "cache" the TLD because .com is so big
                    if (first)
                    {
                        // first time we need to find the TLD pointer
                        child = pointer.ChildMatch(part);
                        tld = part;
                        tldPtr = child;
                    }
                    else
                    {
                        if (tld == part)
                        {
                            child = tldPtr;
                        }
                        else
                        {
                            child = pointer.ChildMatch(part);
                            tld = part;
                            tldPtr = child;
                        }
                    }

                    if (child == null)
                    {
                        pointer = pointer.AddChild(part);
                    }
                    else
                    {
                        pointer = child;
                    }
                }

                if (i % 1000 == 0)
                {
                    Console.Write($"\rAdded {i} of {entries.Length} entries. (1000 entries in {sw.ElapsedMilliseconds}ms.)");
                    sw.Restart();
                }
            }

            Console.WriteLine("\rNaughty list loaded.");

            return naughtyList;
        }

        // public static NaughtyList FromFile(string path)
        // {
        //     NaughtyList naughtyList = new NaughtyList();

        //     naughtyList.hosts = File.ReadAllLines(path);

        //     for (int i = 0; i < naughtyList.hosts.Length; i++)
        //     {
        //         string host = naughtyList.hosts[i];
        //         char c1 = host[0];
        //         char c2 = host[1];

        //         if (!naughtyList.indices.ContainsKey(c1))
        //         {
        //             naughtyList.indices.Add(c1, new Dictionary<char, int>());
        //         }

        //         if (!naughtyList.indices[c1].ContainsKey(c2))
        //         {
        //             naughtyList.indices[c1][c2] = i;
        //         }
        //     }

        //     return naughtyList;
        // }
    }
}