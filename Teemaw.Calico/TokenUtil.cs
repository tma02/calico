using GDWeave.Godot;
using GDWeave.Godot.Variants;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public static class TokenUtil
{
    public static IEnumerable<Token> ReplaceAssignmentsAsDeferred(IEnumerable<Token> tokens,
        HashSet<string>? ignoredIdentifiers = null)
    {
        Token? lastToken = null;
        var inAssignmentStatement = false;
        var skipLine = false;
        var line = 0;
        foreach (var t in tokens)
        {
            switch (t)
            {
                case { Type: Newline }:
                    line += 1;
                    skipLine = false;
                    break;
                case { Type: PrVar }:
                    skipLine = true;
                    break;
                default:
                {
                    if (lastToken is IdentifierToken identifier &&
                        (ignoredIdentifiers == null || !ignoredIdentifiers.Contains(identifier.Name)) &&
                        t is { Type: OpAssign } && !skipLine)
                    {
                        inAssignmentStatement = true;
                        yield return new IdentifierToken("set_deferred");
                        yield return new Token(ParenthesisOpen);
                        yield return new ConstantToken(new StringVariant(identifier.Name));
                        yield return new Token(Comma);
                        // Don't return the last token or current token
                        lastToken = null;
                        continue;
                    }

                    break;
                }
            }

            if (t.Type == Newline && inAssignmentStatement)
            {
                inAssignmentStatement = false;
                yield return lastToken;
                // Close out the set_deferred call...
                yield return new Token(ParenthesisClose);
                // ...before the Newline token.
                yield return t;
            }
            else if (lastToken != null)
                yield return lastToken;
            // TODO: this is for debugging 
            // if (t is { Type: Newline } && line < 79)
            // {
            //     yield return t;
            //     yield return new Token(BuiltInFunc, (uint?)BuiltinFunction.TextPrint);
            //     yield return new Token(ParenthesisOpen);
            //     yield return new ConstantToken(new IntVariant(line));
            //     yield return new Token(ParenthesisClose);
            // }

            lastToken = inAssignmentStatement && t is { Type: Identifier } ? StripAssociatedData(t) : t;
        }

        if (lastToken != null)
            yield return lastToken;
    }
    
    public static Token ReplaceToken(Token cursor, Token target, Token replacement)
    {
        return TokenEquals(cursor, target) ? replacement : cursor;
    }

    public static IEnumerable<Token> ReplaceTokens(IEnumerable<Token> haystack, IEnumerable<Token> needle,
        IEnumerable<Token> replacements)
    {
        var haystackList = haystack.ToList();
        var needleList = needle.ToList();
        var replacementsList = replacements.ToList();

        if (needleList.Count == 0) return haystackList;

        var result = new List<Token>();
        var i = 0;

        while (i < haystackList.Count)
        {
            if (IsMatch(haystackList, needleList, i))
            {
                result.AddRange(replacementsList);
                i += needleList.Count;
            }
            else
            {
                result.Add(haystackList[i]);
                i++;
            }
        }

        return result;
    }

    private static bool IsMatch(List<Token> haystack, List<Token> needle, int startIndex)
    {
        if (startIndex + needle.Count > haystack.Count)
            return false;

        for (int i = 0; i < needle.Count; i++)
        {
            if (!TokenEquals(haystack[startIndex + i], needle[i]))
                return false;
        }

        return true;
    }

    private static bool TokenEquals(Token token, Token token1)
    {
        if (token is IdentifierToken id && token1 is IdentifierToken id1)
        {
            return id.Name == id1.Name;
        }
        if (token is ConstantToken constant && token1 is ConstantToken constant1)
        {
            return constant.Value.Equals(constant1.Value);
        }
        return token.Type == token1.Type && token.AssociatedData == token1.AssociatedData;
    }

    private static Token StripAssociatedData(Token token)
    {
        token.AssociatedData = null;
        return token;
    }
}