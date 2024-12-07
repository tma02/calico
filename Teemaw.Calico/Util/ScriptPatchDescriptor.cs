using GDWeave.Godot;
using GDWeave.Modding;

namespace Teemaw.Calico.Util;

using MultiTokenChecks = Func<Token, bool>[];

public enum PatchType
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
    ReplaceFinal,

    /// <summary>
    /// Appends after the final token of the waiter.
    /// </summary>
    Append,
}

/// <summary>
/// This holds the information required to perform a patch at a single locus.
/// </summary>
/// <param name="name">The name of this descriptor. Used for logging.</param>
/// <param name="checks">A list of checks to be used in a MultiTokenWaiter.</param>
/// <param name="tokens">A list of GDScript tokens which will be patched in.</param>
/// <param name="patchType">The type of patch.</param>
public class ScriptPatchDescriptor(
    string name,
    MultiTokenChecks checks,
    IEnumerable<Token> tokens,
    PatchType patchType = PatchType.Append)
{
    /// <summary>
    /// This holds the information required to perform a patch at a single locus.
    /// </summary>
    /// <param name="name">The name of this descriptor. Used for logging.</param>
    /// <param name="checks">A list of checks to be used in a MultiTokenWaiter.</param>
    /// <param name="snippet">A snippet of GDScript which will be patched in.</param>
    /// <param name="patchType">The type of patch.</param>
    public ScriptPatchDescriptor(string name, MultiTokenChecks checks, string snippet,
        PatchType patchType = PatchType.Append) :
        this(name, checks, ScriptTokenizer.Tokenize(snippet), patchType)
    {
    }

    /// <summary>
    /// This holds the information required to perform a patch at a single locus.
    /// </summary>
    /// <param name="name">The name of this descriptor. Used for logging.</param>
    /// <param name="check">A check to be used in a MultiTokenWaiter.</param>
    /// <param name="snippet">A snippet of GDScript which will be patched in.</param>
    /// <param name="patchType">The type of patch.</param>
    public ScriptPatchDescriptor(string name, Func<Token, bool> check, string snippet,
        PatchType patchType = PatchType.Append) :
        this(name, [check], ScriptTokenizer.Tokenize(snippet), patchType)
    {
    }

    public string GetName() => name;

    public MultiTokenWaiter CreateMultiTokenWaiter() => new(checks);

    public IEnumerable<Token> GetTokens() => tokens;

    public PatchType GetPatchType() => patchType;
}