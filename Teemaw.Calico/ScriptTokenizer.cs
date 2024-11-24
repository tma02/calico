﻿using System.Text;
using GDWeave.Godot;
using GDWeave.Godot.Variants;

namespace Teemaw.Calico;

/**
 * This is copied from https://github.com/NotNite/GDWeave/blob/main/GDWeave/Script/ScriptTokenizer.cs since this class
 * was not made visible. Minor modifications have been made to make it more ergonomic to tokenize snippets.
 */
public static class ScriptTokenizer {
    private static readonly Dictionary<string, TokenType> Tokens = new() {
        {"continue", TokenType.CfContinue},
        {"return", TokenType.CfReturn},
        {"break", TokenType.CfBreak},
        {"match", TokenType.CfMatch},
        {"while", TokenType.CfWhile},
        {"elif", TokenType.CfElif},
        {"else", TokenType.CfElse},
        {"pass", TokenType.CfPass},
        {"for", TokenType.CfFor},
        {"if", TokenType.CfIf},
        {"const", TokenType.PrConst},
        {"var", TokenType.PrVar},
        {"func", TokenType.PrFunction},
        {"class", TokenType.PrClass},
        {"extends", TokenType.PrExtends},
        {"is", TokenType.PrIs},
        {"as", TokenType.PrAs},
        {"@onready", TokenType.PrOnready},
        {"@tool", TokenType.PrTool},
        {"@export", TokenType.PrExport},

        {"setget", TokenType.PrSetget},
        {"static", TokenType.PrStatic},

        {"void", TokenType.PrVoid},
        {"enum", TokenType.PrEnum},
        {"preload", TokenType.PrPreload},
        {"assert", TokenType.PrAssert},

        {"signal", TokenType.PrSignal},
        {"breakpoint", TokenType.PrBreakpoint},

        {"sync", TokenType.PrSync},
        {"remote", TokenType.PrRemote},
        {"master", TokenType.PrMaster},
        {"slave", TokenType.PrSlave},
        {"puppet", TokenType.PrPuppet},

        {"remotesync", TokenType.PrRemotesync},
        {"mastersync", TokenType.PrMastersync},
        {"puppetsync", TokenType.PrPuppetsync},

        {"\n", TokenType.Newline},

        {"PI", TokenType.ConstPi},
        {"TAU", TokenType.ConstTau},
        {"INF", TokenType.ConstInf},
        {"NAN", TokenType.ConstNan},

        {"error", TokenType.Error},
        {"cursor", TokenType.Cursor},

        {"self", TokenType.Self},

        {"in", TokenType.OpIn},

        {"_", TokenType.Wildcard},

        {"[", TokenType.BracketOpen},
        {"]", TokenType.BracketClose},
        {"{", TokenType.CurlyBracketOpen},
        {"}", TokenType.CurlyBracketClose},

        {"(", TokenType.ParenthesisOpen},
        {")", TokenType.ParenthesisClose},

        {",", TokenType.Comma},
        {";", TokenType.Semicolon},
        {".", TokenType.Period},
        {"?", TokenType.QuestionMark},
        {":", TokenType.Colon},
        {"$", TokenType.Dollar},
        {"->", TokenType.ForwardArrow},

        {">>=", TokenType.OpAssignShiftRight},
        {"<<=", TokenType.OpAssignShiftLeft},

        {">>", TokenType.OpShiftRight},
        {"<<", TokenType.OpShiftLeft},

        {"==", TokenType.OpEqual},
        {"!=", TokenType.OpNotEqual},
        {"&&", TokenType.OpAnd},
        {"||", TokenType.OpOr},
        {"!", TokenType.OpNot},

        {"+=", TokenType.OpAssignAdd},
        {"-=", TokenType.OpAssignSub},
        {"*=", TokenType.OpAssignMul},
        {"/=", TokenType.OpAssignDiv},
        {"%=", TokenType.OpAssignMod},
        {"&=", TokenType.OpAssignBitAnd},
        {"|=", TokenType.OpAssignBitOr},
        {"^=", TokenType.OpAssignBitXor},

        {"+", TokenType.OpAdd},
        {"-", TokenType.OpSub},
        {"*", TokenType.OpMul},
        {"/", TokenType.OpDiv},
        {"%", TokenType.OpMod},

        {"~", TokenType.OpBitInvert},
        {"&", TokenType.OpBitAnd},
        {"|", TokenType.OpBitOr},
        {"^", TokenType.OpBitXor},

        {"<=", TokenType.OpLessEqual},
        {">=", TokenType.OpGreaterEqual},
        {"<", TokenType.OpLess},
        {">", TokenType.OpGreater},

        {"=", TokenType.OpAssign},
    };

