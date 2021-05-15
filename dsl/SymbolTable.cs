using System.Collections.Generic;

namespace dsl
{
    public class SymbolTable
    {
        public SymbolTableNode Root { get; set; }
        public int TableIndex { get; set; }
        public int NodeCount { get; set; }
        public List<SymbolTableNode> Nodes { get; set; }

        public SymbolTable()
        {
            Nodes = new List<SymbolTableNode>();
        }

        public SymbolTableNode Search(string symbol, bool xref = false, int lineNumber = 0)
        {
            SymbolTableNode node = Root;

            while (node != null)
            {
                int result = string.Compare(symbol, node.Symbol);
                if (result == 0)
                {
                    break;
                }

                node = result < 0 ? node.Left : node.Right;
            }

            if (xref && node != null)
            {
                node.LineNumbers.Add(lineNumber);
            }

            return node;
        }

        public SymbolTableNode Enter(string symbol)
        {
            if (Root == null)
            {
                Root = new SymbolTableNode(symbol);
                Root.TableIndex = TableIndex;
                Root.NodeIndex = NodeCount++;
                return Root;
            }

            SymbolTableNode node = Root;

            while (node != null)
            {
                int result = string.Compare(symbol, node.Symbol);
                if (result == 0) 
                {
                    return node;
                }
                else if (result < 0)
                {
                    if (node.Left == null)
                    {
                        node.Left = new SymbolTableNode(symbol);
                        node.Left.TableIndex = TableIndex;
                        node.Left.NodeIndex = NodeCount++;
                        return node.Left;
                    }
                    else
                    {
                        node = node.Left;
                    }
                }
                else
                {
                    if (node.Right == null)
                    {
                        node.Right = new SymbolTableNode(symbol);
                        node.Right.TableIndex = TableIndex;
                        node.Right.NodeIndex = NodeCount++;
                        return node.Right;
                    }
                    else
                    {
                        node = node.Right;
                    }
                }
            }

            return node;
        }

        public void Print()
        {
            Root.Print();
        }
    }
}