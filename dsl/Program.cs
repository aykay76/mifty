using System;
using System.Diagnostics;
using System.IO;

namespace dsl
{
    class Program
    {
        static void Main(string[] args)
        {
            // ScannerTest.Run();
            //SymbolTableTest.Run();

            ParserCalc p = new ParserCalc();
            p.Parse("test.calc");
        }
    }
}