    private static readonly HashSet<string> Symbols = new() {
        "->",

        ">>=",
        "<<=",

        ">>",
        "<<",

        "==",
        "!=",
        "&&",
        "||",
        "!",

        "+=",
        "-=",
        "*=",
        "/=",
        "%=",
       "&=",
        "|=",
        "^=",

        "_",

        "[",
        "]",

        "{",
        "}",

        "(",
        ")",

        ",",
        ";",
        ".",
        "?",
        ":",
        "$",
        "+",
        "-",
        "*",
        "/",
        "%",

        "~",
        "&",
        "|",
        "^",

        "<=",
        ">=",
        "<",
        ">",

        "=",
    };

    private static readonly List<string> BuiltinFunctions = Enum.GetNames<BuiltinFunction>().ToList();

    private static void InsertNewLine(IEnumerator<string> enumerator, uint baseIndent, List<Token> toFlush) {
        if (!enumerator.MoveNext()) {
            return;
        }

        var tabCount = uint.Parse(enumerator.Current);
        if (tabCount + baseIndent > 0)
        {
            toFlush.Add(new Token(TokenType.Newline, tabCount + baseIndent));
        }
        else
        {
            toFlush.Add(new Token(TokenType.Newline));
        }
    }

    private static void BuildIdentifierName(IEnumerator<string> enumerator, List<Token> toFlush, out string? found) {
        found = string.Empty;
        if (!enumerator.MoveNext()) {
            return;
        }

        if (enumerator.Current == ":") {
            toFlush.Add(new Token(TokenType.Wildcard));
            toFlush.Add(new Token(TokenType.Semicolon));
            return;
        }

        found = "_" + enumerator.Current;
    }

    private static void BuildNumber(IEnumerator<string> enumerator, List<Token> toFlush, out bool foundFull) {
        foundFull = true;
        int sign = 1;

        if (enumerator.Current == "-") {
            sign = -1;
            if (!enumerator.MoveNext()) return;
        }

        if (!long.TryParse(enumerator.Current, out long upper)) {
            toFlush.Add(new Token(TokenType.OpSub));
            foundFull = false;
            return;
        }

        if (!enumerator.MoveNext()) return;

        if (enumerator.Current != ".") {
            toFlush.Add(new ConstantToken(new IntVariant(upper * sign)));
            foundFull = false;
            return;
        }

        if (!enumerator.MoveNext()) return;

        if (!long.TryParse(enumerator.Current, out long lower)) {
            // I dont think there is really a proper return for here.
            // You'd have a number that looks like this "1000."
            // No following decimal
            // Comment if you had ideas
            return;
        }

        var result = upper + (lower / Math.Pow(10, lower.ToString().Length));
        toFlush.Add(new ConstantToken(new RealVariant(result * sign)));
    }

