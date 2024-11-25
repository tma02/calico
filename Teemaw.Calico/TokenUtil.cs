using GDWeave.Godot;
using GDWeave.Godot.Variants;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class TokenUtil
{
    public static IEnumerable<Token> ReplaceAssignmentsAsDeferred(IEnumerable<Token> tokens)
    {
        Token? lastToken = null;
        var inAssignmentStatement = false;
        foreach (var t in tokens)
        {
            if (lastToken is IdentifierToken identifier && t.Type == OpAssign)
            {
                inAssignmentStatement = true;
                yield return new IdentifierToken("set_deferred");
                yield return new Token(ParenthesisOpen);
                yield return new ConstantToken(new StringVariant(identifier.Name));
                yield return new Token(Comma);
                // Don't return the last token
                lastToken = t;
                continue;
            }
            if (t.Type == Newline && inAssignmentStatement)
            {
                inAssignmentStatement = false;
                // Close out the set_deferred call...
                yield return new Token(ParenthesisClose);
                // ...before the Newline token.
                yield return t;
            }
            if (lastToken != null)
                yield return lastToken;
            lastToken = t;
        }
        if (lastToken != null)
            yield return lastToken;
    }
}