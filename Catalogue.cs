using System;
using System.Collections.Generic;

namespace mifty
{
    public class Catalogue
    {
        public string Label { get; set; }
        public List<MasterFileEntry> Entries { get; set; }
        public List<Catalogue> Children { get; set; }

        // add entries to a catalogue as described in section 6 of RFC 1035 - if this gets very big a list might not cut it and
        // I might change to a hashtable (Dictionary) with the entry name being key
        public static Catalogue FromEntryList(List<MasterFileEntry> entries)
        {
            Catalogue catalogue = new Catalogue();
            foreach (MasterFileEntry entry in entries)
            {
                string[] parts = entry.Owner.Split('.', StringSplitOptions.RemoveEmptyEntries);
                Array.Reverse(parts);

                // start pointing at root, look for a match
                Catalogue pointer = catalogue;
                foreach (string s in parts)
                {
                    if (pointer.Children == null)
                    {
                        pointer.Children = new List<Catalogue>();
                    }

                    Catalogue child = pointer.FindChild(s);
                    if (child == null)
                    {
                        child = new Catalogue() { Label = s };
                        pointer.Children.Add(child);
                    }

                    pointer = child;
                }

                if (pointer.Entries == null)
                {
                    pointer.Entries = new List<MasterFileEntry>();
                }

                pointer.Entries.Add(entry);
            }

            return catalogue;
        }

        private Catalogue FindChild(string label)
        {
            if (Children == null) return null;

            foreach (Catalogue child in Children)
            {
                if (child.Label == label)
                {
                    return child;
                }
            }

            return null;
        }

        public MasterFileEntry FindEntry(Query query)
        {
            string[] parts = query.Name.Split('.');
            Array.Reverse(parts);

            Catalogue c = this;
            for (int i = 0; i < parts.Length; i++)
            {
                c = c.FindChild(parts[i]);
            }

            if (c != null)
            {
                // we found the domain in the catalogue, now find the entry based on class and type
                foreach (MasterFileEntry entry in c.Entries)
                {
                    Console.WriteLine(entry.Owner);
                }
            }

            return null;
        }
    }
}