    public static IEnumerable<Token> Tokenize(string gdScript, uint baseIndent = 0) {
        var finalTokens = new List<Token>();
        var tokens = SanitizeInput(TokenizeString(gdScript + " "));

        var previous = string.Empty;
        var idName = string.Empty;

        var toFlush = new List<Token>(2);
        //finalTokens.Add(new Token(TokenType.Newline, baseIndent));
        var enumerator = tokens.GetEnumerator();
        var reparse = false;
        while (reparse ? true : enumerator.MoveNext()) {
            reparse = false;

            if (enumerator.Current == "\n") {
                InsertNewLine(enumerator, baseIndent, toFlush);
                endAndFlushId();
                continue;
            }

            if (enumerator.Current == "_") {
                BuildIdentifierName(enumerator, toFlush, out string? found);
                if (found == string.Empty) {
                    endAndFlushId();
                    continue;
                }

                idName += found;

                end();
                continue;
            }

            if (enumerator.Current == "-" || char.IsDigit(enumerator.Current[0])) {
                BuildNumber(enumerator, toFlush, out bool foundFull);
                reparse = !foundFull;
                endAndFlushId();
                continue;
            }

            if (BuiltinFunctions.Contains(enumerator.Current)) {
                toFlush.Add(new Token(TokenType.BuiltInFunc, (uint?) BuiltinFunctions.IndexOf(enumerator.Current)));
                endAndFlushId();
                continue;
            }

            if (Tokens.TryGetValue(enumerator.Current, out var type)) {
                toFlush.Add(new Token(type));
                endAndFlushId();
                continue;
            }

            if (enumerator.Current.StartsWith('"')) {
                var current = enumerator.Current;
                toFlush.Add(new ConstantToken(new StringVariant(current.Substring(1, current.Length - 2))));
                endAndFlushId();
                continue;
            }

            if (bool.TryParse(enumerator.Current, out var resultB)) {
                toFlush.Add(new ConstantToken(new BoolVariant(resultB)));
                endAndFlushId();
                continue;
            }

            idName += enumerator.Current;

            end();

            void end() {
                previous = enumerator.Current;
                finalTokens.AddRange(toFlush);
                toFlush.Clear();
            }

            void endAndFlushId() {
                if (idName != string.Empty) {
                    if (idName.Trim() == "return")
                    {
                        // Not sure why we sometimes hit this here. I think it has to do with return being the final
                        // token of a line.
                        finalTokens.Add(new Token(TokenType.CfReturn));
                    }
                    else if (idName.Trim() == "self")
                    {
                        // Not sure why we sometimes hit this here. I think it has to do with self being the final
                        // token of a line.
                        finalTokens.Add(new Token(TokenType.Self));
                    }
                    else if (idName == "print")
                    {
                        // Without this, `print` is tokenized as an identifier.
                        finalTokens.Add(new Token(TokenType.BuiltInFunc, (uint?) BuiltinFunction.TextPrint));
                    }
                    else if (idName == "Color")
                    {
                        // Without this, `print` is tokenized as an identifier.
                        finalTokens.Add(new Token(TokenType.BuiltInType, 14));
                    }
                    else if (idName == "Vector3")
                    {
                        // Without this, `print` is tokenized as an identifier.
                        finalTokens.Add(new Token(TokenType.BuiltInType, 7));
                    }
                    else
                    {
                        finalTokens.Add(new IdentifierToken(idName));
                    }
                    idName = string.Empty;
                }

                end();
            }
        }

        //finalTokens.Add(new(TokenType.Newline, baseIndent));

        foreach (var t in finalTokens) yield return t;
    }

    private static IEnumerable<string> SanitizeInput(IEnumerable<string> tokens) {
        foreach (var token in tokens) {
            if (token != "\n" && string.IsNullOrWhiteSpace(token)) {
                continue;
            }

            yield return token;
        }
    }

    private static IEnumerable<string> TokenizeString(string text) {
        StringBuilder builder = new(20);
        for (var i = 0; i < text.Length; i++) {
            switch (text[i]) {
                case '"': {
                    yield return ClearBuilder();
                    builder.Append('"');
                    i++;
                    for (; i < text.Length; i++) {
                        builder.Append(text[i]);
                        if (text[i] == '"') {
                            break;
                        }
                    }

                    yield return ClearBuilder();
                    continue;
                }

                // This is stupid and awful
                case '\n': {
                    yield return ClearBuilder();
                    var start = i;
                    i++;
                    for (; i < text.Length && text[i] == '\t'; i++) ;
                    i--;
                    yield return "\n";
                    yield return $"{i - start}";
                    continue;
                }
            }

            var matched = false;
            foreach (var delimiter in Symbols) {
                if (Match(text, i, delimiter)) {
                    yield return ClearBuilder();
                    yield return delimiter;
                    i += delimiter.Length - 1;
                    matched = true;
                    break;
                }
            }

            if (matched) continue;

            if (text[i] == ' ') {
                yield return ClearBuilder();
                continue;
            }

            builder.Append(text[i]);
        }

        yield return "\n";

        string ClearBuilder() {
            var built = builder.ToString();
            builder.Clear();
            return built;
        }
    }

    private static bool Match(string text, int index, string match) {
        return string.Compare(text, index, match, 0, match.Length) == 0;
    }
}