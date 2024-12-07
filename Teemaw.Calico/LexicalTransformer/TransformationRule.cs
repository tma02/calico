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
/// <param name="Name">The name of this descriptor. Used for logging.</param>
/// <param name="Pattern">A list of checks to be used in a MultiTokenWaiter.</param>
/// <param name="Tokens">A list of GDScript tokens which will be patched in.</param>
/// <param name="Operation">The type of patch.</param>
public record TransformationRule(
    string Name,
    MultiTokenPattern Pattern,
    IEnumerable<Token> Tokens,
    Operation Operation = Operation.Append)
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
    
    public MultiTokenWaiter CreateMultiTokenWaiter() => new(Pattern);
}

public class TransformationRuleBuilder
{
    private string? _name;
    private MultiTokenPattern? _pattern;
    private IEnumerable<Token>? _tokens;
    private Operation _operation = Operation.Append;

    public TransformationRuleBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    public TransformationRuleBuilder Matching(MultiTokenPattern pattern)
    {
        _pattern = pattern;
        return this;
    }

    public TransformationRuleBuilder With(IEnumerable<Token> tokens)
    {
        _tokens = tokens;
        return this;
    }

    public TransformationRuleBuilder Do(Operation operation)
    {
        _operation = operation;
        return this;
    }
    
    public TransformationRule Build()
    {
        if (string.IsNullOrEmpty(_name))
        {
            throw new ArgumentNullException(nameof(_name), "Name cannot be null or empty");
        }
        if (_pattern == null) 
        {
            throw new ArgumentNullException(nameof(_pattern), "Pattern cannot be null");
        }
        if (_tokens == null)
        {
            throw new ArgumentNullException(nameof(_tokens), "Tokens cannot be null");
        }

        return new TransformationRule(_name, _pattern, _tokens, _operation);
    }
}