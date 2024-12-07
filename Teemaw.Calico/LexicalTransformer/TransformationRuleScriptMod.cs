using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static Teemaw.Calico.LexicalTransformer.Operation;

namespace Teemaw.Calico.LexicalTransformer;

/// <summary>
/// An IScriptMod implementation that handles patching through the provided list of TransformationRules.
/// </summary>
/// <param name="mod">IModInterface of the current mod.</param>
/// <param name="name">The name of this script mod. Used for logging.</param>
/// <param name="scriptPath">The GD res:// path of the script which will be patched.</param>
/// <param name="rules">A list of patches to perform. Multiple descriptors with overlapping checks is not supported.</param>
public class TransformationRuleScriptMod(IModInterface mod, string name, string scriptPath, TransformationRule[] rules)
    : IScriptMod
{
    public bool ShouldRun(string path) => path == scriptPath;

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        var transformers = rules.Select(rule =>
                (Rule: rule, Waiter: rule.CreateMultiTokenWaiter(), Buffer: new List<Token>()))
            .ToList();
        mod.Logger.Information($"[calico.{name}] Patching {path}");

        var patchResults = rules.ToDictionary(r => r.Name, _ => false);
        var yieldAfter = true;
        var buffersAtThisToken = 0;

        foreach (var t in tokens)
        {
            transformers.ForEach(w => w.Waiter.Check(t));
            foreach (var w in transformers.Where(w => w.Waiter.Step == 0))
            {
                // Flush any tokens in the buffer if we are out of the match
                foreach (var bufferedToken in w.Buffer)
                {
                    yield return bufferedToken;
                }

                w.Buffer.Clear();
                // We didn't buffer the current token so we can leave yieldAfter as what it is.
            }

            foreach (var w in transformers.Where(w => w.Waiter.Step > 0))
            {
                w.Buffer.Add(t);
                buffersAtThisToken += 1;
                yieldAfter = false;

                if (!w.Waiter.Matched)
                {
                    continue;
                }

                w.Waiter.Reset();

                switch (w.Rule.Operation)
                {
                    case Prepend:
                        foreach (var patchToken in w.Rule.Tokens)
                        {
                            yield return patchToken;
                        }

                        foreach (var bufferedToken in w.Buffer)
                        {
                            yield return bufferedToken;
                        }

                        w.Buffer.Clear();
                        break;
                    case ReplaceLast:
                        w.Buffer.RemoveAt(w.Buffer.Count - 1);

                        goto case Append;
                    case Append:
                        foreach (var bufferedToken in w.Buffer)
                        {
                            yield return bufferedToken;
                        }

                        goto case ReplaceAll;
                    case ReplaceAll:
                        w.Buffer.Clear();
                        foreach (var patchToken in w.Rule.Tokens)
                        {
                            yield return patchToken;
                        }

                        break;
                    case None:
                    default:
                        foreach (var bufferedToken in w.Buffer)
                        {
                            yield return bufferedToken;
                        }

                        w.Buffer.Clear();
                        break;
                }

                mod.Logger.Information($"[calico.{name}] Patch {w.Rule.Name} OK!");
                patchResults[w.Rule.Name] = true;
            }

            if (yieldAfter)
            {
                yield return t;
            }
            else
            {
                yieldAfter = true;
            }

            if (buffersAtThisToken > 1)
            {
                mod.Logger.Warning(
                    $"[calico.{name}] {t} Token buffered by multiple transformers. This may cause unexpected behavior!");
            }

            buffersAtThisToken = 0;
        }

        foreach (var result in patchResults.Where(result => !result.Value))
        {
            mod.Logger.Error($"[calico.{name}] Patch {result.Key} FAILED!");
        }
    }
}

public class TransformationRuleScriptModBuilder
{
    private IModInterface? _mod;
    private string? _name;
    private string? _scriptPath;
    private List<TransformationRule> _rules = [];

    public TransformationRuleScriptModBuilder ForMod(IModInterface mod)
    {
        _mod = mod;
        return this;
    }

    public TransformationRuleScriptModBuilder Named(string name)
    {
        _name = name;
        return this;
    }
    
    public TransformationRuleScriptModBuilder Patching(string scriptPath)
    {
        _scriptPath = scriptPath;
        return this;
    }

    public TransformationRuleScriptModBuilder AddRule(TransformationRule rule)
    {
        _rules.Add(rule);
        return this;
    }

    public TransformationRuleScriptModBuilder AddRule(TransformationRuleBuilder rule)
    {
        _rules.Add(rule.Build());
        return this;
    }

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
            throw new ArgumentNullException(nameof(_scriptPath), "Script path cannot be null or empty");
        }
        
        return new TransformationRuleScriptMod(_mod, _name, _scriptPath, _rules.ToArray());
    }
}