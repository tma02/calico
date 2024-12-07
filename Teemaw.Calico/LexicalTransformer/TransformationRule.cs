using GDWeave.Godot;
using GDWeave.Modding;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.LexicalTransformer;

using MultiTokenPattern = Func<Token, bool>[];

public enum Operation
{
    /// <summary>
    /// Do not patch.
    /// </summary>
    None,
    
    /// <summary>
    /// Replace all tokens of the waiter.
    /// </summary>
    ReplaceAll,

    /// <summary>
    /// Replace the final token of the waiter.
    /// </summary>
    ReplaceLast,

    /// <summary>
    /// Appends after the final token of the waiter.
    /// </summary>
    Append,
    
    /// <summary>
    /// Prepends before the first token of the waiter.
    /// </summary>
    Prepend,
}

/// <summary>
/// This holds the information required to perform a patch at a single locus.
/// </summary>
/// <param name="name">The name of this descriptor. Used for logging.</param>
/// <param name="pattern">A list of checks to be used in a MultiTokenWaiter.</param>
/// <param name="tokens">A list of GDScript tokens which will be patched in.</param>
/// <param name="operation">The type of patch.</param>
public class TransformationRule(
    string name,
    MultiTokenPattern pattern,
    IEnumerable<Token> tokens,
    Operation operation = Operation.Append)
{
    /// <summary>
    /// This holds the information required to perform a patch at a single locus.
    /// </summary>
    /// <param name="name">The name of this descriptor. Used for logging.</param>
    /// <param name="pattern">A list of checks to be used in a MultiTokenWaiter.</param>
    /// <param name="snippet">A snippet of GDScript which will be patched in.</param>
    /// <param name="operation">The type of patch.</param>
    public TransformationRule(string name, MultiTokenPattern pattern, string snippet,
        Operation operation = Operation.Append) :
        this(name, pattern, ScriptTokenizer.Tokenize(snippet), operation)
    {
    }

    /// <summary>
    /// This holds the information required to perform a patch at a single locus.
    /// </summary>
    /// <param name="name">The name of this descriptor. Used for logging.</param>
    /// <param name="pattern">A list of checks to be used in a MultiTokenWaiter.</param>
    /// <param name="token">A GDScript Token which will be patched in.</param>
    /// <param name="operation">The type of patch.</param>
    public TransformationRule(string name, MultiTokenPattern pattern, Token token,
        Operation operation = Operation.Append) :
        this(name, pattern, [token], operation)
    {
    }

    public string GetName() => name;

    public MultiTokenWaiter CreateMultiTokenWaiter() => new(pattern);

    public IEnumerable<Token> GetTokens() => tokens;

    public Operation GetPatchType() => operation;
}