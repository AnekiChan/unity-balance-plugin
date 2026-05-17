using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BalancePlugin
{
    public enum OutputAmountType
    {
        Number,
        Formula,
        Random,
        RandomRange,
        All
    }

    public static class FormulaEvaluator
    {
        private static readonly Dictionary<string, Func<double, double>> SingleArgFunctions = new Dictionary<string, Func<double, double>>
        {
            { "sin", d => Math.Sin(d) },
            { "cos", d => Math.Cos(d) },
            { "tan", d => Math.Tan(d) },
            { "asin", d => Math.Asin(d) },
            { "acos", d => Math.Acos(d) },
            { "atan", d => Math.Atan(d) },
            { "sinh", d => Math.Sinh(d) },
            { "cosh", d => Math.Cosh(d) },
            { "tanh", d => Math.Tanh(d) },
            { "sqrt", d => Math.Sqrt(d) },
            { "abs", d => Math.Abs(d) },
            { "ceil", d => Math.Ceiling(d) },
            { "floor", d => Math.Floor(d) },
            { "trunc", d => Math.Truncate(d) },
            { "round", d => Math.Round(d) },
            { "exp", d => Math.Exp(d) },
            { "log", d => Math.Log(d) },
            { "log10", d => Math.Log10(d) },
            { "log2", d => Math.Log(d, 2) },
            { "sign", d => (double)Math.Sign(d) },
            { "factorial", Factorial }
        };

        private static readonly Dictionary<string, Func<double, double, double>> TwoArgFunctions = new Dictionary<string, Func<double, double, double>>
        {
            { "pow", (a, b) => Math.Pow(a, b) },
            { "max", (a, b) => Math.Max(a, b) },
            { "min", (a, b) => Math.Min(a, b) },
            { "atan2", (a, b) => Math.Atan2(a, b) },
            { "mod", (a, b) => a % b }
        };

        private static readonly Dictionary<string, double> Constants = new Dictionary<string, double>
        {
            { "pi", Math.PI },
            { "e", Math.E }
        };

        public static (bool success, string result, string preview) Evaluate(string formula, int x, int s = 0)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return (false, "Formula is empty", "");

            try
            {
                double result = EvaluateFormula(formula, x, s);
                int rounded = (int)Math.Round(result);

                string preview = "";
                for (int i = 1; i <= 2; i++)
                {
                    double previewValue = EvaluateFormula(formula, i, s);
                    preview += ((int)Math.Round(previewValue)).ToString(CultureInfo.InvariantCulture);
                    if (i < 2) preview += ", ";
                }

                return (true, rounded.ToString(CultureInfo.InvariantCulture), preview);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, "");
            }
        }

        public static int EvaluateSingle(string formula, int x, int s = 0)
        {
            try
            {
                double result = EvaluateFormula(formula, x, s);
                return (int)Math.Round(result);
            }
            catch
            {
                return 0;
            }
        }

        private static double Factorial(double n)
        {
            int i = (int)n;
            if (i < 0) return 0;
            if (i == 0 || i == 1) return 1;
            double result = 1;
            for (int j = 2; j <= i; j++)
                result *= j;
            return result;
        }

        private static double EvaluateFormula(string formula, int x, int s = 0)
        {
            return new Parser(formula, x, s).Parse();
        }

        private static double EvaluateExpression(string expr)
        {
            return new Parser(expr, 0, 0).Parse();
        }

        private sealed class Parser
        {
            private readonly string _text;
            private readonly int _x;
            private readonly int _s;
            private int _position;

            public Parser(string text, int x, int s)
            {
                _text = text ?? "";
                _x = x;
                _s = s;
            }

            public double Parse()
            {
                double value = ParseAddSubtract();
                SkipWhitespace();

                if (!IsAtEnd)
                    throw new Exception("Unexpected token: " + Current);

                return value;
            }

            private double ParseAddSubtract()
            {
                double value = ParseMultiplyDivide();

                while (true)
                {
                    SkipWhitespace();

                    if (Match('+'))
                        value += ParseMultiplyDivide();
                    else if (Match('-'))
                        value -= ParseMultiplyDivide();
                    else
                        return value;
                }
            }

            private double ParseMultiplyDivide()
            {
                double value = ParsePower();

                while (true)
                {
                    SkipWhitespace();

                    if (Match('*'))
                        value *= ParsePower();
                    else if (Match('/'))
                        value /= ParsePower();
                    else if (Match('%'))
                        value %= ParsePower();
                    else
                        return value;
                }
            }

            private double ParsePower()
            {
                double value = ParseUnary();
                SkipWhitespace();

                if (Match('^'))
                    value = Math.Pow(value, ParsePower());

                return value;
            }

            private double ParseUnary()
            {
                SkipWhitespace();

                if (Match('+'))
                    return ParseUnary();

                if (Match('-'))
                    return -ParseUnary();

                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                SkipWhitespace();

                if (Match('('))
                {
                    double value = ParseAddSubtract();
                    Expect(')');
                    return value;
                }

                if (IsAtEnd)
                    throw new Exception("Unexpected end of formula");

                if (char.IsDigit(Current) || Current == '.' || Current == ',')
                    return ParseNumber();

                if (char.IsLetter(Current))
                    return ParseIdentifier();

                throw new Exception("Unexpected token: " + Current);
            }

            private double ParseNumber()
            {
                int start = _position;
                bool hasDecimalSeparator = false;

                while (!IsAtEnd)
                {
                    char c = Current;
                    if (char.IsDigit(c))
                    {
                        _position++;
                        continue;
                    }

                    if ((c == '.' || c == ',') && !hasDecimalSeparator && HasDigitAfter(_position))
                    {
                        hasDecimalSeparator = true;
                        _position++;
                        continue;
                    }

                    break;
                }

                string number = _text.Substring(start, _position - start).Replace(',', '.');
                return double.Parse(number, CultureInfo.InvariantCulture);
            }

            private double ParseIdentifier()
            {
                string name = ParseIdentifierName().ToLowerInvariant();

                if (name == "x")
                    return _x;

                if (name == "s")
                    return _s;

                if (Constants.TryGetValue(name, out double constant))
                    return constant;

                SkipWhitespace();
                if (!Match('('))
                    throw new Exception("Unknown variable: " + name);

                List<double> args = new List<double>();
                SkipWhitespace();

                if (!Match(')'))
                {
                    do
                    {
                        args.Add(ParseAddSubtract());
                        SkipWhitespace();
                    }
                    while (Match(';') || MatchArgumentComma());

                    Expect(')');
                }

                return EvaluateFunction(name, args);
            }

            private double EvaluateFunction(string name, List<double> args)
            {
                if (SingleArgFunctions.TryGetValue(name, out var singleArgFunc))
                {
                    if (args.Count != 1)
                        throw new Exception("Function " + name + " expects 1 argument");

                    return singleArgFunc(args[0]);
                }

                if (TwoArgFunctions.TryGetValue(name, out var twoArgFunc))
                {
                    if (args.Count != 2)
                        throw new Exception("Function " + name + " expects 2 arguments");

                    return twoArgFunc(args[0], args[1]);
                }

                throw new Exception("Unknown function: " + name);
            }

            private string ParseIdentifierName()
            {
                int start = _position;

                while (!IsAtEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
                    _position++;

                return _text.Substring(start, _position - start);
            }

            private bool MatchArgumentComma()
            {
                SkipWhitespace();

                if (Current != ',')
                    return false;

                _position++;
                return true;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();

                if (!Match(expected))
                    throw new Exception("Expected '" + expected + "'");
            }

            private bool Match(char expected)
            {
                if (Current != expected)
                    return false;

                _position++;
                return true;
            }

            private void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(Current))
                    _position++;
            }

            private bool HasDigitAfter(int index)
            {
                return index + 1 < _text.Length && char.IsDigit(_text[index + 1]);
            }

            private bool IsAtEnd => _position >= _text.Length;
            private char Current => IsAtEnd ? '\0' : _text[_position];
        }
    }
}
