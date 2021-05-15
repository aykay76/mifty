using System.Collections.Generic;
using System.IO;

namespace dsl
{
    public class Intermediate : Scanner
    {
        private int[] code;
        int index;

        public List<SymbolTable> Symbols { get; set; }

        public Intermediate(TextReader reader) : base(reader)
        {
            code = new int[4096];
            index = 0;
        }

        public void PutTokenType(int type)
        {
            code[index++] = type;
        }

        public void PutSymbolTableNode(SymbolTableNode node)
        {
            code[index++] = node.TableIndex;
            code[index++] = node.NodeIndex;
        }

        public Token Get()
        {
            int tokenCode = -1;

            do
            {
                tokenCode = code[index++];
                if (tokenCode == TokenType.LineMarker)
                {
                    // TODO: replicate the global currentLineNumber from book
                    int lineNumber = code[index++];
                }
            }
            while (tokenCode == TokenType.LineMarker);

            if (tokenCode == TokenType.Numeric)
            {
                var token = new NumberToken();
                var node = GetSymbolTableNode();
                token.Value = node.Value;
                return token;
            }
            
            if (tokenCode == TokenType.String)
            {
                var token = new StringToken();
                var node = GetSymbolTableNode();
                token.Value = node.Symbol;
                return token;
            }
            
            if (tokenCode == TokenType.Identifier)
            {
                var token = new WordToken();
                var node = GetSymbolTableNode();
                token.Word = node.Symbol;
                return token;
            }

            var defaultToken = new WordToken();
            defaultToken.Word = "";
            return defaultToken;
        }

        SymbolTableNode GetSymbolTableNode()
        {
            SymbolTableNode node = null;

            int symbolTable = code[index++];
            int nodeIndex = code[index++];

            return Symbols[symbolTable].Nodes[nodeIndex];
        }

        void InsertLineMarker()
        {
            // TODO: pick up from here
        }
    }
}