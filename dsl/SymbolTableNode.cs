using System;
using System.Collections.Generic;

namespace dsl
{
    public class SymbolTableNode
    {
        public SymbolTableNode Left { get; set; }
        public SymbolTableNode Right { get; set; }
        public string Symbol { get; set; }
        public int TableIndex { get; set; }
        public int NodeIndex { get; set; }
        public List<int> LineNumbers { get; set; }
        
        public double Value { get; set; }

        public SymbolTableNode(string symbol)
        {
            Symbol = symbol;
            LineNumbers = new List<int>();
        }

        public void Print()
        {
            if (Left != null)
            {
                Left.Print();
            }

            Console.WriteLine(Symbol);

            if (Right != null)
            {
                Right.Print();
            }
        }
    }
}