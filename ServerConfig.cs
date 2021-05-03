using System.IO;
using System.Text.Json;

namespace mifty
{
    public class ServerConfig
    {
        public string ListenAddress { get; set; }
        public string ResolverAddress { get; set; }
        public int ListenPort { get; set; }
        public string Forwarder { get; set; }
        public int LogLevel { get; set; }

        public static ServerConfig FromFile(string filename)
        {
            string s = File.ReadAllText(filename);
            return JsonSerializer.Deserialize<ServerConfig>(s);
        }
    }
}