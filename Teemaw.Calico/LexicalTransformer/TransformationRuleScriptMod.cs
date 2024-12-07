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

        var patchResults = rules.ToDictionary(r => r.GetName(), _ => false);
        var yieldAfter = true;

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
                yieldAfter = false;

                if (!w.Waiter.Matched)
                {
                    continue;
                }

                w.Waiter.Reset();

                switch (w.Rule.GetPatchType())
                {
                    case Prepend:
                        foreach (var patchToken in w.Rule.GetTokens())
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
                        foreach (var patchToken in w.Rule.GetTokens())
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

                mod.Logger.Information($"[calico.{name}] Patch {w.Rule.GetName()} OK!");
                patchResults[w.Rule.GetName()] = true;
            }

            if (yieldAfter)
            {
                yield return t;
            }
            else
            {
                yieldAfter = true;
            }
        }

        foreach (var result in patchResults.Where(result => !result.Value))
        {
            mod.Logger.Error($"[calico.{name}] Patch {result.Key} FAILED!");
        }
    }
}