using System.IO;

namespace dsl
{
    public class MasterFileParser : Parser
    {
        public override void Parse(string filename)
        {
            FileStream fs = File.OpenRead(filename);
            StreamReader sr = new StreamReader(fs);
            scanner = new Scanner(sr);

            // with the DSLs I've dealt with so far I would always immediately ignore whitespace.
            // but in the case of a DNS master file whitespace is significant, so needs a slightly
            // different approach before tokenising
            
            scanner.Next();
        }
    }
}