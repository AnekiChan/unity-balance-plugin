using System;
using System.Collections.Generic;

namespace BalancePlugin
{
    public static class TableFormulaEvaluator
    {
        private static readonly Dictionary<string, double> Constants = new Dictionary<string, double>
        {
            { "pi", Math.PI },
            { "e", Math.E }
        };

        private static readonly Dictionary<string, Func<double, double>> SingleFuncs = new Dictionary<string, Func<double, double>>
        {
            { "sin", Math.Sin }, { "cos", Math.Cos }, { "tan", Math.Tan },
            { "asin", Math.Asin }, { "acos", Math.Acos }, { "atan", Math.Atan },
            { "sqrt", Math.Sqrt }, { "abs", Math.Abs }, { "ceil", Math.Ceiling },
            { "floor", Math.Floor }, { "round", Math.Round }, { "exp", Math.Exp },
            { "log", Math.Log }, { "log10", Math.Log10 }, { "sign", d => (double)Math.Sign(d) }
        };

        private static readonly Dictionary<string, Func<double, double, double>> DoubleFuncs = new Dictionary<string, Func<double, double, double>>
        {
            { "pow", Math.Pow }, { "max", Math.Max }, { "min", Math.Min }, { "mod", (a, b) => a % b }
        };

        public static (bool success, string result) Evaluate(string formula, TableRow row)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return (false, "empty");

            try
            {
                var visited = new HashSet<int>();
                double val = new Parser(formula, row, visited).Parse();
                return (true, Math.Round(val, 6).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private sealed class Parser
        {
            private readonly string _text;
            private readonly TableRow _row;
            private readonly HashSet<int> _visited;
            private int _pos;

            public Parser(string text, TableRow row, HashSet<int> visited = null)
            {
                _text = text ?? "";
                _row = row;
                _visited = visited ?? new HashSet<int>();
            }

            public double Parse()
            {
                double val = ParseAddSub();
                SkipSpace();
                if (!AtEnd)
                    throw new Exception("unexpected '" + Peek + "'");
                return val;
            }

            private double ParseAddSub()
            {
                double val = ParseMulDiv();
                while (true)
                {
                    SkipSpace();
                    if (TryMatch('+')) val += ParseMulDiv();
                    else if (TryMatch('-')) val -= ParseMulDiv();
                    else break;
                }
                return val;
            }

            private double ParseMulDiv()
            {
                double val = ParsePow();
                while (true)
                {
                    SkipSpace();
                    if (TryMatch('*')) val *= ParsePow();
                    else if (TryMatch('/')) val /= ParsePow();
                    else if (TryMatch('%')) val %= ParsePow();
                    else break;
                }
                return val;
            }

            private double ParsePow()
            {
                double val = ParseUnary();
                SkipSpace();
                if (TryMatch('^'))
                    return Math.Pow(val, ParsePow());
                return val;
            }

            private double ParseUnary()
            {
                SkipSpace();
                if (TryMatch('+')) return ParseUnary();
                if (TryMatch('-')) return -ParseUnary();
                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                SkipSpace();
                if (TryMatch('('))
                {
                    double val = ParseAddSub();
                    Expect(')');
                    return val;
                }

                if (AtEnd)
                    throw new Exception("unexpected end");

                if (char.IsDigit(Peek) || Peek == '.')
                    return ParseNumber();

                if (char.IsLetter(Peek))
                    return ParseIdentifier();

                throw new Exception("unexpected '" + Peek + "'");
            }

            private double ParseNumber()
            {
                int start = _pos;
                while (!AtEnd && (char.IsDigit(Peek) || Peek == '.'))
                    _pos++;
                string num = _text.Substring(start, _pos - start);
                return double.Parse(num, System.Globalization.CultureInfo.InvariantCulture);
            }

            private double ParseIdentifier()
            {
                string name = ReadIdentifier().ToLowerInvariant();

                SkipSpace();
                if (!AtEnd && Peek == '(')
                {
                    return ParseFunction(name);
                }

                return ResolveName(name);
            }

            private double ParseFunction(string name)
            {
                _pos++;
                SkipSpace();
                List<double> args = new List<double>();

                if (!TryMatch(')'))
                {
                    do
                    {
                        args.Add(ParseAddSub());
                        SkipSpace();
                    }
                    while (TryMatch(','));
                    Expect(')');
                }

                if (SingleFuncs.TryGetValue(name, out var f1))
                {
                    if (args.Count != 1)
                        throw new Exception(name + "() expects 1 arg");
                    return f1(args[0]);
                }

                if (DoubleFuncs.TryGetValue(name, out var f2))
                {
                    if (args.Count != 2)
                        throw new Exception(name + "() expects 2 args");
                    return f2(args[0], args[1]);
                }

                throw new Exception("unknown function: " + name);
            }

            private double ResolveName(string name)
            {
                if (Constants.TryGetValue(name, out double c))
                    return c;

                int colIndex = _row.ParentTable?.GetColumnIndex(name) ?? -1;
                if (colIndex >= 0)
                    return GetColumnValue(colIndex);

                throw new Exception("unknown: " + name);
            }

            private double GetColumnValue(int colIndex)
            {
                if (_row == null || _row.ParentTable == null)
                    throw new Exception("no table context");

                var colType = _row.ParentTable.GetColumnType(colIndex);
                var cell = _row.GetCell(colIndex);

                if (cell == null)
                    throw new Exception("cell missing");

                switch (colType)
                {
                    case ColumnType.Integer:
                        return cell.intValue;
                    case ColumnType.Float:
                        return cell.floatValue;
                    case ColumnType.Formula:
                        if (string.IsNullOrEmpty(cell.formulaString))
                            return 0;
                        if (!_visited.Add(colIndex))
                            throw new Exception("circular ref");
                        return new Parser(cell.formulaString, _row, _visited).Parse();
                    case ColumnType.String:
                        if (double.TryParse(cell.stringValue, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double dv))
                            return dv;
                        throw new Exception("not a number: " + cell.stringValue);
                    default:
                        throw new Exception("non-numeric column");
                }
            }

            private string ReadIdentifier()
            {
                int start = _pos;
                while (!AtEnd && (char.IsLetterOrDigit(Peek) || Peek == '_'))
                    _pos++;
                return _text.Substring(start, _pos - start);
            }

            private void Expect(char c)
            {
                SkipSpace();
                if (!TryMatch(c))
                    throw new Exception("expected '" + c + "'");
            }

            private bool TryMatch(char c)
            {
                if (Peek != c)
                    return false;
                _pos++;
                return true;
            }

            private void SkipSpace()
            {
                while (!AtEnd && char.IsWhiteSpace(Peek))
                    _pos++;
            }

            private bool AtEnd => _pos >= _text.Length;
            private char Peek => AtEnd ? '\0' : _text[_pos];
        }
    }
}
