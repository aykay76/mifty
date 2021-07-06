using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace mifty
{
    public class NaughtyList
    {
        private BTree<string> entries;

        public NaughtyList()
        {
            entries = new BTree<string>(20);
        }

        public bool Match(string host)
        {
            // first, reverse the hostname so that TLD is first
            string[] parts = host.Split('.', System.StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(parts);

            // now check each level of the input to see if the host, domain or TLD is blocked
            for (int i = parts.Length; i >= 1; i--)
            {
                StringBuilder sb = new StringBuilder();

                for (int p = 0; p < i; p++)
                {
                    if (p > 0) sb.Append(".");
                    sb.Append(parts[p]);
                }

                string check = sb.ToString();
                if (entries.Find(sb.ToString())) return true;
            }

            return false;
        }

        public static NaughtyList FromFile(string path)
        {
            NaughtyList naughtyList = new NaughtyList();

            string[] strings = File.ReadAllLines(path);
            BTree<string> tree = new BTree<string>(20);
            for (int i = 0; i < strings.Length; i++)
            {
                string s = strings[i];
                naughtyList.entries = naughtyList.entries.Insert(s);
            }

            return naughtyList;
        }
    }
}