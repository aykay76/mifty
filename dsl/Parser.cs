using System;
using System.Collections.Generic;

namespace dsl
{
    public class Parser
    {
        protected Scanner scanner;
        protected Token token;
        protected Stack<double> runtimeStack;
        // protected SymbolTable globalTable;

        public Parser()
        {
            runtimeStack = new Stack<double>();
            // globalTable = new SymbolTable();
        }

        public virtual void Parse(string filename)
        {
            throw new NotSupportedException("Declare a subclass");
        }
        
        protected virtual void ParseStatement()
        {
            throw new NotSupportedException("Declare a subclass");
        }

        protected virtual void ParseAssignment()
        {
            throw new NotSupportedException("Declare a subclass");
        }

        protected virtual void ParseExpression()
        {
            throw new NotSupportedException("Declare a subclass");
        }

        protected virtual void ParseSimpleExpression()
        {
            throw new NotSupportedException("Declare a subclass");
        }

        protected virtual void ParseTerm()
        {
            throw new NotSupportedException("Declare a subclass");
        }

        protected virtual void ParseFactor()
        {
            throw new NotSupportedException("Declare a subclass");
        }

        protected virtual void GetToken(bool skipWhitespace = true)
        {
            token = scanner.GetToken(skipWhitespace);
        }

        // protected SymbolTableNode SearchAll(string symbol)
        // {
        //     return globalTable.Search(symbol);
        // }

        // protected SymbolTableNode EnterLocal(string symbol)
        // {
        //     return globalTable.Enter(symbol);
        // }
    }
}