namespace mifty
{
    public class ServerConfig
    {
        public string ServerAddress { get; set; }
        public string ResolverAddress { get; set; }
        public int ServerPort { get; set; }
        public string Forwarder { get; set; }
        public int LogLevel { get; set; }
    }
}