using GDWeave;

namespace Teemaw.Calico.LexicalTransformer;

public class TransformationRuleScriptModBuilder
{
    private IModInterface? _mod;
    private string? _name;
    private string? _scriptPath;
    private Func<bool> _predicate = () => true;
    private List<TransformationRule> _rules = [];

    /// <summary>
    /// Sets the IModInterface to be used by the ScriptMod.
    /// </summary>
    /// <param name="mod"></param>
    /// <returns></returns>
    public TransformationRuleScriptModBuilder ForMod(IModInterface mod)
    {
        _mod = mod;
        return this;
    }

    /// <summary>
    /// Sets the predicate which must return true for this ScriptMod to run. Config options MUST be
    /// evaluated at patch-time since the environment is not fully known at load-time. Currently,
    /// this affects mod conflict detection, but there may be other affected features in the future.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public TransformationRuleScriptModBuilder When(Func<bool> predicate)
    {
        _predicate = predicate;
        return this;
    }

    /// <summary>
    /// Sets the name of the ScriptMod. Used for logging.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public TransformationRuleScriptModBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the Godot resource path of the script to be patched.
    /// </summary>
    /// <param name="scriptPath"></param>
    /// <returns></returns>
    public TransformationRuleScriptModBuilder Patching(string scriptPath)
    {
        _scriptPath = scriptPath;
        return this;
    }

    /// <summary>
    /// Adds a TransformationRule to the TransformationRuleScriptMod.
    /// </summary>
    /// <param name="rule"></param>
    /// <returns></returns>
    public TransformationRuleScriptModBuilder AddRule(TransformationRule rule)
    {
        if (_rules.Select(r => r.Name).Contains(rule.Name))
        {
            throw new InvalidOperationException(
                $"Another rule with the name '{rule.Name}' already exists!");
        }

        _rules.Add(rule);
        return this;
    }

    /// <summary>
    /// Adds the TransformationRule built by calling Build() on the provided builder to the TransformationRuleScriptMod.
    /// </summary>
    /// <param name="rule"></param>
    /// <returns></returns>
    public TransformationRuleScriptModBuilder AddRule(TransformationRuleBuilder rule)
    {
        return AddRule(rule.Build());
    }

    /// <summary>
    /// Build the TransformationRuleScriptMod.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Thrown if any required fields were not set.</exception>
    public TransformationRuleScriptMod Build()
    {
        if (_mod == null)
        {
            throw new ArgumentNullException(nameof(_mod), "Mod cannot be null");
        }

        if (string.IsNullOrEmpty(_name))
        {
            throw new ArgumentNullException(nameof(_name), "Name cannot be null or empty");
        }

        if (string.IsNullOrEmpty(_scriptPath))
        {
            throw new ArgumentNullException(nameof(_scriptPath),
                "Script path cannot be null or empty");
        }

        return new TransformationRuleScriptMod(_mod, _name, _scriptPath, _predicate,
            _rules.ToArray());
    }
}