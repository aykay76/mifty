using System;
using System.Collections.Generic;

namespace mifty
{
    public class Catalogue
    {
        public string Label { get; set; }
        // TODO: restructure this to be a dictionary with name/type as a two-tuple key, and a list of answers as the value
        // this will make it easier to iterate through rrsets for updates etc.
        public List<Answer> Answers { get; set; }
        public List<Catalogue> Children { get; set; }

        // TODO: add a classification whether this is a MASTER or SLAVE zone
        // this will direct whether we answer directly or forward to another master server

        // add entries to a catalogue as described in section 6 of RFC 1035 - if this gets very big a list might not cut it and
        // I might change to a hashtable (Dictionary) with the entry name being key
        public static Catalogue FromAnswerList(List<Answer> entries)
        {
            Catalogue catalogue = new Catalogue();
            foreach (Answer answer in entries)
            {
                string[] parts = answer.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
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

                if (pointer.Answers == null)
                {
                    pointer.Answers = new List<Answer>();
                }

                pointer.Answers.Add(answer);
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

        public Catalogue FindFQDN(string fqdn)
        {
            string[] parts = fqdn.Split('.', StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(parts);

            Catalogue c = this;
            for (int i = 0; i < parts.Length; i++)
            {
                c = c.FindChild(parts[i]);
                // fail fast if no match
                if (c == null) return null;
            }

            return c;
        }

        public List<Answer>FindEntryNotType(ushort queryClass, ushort queryType, string queryName)
        {
            List<Answer> answers = new List<Answer>();
            string[] parts = queryName.Split('.', StringSplitOptions.RemoveEmptyEntries);
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
                foreach (Answer answer in c.Answers)
                {
                    if (answer.Type != queryType && answer.Class == queryClass)
                    {
                        answers.Add(answer);
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
                foreach (Answer answer in c.Answers)
                {
                    if (answer.Type == QueryType.CNAME && answer.Class == queryClass)
                    {
                        answers.Add(answer);
                    }                    
                }
            }

            return answers;
        }

        public List<Answer>FindEntry(ushort queryClass, ushort queryType, string queryName)
        {
            List<Answer> answers = new List<Answer>();
            string[] parts = queryName.Split('.', StringSplitOptions.RemoveEmptyEntries);
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
                foreach (Answer answer in c.Answers)
                {
                    if (answer.Type == queryType && answer.Class == queryClass)
                    {
                        answers.Add(answer);
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
                foreach (Answer answer in c.Answers)
                {
                    if (answer.Type == QueryType.CNAME && answer.Class == queryClass)
                    {
                        answers.Add(answer);
                    }                    
                }
            }

            return answers;
        }

        public List<Answer> FindEntry(Query query)
        {
            List<Answer> answers = new List<Answer>();
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
                foreach (Answer answer in c.Answers)
                {
                    if (answer.Type == query.Type && answer.Class == query.Class)
                    {
                        answers.Add(answer);
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
                foreach (Answer answer in c.Answers)
                {
                    if (answer.Type == QueryType.CNAME && answer.Class == query.Class)
                    {
                        answers.Add(answer);
                    }                    
                }
            }

            return answers;
        }
    }
}