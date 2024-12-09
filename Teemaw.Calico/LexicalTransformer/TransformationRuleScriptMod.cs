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
        var eligibleRules = rules.Where(rule =>
        {
            var eligible = rule.Predicate();
            if (!eligible)
            {
                mod.Logger.Information($"[calico.{name}] Skipping patch {rule.Name}...");
            }

            return eligible;
        }).ToList();
        var transformers = eligibleRules.Select(rule =>
                (Rule: rule, Waiter: rule.CreateMultiTokenWaiter(), Buffer: new List<Token>()))
            .ToList();
        mod.Logger.Information($"[calico.{name}] Patching {path}");

        var patchOccurrences = eligibleRules.ToDictionary(r => r.Name, r => (Occurred: 0, Expected: r.Times));
        var yieldAfter = true;

        foreach (var t in tokens)
        {
            var buffersAtThisToken = 0;
            var flushesAtThisToken = 0;

            transformers.ForEach(w => w.Waiter.Check(t));
            foreach (var w in transformers.Where(w => w.Waiter.Step == 0))
            {
                // Flush any tokens in the buffer if we are out of the match
                foreach (var bufferedToken in w.Buffer)
                {
                    yield return bufferedToken;
                }

                if (w.Buffer.Count > 0)
                {
                    flushesAtThisToken += 1;
                }

                w.Buffer.Clear();
                // We haven't buffered the current token so we can leave yieldAfter as what it is.
            }

            foreach (var w in transformers.Where(w => w.Waiter.Step > 0))
            {
                if (w.Rule.Operation.RequiresBuffer())
                {
                    w.Buffer.Add(t);
                    buffersAtThisToken += 1;
                    yieldAfter = false;
                }

                if (!w.Waiter.Matched)
                {
                    continue;
                }

                if (w.Rule.Operation.YieldTokenBeforeOperation())
                {
                    yield return t;
                    yieldAfter = false;
                }
                else
                {
                    yieldAfter = w.Rule.Operation.YieldTokenAfterOperation();
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
                    case Append:
                    case ReplaceAll:
                        w.Buffer.Clear();
                        foreach (var patchToken in w.Rule.Tokens)
                        {
                            yield return patchToken;
                        }

                        break;
                    case None:
                    default:
                        break;
                }

                mod.Logger.Information($"[calico.{name}] Patch {w.Rule.Name} OK!");
                patchOccurrences[w.Rule.Name] = patchOccurrences[w.Rule.Name] with
                {
                    Occurred = patchOccurrences[w.Rule.Name].Occurred + 1
                };
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
                    $"[calico.{name}] {t} Token buffered by multiple transformers. This may cause unexpected behavior! Do you have overlapping transformer rules?");
            }

            if (flushesAtThisToken > 1)
            {
                mod.Logger.Warning(
                    $"[calico.{name}] Flushes performed by multiple transformers at this token. This may cause unexpected behavior! Do you have overlapping transformer rules?");
            }
        }

        foreach (var result in patchOccurrences.Where(result => result.Value.Occurred != result.Value.Expected))
        {
            mod.Logger.Error(
                $"[calico.{name}] Patch {result.Key} FAILED! Times expected={result.Value.Expected}, actual={result.Value.Occurred}");
        }
    }
}