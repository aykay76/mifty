using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace mifty
{
    public class ServerConfig
    {
        public string ListenAddressV6 { get; set; }
        public string ResolverAddressV6 { get; set; }
        public List<string> ForwardersV6 { get; set; }
        public string ListenAddressV4 { get; set; }
        public string ResolverAddressV4 { get; set; }
        public List<string> ForwardersV4 { get; set; }
        public int ListenPort { get; set; }
        public int LogLevel { get; set; }

        public static ServerConfig FromFile(string filename)
        {
            string s = File.ReadAllText(filename);
            return JsonSerializer.Deserialize<ServerConfig>(s);
        }
    }
}