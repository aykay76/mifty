using System;
using System.Collections.Generic;
using System.IO;

namespace mifty
{
    public class MasterFile
    {
        public string Origin { get; set; }
        public int TTL { get; set; }
        public List<MasterFileEntry> Entries { get; set; }

        public MasterFile()
        {
            Entries = new List<MasterFileEntry>();
        }

        protected static bool IsBlankLine(string line)
        {
            bool blank = false;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ' || line[i] == '\t')
                {
                    blank = true;
                }
                else if (line[i] == ';')
                {
                    break;
                }
                else
                {
                    blank = false;
                    break;
                }
            }

            return blank;
        }

        public static MasterFile FromFile(string filename)
        {
            MasterFile masterFile = new MasterFile();

            string[] lines = File.ReadAllLines(filename);
            string owner = string.Empty;
            int ttl = 3600;
            string @class = "IN"; // assume internet for now
            string[] parts = null;

            // TODO: refactor this - i'm sure it can be a lot neater using tokenisation!
            // The following entries are defined:
            //     <blank>[<comment>]
            //     $ORIGIN <domain-name> [<comment>]
            //     $INCLUDE <file-name> [<domain-name>] [<comment>]
            //     <domain-name><rr> [<comment>]
            //     <blank><rr> [<comment>]
            // <rr> contents take one of the following forms:
            //     [<TTL>] [<class>] <type> <RDATA>
            //     [<class>] [<TTL>] <type> <RDATA>

            // predominantly line based so should be one entry per line but beware the parentheses!
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (!IsBlankLine(line))
                {
                    // only process lines with something in them

                }

                int semi = line.IndexOf(';');
                if (semi != -1)
                {
                    line = line.Substring(0, semi).Trim();
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
                            line += nextLine.Substring(0, semi);
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
                else if (line.StartsWith("$INCLUDE"))
                {
                    // TODO: include the file and could be an option new origin
                }
                else
                {
                    MasterFileEntry entry = new MasterFileEntry();

                    // this often isn't specified so setting here to the default
                    entry.TTL = ttl;

                    parts = line.Split(new char[] { ' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);

                    if (line[0] == ' ' || line[0] == '\t')
                    {
                        entry.Owner = owner;
                    }
                    else
                    {
                        // suck out the owner then the rest will be <rr>
                        entry.Owner = parts[0];
                        string[] newParts = new string[parts.Length - 1];
                        Array.Copy(parts, 1, newParts, 0, newParts.Length);
                        parts = newParts;
                    }

                    // TODO: either parse fully or process SOA records differently.
                    //       I think I need a parser to handle special characters but it's Friday evening
                    //       this might wait until tomorrow.

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

                    masterFile.Entries.Add(entry);
                }
            }

            return masterFile;
        }
    }
}