using System.Collections.Generic;
using System.IO;

namespace mifty
{
    public class NaughtyList
    {
        private string[] hosts;
        private Dictionary<char, Dictionary<char, int>> indices;

        public NaughtyList()
        {
            indices = new Dictionary<char, Dictionary<char, int>>();
        }

        // TODO: rework this to cater for the fact that we might not be searching against a specific host name, if we have an entire domain on the naughty list
        // we should match anything within that domain
        public bool Contains(string host)
        {
            char c1 = host[0];
            char c2 = host[1];

            if (indices.ContainsKey(c1))
            {
                if (indices[c1].ContainsKey(c2))
                {
                    int c = 0;
                    int i = indices[c1][c2];
                    do
                    {
                        c = string.Compare(host, hosts[i]);
                        if (c == 0) return true;
                        i++;
                    }
                    while (c > 0 && i < hosts.Length);
                }
            }

            return false;
        }

        public static NaughtyList FromFile(string path)
        {
            NaughtyList naughtyList = new NaughtyList();

            naughtyList.hosts = File.ReadAllLines(path);

            for (int i = 0; i < naughtyList.hosts.Length; i++)
            {
                string host = naughtyList.hosts[i];
                char c1 = host[0];
                char c2 = host[1];

                if (!naughtyList.indices.ContainsKey(c1))
                {
                    naughtyList.indices.Add(c1, new Dictionary<char, int>());
                }

                if (!naughtyList.indices[c1].ContainsKey(c2))
                {
                    naughtyList.indices[c1][c2] = i;
                }
            }

            return naughtyList;
        }
    }
}