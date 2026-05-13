using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BalancePlugin
{
    public enum OutputAmountType
    {
        Number,
        Formula,
        Random
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

        public static (bool success, string result, string preview) Evaluate(string formula, int x)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return (false, "Formula is empty", "");

            try
            {
                double result = EvaluateFormula(formula, x);
                int rounded = (int)Math.Round(result);

                string preview = "";
                for (int i = 1; i <= 2; i++)
                {
                    double previewValue = EvaluateFormula(formula, i);
                    preview += ((int)Math.Round(previewValue)).ToString();
                    if (i < 2) preview += ", ";
                }

                return (true, rounded.ToString(), preview);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, "");
            }
        }

        public static int EvaluateSingle(string formula, int x)
        {
            try
            {
                double result = EvaluateFormula(formula, x);
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

        private static double EvaluateFormula(string formula, int x)
        {
            string processed = Preprocess(formula, x);
            return EvaluateExpression(processed);
        }

        private static string Preprocess(string formula, int x)
        {
            string result = formula.Trim().ToLower();

            result = result.Replace("^", "#pow#");

            foreach (var c in Constants)
                result = Regex.Replace(result, @"\b" + c.Key + @"\b", c.Value.ToString(), RegexOptions.IgnoreCase);

            result = Regex.Replace(result, @"(\w+)\s*\(", match =>
            {
                string func = match.Groups[1].Value.ToLower();
                if (SingleArgFunctions.ContainsKey(func) || TwoArgFunctions.ContainsKey(func))
                    return match.Value;
                return match.Value;
            });

            result = result.Replace("x", x.ToString());

            return result;
        }

        private static double EvaluateExpression(string expr)
        {
            expr = expr.Trim();
            if (string.IsNullOrEmpty(expr))
                return 0;

            if (expr.Contains("(") && expr.Contains(")"))
            {
                for (int i = expr.Length - 1; i >= 0; i--)
                {
                    if (expr[i] == ')' && TryFindMatchingParen(expr, i, out int open))
                    {
                        int funcStart = open;
                        while (funcStart > 0 && char.IsLetter(expr[funcStart - 1]))
                            funcStart--;

                        string funcCall = expr.Substring(funcStart, i - funcStart + 1);
                        double value = EvaluateFunctionCall(funcCall);
                        string before = funcStart > 0 ? expr.Substring(0, funcStart) : "";
                        string after = i + 1 < expr.Length ? expr.Substring(i + 1) : "";
                        expr = before + value.ToString() + after;
                        i = Math.Max(0, funcStart + 1);
                    }
                }
            }

            if (expr.Contains("#pow#"))
                expr = ProcessPowerOperator(expr);

            if (expr.Contains("+") || expr.Contains("-"))
            {
                expr = ProcessAddSubtract(expr);
            }

            if (expr.Contains("*") || expr.Contains("/") || expr.Contains("%"))
            {
                expr = ProcessMultiplyDivide(expr);
            }

            return double.Parse(expr.Trim());
        }

        private static bool TryFindMatchingParen(string expr, int closeIndex, out int openIndex)
        {
            int depth = 1;
            openIndex = -1;
            for (int i = closeIndex - 1; i >= 0; i--)
            {
                if (expr[i] == ')') depth++;
                else if (expr[i] == '(')
                {
                    depth--;
                    if (depth == 0)
                    {
                        openIndex = i;
                        return true;
                    }
                }
            }
            return false;
        }

        private static double EvaluateFunctionCall(string call)
        {
            int openParen = call.IndexOf('(');
            int closeParen = call.LastIndexOf(')');
            string funcName = call.Substring(0, openParen).ToLower();
            string argsStr = call.Substring(openParen + 1, closeParen - openParen - 1);

            if (TwoArgFunctions.TryGetValue(funcName, out var twoArgFunc))
            {
                var args = SplitArguments(argsStr);
                if (args.Length == 2)
                    return twoArgFunc(EvaluateExpression(args[0]), EvaluateExpression(args[1]));
            }

            if (SingleArgFunctions.TryGetValue(funcName, out var singleArgFunc))
            {
                double arg = EvaluateExpression(argsStr);
                return singleArgFunc(arg);
            }

            throw new Exception("Unknown function: " + funcName);
        }

        private static string[] SplitArguments(string args)
        {
            var result = new List<string>();
            int depth = 0;
            int lastStart = 0;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == '(') depth++;
                else if (args[i] == ')') depth--;
                else if (depth == 0 && args[i] == ',')
                {
                    result.Add(args.Substring(lastStart, i - lastStart).Trim());
                    lastStart = i + 1;
                }
            }

            result.Add(args.Substring(lastStart).Trim());
            return result.ToArray();
        }

        private static string ProcessPowerOperator(string expr)
        {
            int idx = expr.IndexOf("#pow#");
            while (idx >= 0)
            {
                int start = idx - 1;
                while (start >= 0 && (char.IsDigit(expr[start]) || expr[start] == '.' || expr[start] == '-'))
                    start--;
                start++;

                int end = idx + 6;
                while (end < expr.Length && (char.IsDigit(expr[end]) || expr[end] == '.' || expr[end] == '-'))
                    end++;

                double baseVal = EvaluateExpression(expr.Substring(start, idx - start));
                double expVal = EvaluateExpression(expr.Substring(idx + 5, end - idx - 5));
                double result = Math.Pow(baseVal, expVal);

                expr = expr.Substring(0, start) + result.ToString() + expr.Substring(end);
                idx = expr.IndexOf("#pow#", start);
            }

            return expr.Replace("#pow#", "^");
        }

        private static string ProcessAddSubtract(string expr)
        {
            int parenDepth = 0;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == ')') parenDepth++;
                else if (c == '(') parenDepth--;
                else if (parenDepth == 0 && (c == '+' || (c == '-' && i > 0 && !char.IsDigit(expr[i - 1]) && expr[i - 1] != ')')))
                {
                    string left = expr.Substring(0, i).Trim();
                    string right = expr.Substring(i + 1).Trim();

                    if (left.Length == 0) continue;

                    double leftVal = EvaluateExpression(left);
                    double rightVal = EvaluateExpression(right);

                    return (c == '+' ? leftVal + rightVal : leftVal - rightVal).ToString();
                }
            }

            return expr;
        }

        private static string ProcessMultiplyDivide(string expr)
        {
            int parenDepth = 0;

            for (int i = expr.Length - 1; i >= 0; i--)
            {
                char c = expr[i];
                if (c == ')') parenDepth++;
                else if (c == '(') parenDepth--;
                else if (parenDepth == 0 && (c == '*' || c == '/' || c == '%'))
                {
                    string left = expr.Substring(0, i).Trim();
                    string right = expr.Substring(i + 1).Trim();

                    double leftVal = double.Parse(left);
                    double rightVal = double.Parse(right);

                    return c == '*'
                        ? (leftVal * rightVal).ToString()
                        : c == '/' ? (leftVal / rightVal).ToString()
                        : (leftVal % rightVal).ToString();
                }
            }

            return expr;
        }
    }
}