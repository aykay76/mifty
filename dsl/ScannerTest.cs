using System;
using System.Diagnostics;
using System.IO;

namespace dsl
{
    public class ScannerTest
    {
        public static void Run()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            FileStream fs = File.OpenRead("test.dsl");
            StreamReader sr = new StreamReader(fs);
            Scanner s = new Scanner(sr);
            
            Token t = s.GetToken();
            Console.WriteLine($"{t.sr}, {t.sc} - {t.er}, {t.ec} : {t.ToString()}");
            while (!(s.currType == CharType.EOF))
            {
                t = s.GetToken();
                Console.WriteLine($"{t.sr}, {t.sc} - {t.er}, {t.ec} : {t.ToString()}");
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds.ToString());

        }
    }
}