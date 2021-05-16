using System;
using System.Text;

namespace dsl
{
    public class NumberToken : Token
    {
        public string String { get; set; }
        public double Value { get { return _value; } set { _value = value; } }

        private double _value;
        bool real;
        int digitCount;
        int wholePlaces;
        // int decimalPlaces;
        // char exponentSign = '+';
        // double eValue;
        StringBuilder stringBuilder;

        public NumberToken()
        {
            Value = 0.0;
            real = false;
            Type = TokenType.Numeric;
        }

        public static NumberToken GetToken(Scanner scanner)
        {
            NumberToken t = new NumberToken();
            t.stringBuilder = new StringBuilder();
            t.sc = scanner.col;
            t.sr = scanner.row;

            t.AccumulateValue(scanner, ref t._value);
            t.wholePlaces = t.digitCount;

            // no dealing with real values, just integers
            // if (scanner.curr == '.')
            // {
            //     t.stringBuilder.Append(scanner.curr);
            //     scanner.Next();
            //     t.real = true;
            //     t.AccumulateValue(scanner, ref t._value);
            //     t.decimalPlaces = t.digitCount - t.wholePlaces;
            // }

            // if (scanner.curr == 'e' || scanner.curr == 'E')
            // {
            //     t.real = true;
            //     t.stringBuilder.Append(scanner.curr);
            //     scanner.Next();

            //     if (scanner.curr == '+' || scanner.curr == '-')
            //     {
            //         t.exponentSign = scanner.curr;
            //         t.stringBuilder.Append(scanner.curr);
            //         scanner.Next();
            //     }

            //     t.digitCount = 0;
            //     t.AccumulateValue(scanner, ref t.eValue);

            //     if (t.exponentSign == '-')
            //     {
            //         t.eValue = -t.eValue;
            //     }
            // }

            t.ec = scanner.col;
            t.er = scanner.row;

            // var exponent = t.eValue - t.decimalPlaces;
            // if (exponent != 0)
            // {
            //     t.Value *= Math.Pow(10, exponent);
            // }

            t.String = t.stringBuilder.ToString();

            return t;
        }

        private void AccumulateValue(Scanner scanner, ref double value)
        {
            if (scanner.currType != CharType.Numeric)
            {
                throw new FormatException();;
            }

            do {
                stringBuilder.Append(scanner.curr);
                value = 10 * value + scanner.curr - '0';
                digitCount++;
                scanner.Next();
            } while (scanner.currType == CharType.Numeric);
        }

        public override string ToString()
        {
            if (real)
            {
                return $"NumberToken: (real){Value}";
            }

            return $"NumberToken: (int){(int)Value}";
        }
    }
}