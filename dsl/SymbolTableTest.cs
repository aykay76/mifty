using System;

namespace dsl
{
    public class SymbolTableTest
    {
        public static void Run()
        {
            SymbolTable table = new SymbolTable();

            table.Enter("giraffe");
            table.Enter("elephant");
            table.Enter("albatross");
            table.Enter("zebra");
            table.Enter("hippopotamus");
            table.Enter("gazelle");
            table.Enter("porcupine");
            table.Enter("rhinocerous");
            
            var node = table.Search("hippopotamus");
            if (node == null)
            {
                Console.WriteLine("hippopotamus not found");
            }
            else
            {
                Console.WriteLine(node.Symbol);
            }

            node = table.Search("hyena");
            if (node == null)
            {
                Console.WriteLine("hyena not found");
            }
            else
            {
                Console.WriteLine(node.Symbol);
            }

            table.Print();
        }
    }
}