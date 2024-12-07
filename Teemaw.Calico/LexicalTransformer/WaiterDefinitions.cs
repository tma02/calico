using GDWeave.Godot;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico.Util;

using MultiTokenChecks = Func<Token, bool>[];

public static class WaiterDefinitions
{
    /// <summary>
    /// Creates a new array of checks which matches `extends &lt;Identifier&gt;\n`. Useful for patching into the global
    /// scope.
    /// </summary>
    /// <returns></returns>
    public static MultiTokenChecks CreateGlobalsChecks()
    {
        return
        [
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ];
    }

    /// <summary>
    /// Creates a new array of checks which matches a function definition. This will not match the preceding or trailing
    /// Newline tokens.
    /// </summary>
    /// <param name="name">The name of the function to match.</param>
    /// <param name="args">
    /// An array of the names of arguments of the function to match. If null or empty, the returned checks will only
    /// match a function which does not accept arguments.
    /// </param>
    /// <returns></returns>
    public static MultiTokenChecks CreateFunctionDefinitionChecks(string name, string[]? args = null)
    {
        var checks = new List<Func<Token, bool>>();

        checks.Add(t => t.Type is PrFunction);
        checks.Add(t => t is IdentifierToken token && token.Name == name);
        checks.Add(t => t.Type is ParenthesisOpen);
        if (args is { Length: > 0 })
        {
            foreach (var arg in args)
            {
                checks.Add(t => t is IdentifierToken token && token.Name == arg);
                checks.Add(t => t.Type is Comma);
            }

            checks.RemoveAt(checks.Count - 1);
        }

        checks.Add(t => t.Type is ParenthesisClose);
        checks.Add(t => t.Type is Colon);

        return checks.ToArray();
    }

    /// <summary>
    /// Creates a new array of checks which matches the provided GDScript snippet.
    /// </summary>
    /// <param name="snippet">A GDScript snippet to match.</param>
    /// <returns></returns>
    public static MultiTokenChecks CreateSnippetChecks(string snippet)
    {
        var tokens = ScriptTokenizer.Tokenize(snippet);

        return tokens.Select(snippetToken => (Func<Token, bool>)(t =>
        {
            if (t.Type == Identifier)
            {
                return snippetToken is IdentifierToken snippetIdentifier && t is IdentifierToken identifier &&
                       snippetIdentifier.Name == identifier.Name;
            }

            if (t.Type == Constant)
            {
                return snippetToken is ConstantToken snippetConstant && t is ConstantToken constant &&
                       snippetConstant.Value.Equals(constant.Value);
            }

            return t.Type == snippetToken.Type;
        })).ToArray();
    }
}