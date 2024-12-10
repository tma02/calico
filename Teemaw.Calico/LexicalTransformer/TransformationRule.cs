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
    /// Replace all tokens of the waiter. This is a buffered operation. Buffered rules do not support overlapping token
    /// patterns with other buffered rules.
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
    /// Prepends before the first token of the waiter. This is a buffered operation. Buffered rules do not support
    /// overlapping token patterns with other buffered rules.
    /// </summary>
    Prepend,
}

public static class OperationExtensions
{
    public static bool RequiresBuffer(this Operation operation)
    {
        return operation is Operation.ReplaceAll or Operation.Prepend;
    }

    public static bool YieldTokenBeforeOperation(this Operation operation)
    {
        return operation is Operation.Append or Operation.None;
    }

    public static bool YieldTokenAfterOperation(this Operation operation)
    {
        return !operation.RequiresBuffer() && !operation.YieldTokenBeforeOperation() &&
               operation != Operation.ReplaceLast;
    }
}

/// <summary>
/// This holds the information required to perform a patch at a single locus.
/// </summary>
/// <param name="Name">The name of this descriptor. Used for logging.</param>
/// <param name="Pattern">A list of checks to be used in a MultiTokenWaiter.</param>
/// <param name="Tokens">A list of GDScript tokens which will be patched in.</param>
/// <param name="Operation">The type of patch.</param>
/// <param name="Times">The number of times this rule is expected to match.</param>
/// <param name="Predicate">A predicate which must return true for the rule to match.</param>
public record TransformationRule(
    string Name,
    MultiTokenPattern Pattern,
    IEnumerable<Token> Tokens,
    Operation Operation,
    uint Times,
    Func<bool> Predicate)
{

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
    private Func<bool> _predicate = () => true;

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
    public TransformationRuleBuilder ExpectTimes(uint times)
    {
        _times = times;
        return this;
    }

    /// <summary>
    /// Sets the predicate function whose return value will decide if this rule will be checked.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public TransformationRuleBuilder When(Func<bool> predicate)
    {
        _predicate = predicate;
        return this;
    }

    /// <summary>
    /// Sets a value which will decide if this rule will be checked.
    /// </summary>
    /// <param name="eligible">If true, this rule will be checked.</param>
    /// <returns></returns>
    public TransformationRuleBuilder When(bool eligible)
    {
        _predicate = () => eligible;
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

        return new TransformationRule(_name, _pattern, _tokens, _operation, _times, _predicate);
    }
}