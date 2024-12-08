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
/// <param name="Times">The number of times this rule is expected to match.</param>
public record TransformationRule(
    string Name,
    MultiTokenPattern Pattern,
    IEnumerable<Token> Tokens,
    Operation Operation = Operation.Append,
    uint Times = 1)
{
    /// <summary>
    /// This holds the information required to perform a patch at a single locus.
    /// </summary>
    /// <param name="name">The name of this descriptor. Used for logging.</param>
    /// <param name="pattern">A list of checks to be used in a MultiTokenWaiter.</param>
    /// <param name="snippet">A snippet of GDScript which will be patched in.</param>
    /// <param name="operation">The type of patch.</param>
    /// <param name="times">The number of times this rule is expected to match.</param>
    public TransformationRule(string name, MultiTokenPattern pattern, string snippet,
       Operation operation = Operation.Append,  uint times = 1) :
        this(name, pattern, ScriptTokenizer.Tokenize(snippet), operation, times)
    {
    }

    /// <summary>
    /// This holds the information required to perform a patch at a single locus.
    /// </summary>
    /// <param name="name">The name of this descriptor. Used for logging.</param>
    /// <param name="pattern">A list of checks to be used in a MultiTokenWaiter.</param>
    /// <param name="token">A GDScript Token which will be patched in.</param>
    /// <param name="operation">The type of patch.</param>
    /// <param name="times">The number of times this rule is expected to match.</param>
    public TransformationRule(string name, MultiTokenPattern pattern, Token token,
        Operation operation = Operation.Append, uint times = 1) :
        this(name, pattern, [token], operation, times)
    {
    }

    public MultiTokenWaiter CreateMultiTokenWaiter() => new(Pattern);
}

/// <summary>
/// Builder for TransformationRule. Times defaults to 1. Operation defaults to <see cref="Operation.Append"/>.
/// </summary>
public class TransformationRuleBuilder
{
    private string? _name;
    private MultiTokenPattern? _pattern;
    private IEnumerable<Token>? _tokens;
    private uint _times = 1;
    private Operation _operation = Operation.Append;

    /// <summary>
    /// Sets the name for the TransformationRule. Used for logging.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public TransformationRuleBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the token pattern which will be matched by the TransformationRule.
    /// </summary>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public TransformationRuleBuilder Matching(MultiTokenPattern pattern)
    {
        _pattern = pattern;
        return this;
    }

    /// <summary>
    /// Sets the token content which will be patched in for the TransformationRule.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    public TransformationRuleBuilder With(IEnumerable<Token> tokens)
    {
        _tokens = tokens;
        return this;
    }

    /// <summary>
    /// Sets the token content which will be patched in for the TransformationRule.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public TransformationRuleBuilder With(Token token)
    {
        _tokens = [token];
        return this;
    }

    /// <summary>
    /// Sets the token content which will be patched in for the TransformationRule with a GDScript snippet.
    /// </summary>
    /// <param name="snippet"></param>
    /// <param name="indent">The base indentation level for the tokenizer.</param>
    /// <returns></returns>
    public TransformationRuleBuilder With(string snippet, uint indent = 0)
    {
        _tokens = ScriptTokenizer.Tokenize(snippet, indent);
        return this;
    }

    /// <summary>
    /// Sets the <see cref="Operation"/> of the TransformationRule.
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public TransformationRuleBuilder Do(Operation operation)
    {
        _operation = operation;
        return this;
    }

    /// <summary>
    /// Sets the number of times the rule is expected to match.
    /// </summary>
    /// <param name="times"></param>
    /// <returns></returns>
    public TransformationRuleBuilder Times(uint times)
    {
        _times = times;
        return this;
    }

    /// <summary>
    /// Builds the TransformationRule.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown if any required fields were not set.</exception>
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

        return new TransformationRule(_name, _pattern, _tokens, _operation, _times);
    }
}