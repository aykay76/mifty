using System;
using System.Collections.Generic;

namespace mifty
{
    public class Catalogue
    {
        public string Label { get; set; }
        // TODO: change this to be list of Answer's
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

        // TODO: need to return multiple entries for NS and MX, for example!
        public MasterFileEntry FindEntry(Query query)
        {
            string[] parts = query.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(parts);

            Catalogue c = this;
            for (int i = 0; i < parts.Length; i++)
            {
                c = c.FindChild(parts[i]);
                // fail fast if no match
                if (c == null) return null;
            }

            if (c != null)
            {
                // we found the domain in the catalogue, now find the entry based on class and type
                foreach (MasterFileEntry entry in c.Entries)
                {
                    if (entry.Type == query.Type && entry.Class == query.Class)
                    {
                        return entry;
                    }                    
                }

                // If we get this far without a match we can check for CNAME
                // see section 3.6.2 of RFC 1034:
                // CNAME RRs cause special action in DNS software.  When a name server
                // fails to find a desired RR in the resource set associated with the
                // domain name, it checks to see if the resource set consists of a CNAME
                // record with a matching class.  If so, the name server includes the CNAME
                // record in the response and restarts the query at the domain name
                // specified in the data field of the CNAME record.  The one exception to
                // this rule is that queries which match the CNAME type are not restarted.
                foreach (MasterFileEntry entry in c.Entries)
                {
                    if (entry.Type == QueryType.CNAME && entry.Class == query.Class)
                    {
                        // TODO: instead of returning, I should take the name, change the type to A (AAAA?) and re-search (research?)
                        return entry;
                    }                    
                }
            }

            return null;
        }
    }
}