using System.Collections.Generic;
using System.IO;

namespace mifty
{
    public class MasterFile
    {
        public string Origin { get; set; }
        public int TTL { get; set; }
        public Dictionary<string, MasterFileEntry> Entries { get; set; }

        public MasterFile()
        {
            Entries = new Dictionary<string, MasterFileEntry>();
        }

        public static MasterFile FromFile(string filename)
        {
            MasterFile masterFile = new MasterFile();

            string[] lines = File.ReadAllLines(filename);
            string owner = string.Empty;
            int ttl = 3600;
            string @class = "IN"; // assume internet for now

            // TODO: refactor this to handle includes and full parsing of escaped text etc.
            // see section 5 of RFC1035

            // predominantly line based so should be one entry per line but beware the parentheses!
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int semi = line.IndexOf(';');
                if (semi != -1)
                {
                    line = line.Substring(0, semi);
                }

                int bracket = line.IndexOf('(');
                if (bracket != -1)
                {
                    bool foundClose = false;

                    while (++i < lines.Length && !foundClose)
                    {
                        // process the next line
                        string nextLine = lines[i];
                        semi = nextLine.IndexOf(';');
                        if (semi != -1)
                        {
                            line += nextLine.Substring(0, semi);
                        }

                        if (nextLine.Contains(')'))
                        {
                            foundClose = true;
                        }
                    }
                }

                // line now contains either a single line entry or a concatenated bracketed entry
                if (line.StartsWith("$ORIGIN"))
                {
                    masterFile.Origin = line.Substring(7).Trim();
                    owner = masterFile.Origin;
                }
                else if (line.StartsWith("$TTL"))
                {
                    masterFile.TTL = int.Parse(line.Substring(4).Trim());
                    ttl = masterFile.TTL;
                }
                else
                {
                    MasterFileEntry entry = new MasterFileEntry();

                    if (line[0] == ' ' || line[0] == '\t')
                    {
                        entry.Owner = owner;

                        string[] parts = line.Split(new char[] { ' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            // we have no TTL or class, just type and data
                            entry.Type = parts[0];
                            entry.Data = parts[1];
                        }                      
                        else if (parts.Length == 3)
                        {
                            // we have one optional TTL or class, plus type and data
                            if (char.IsDigit(parts[0][0]))
                            {
                                // we have TTL
                                ttl = int.Parse(parts[0]);
                                entry.TTL = ttl;
                            }
                            else
                            {
                                // we have class
                                @class = parts[0];
                                entry.Class = @class;
                            }

                            entry.Type = parts[1];
                            entry.Data = parts[2];
                        }
                        else if (parts.Length == 4)
                        {
                            // we have optional TTL, optional class, plus type and data
                            if (char.IsDigit(parts[0][0]))
                            {
                                ttl = int.Parse(parts[0]);
                                entry.TTL = ttl;
                            }
                            else
                            {
                                @class = parts[0];
                                entry.Class = @class;
                            }

                            if (char.IsDigit(parts[1][0]))
                            {
                                ttl = int.Parse(parts[1]);
                                entry.TTL = ttl;
                            }
                            else
                            {
                                @class = parts[1];
                                entry.Class = @class;
                            }

                            entry.Type = parts[2];
                            entry.Data = parts[3];
                        }
                    }
                }
            }

            return masterFile;
        }
    }
}