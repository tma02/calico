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
public class TransformationRuleScriptMod(
    IModInterface mod,
    string name,
    string scriptPath,
    Func<bool> predicate,
    TransformationRule[] rules)
    : IScriptMod
{
    public bool ShouldRun(string path)
    {
        if (path != scriptPath) return false;
        if (!predicate.Invoke())
        {
            mod.Logger.Information($"[calico.{name}] Predicate failed, not patching.");
            return false;
        }

        return true;
    }

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

        var patchOccurrences =
            eligibleRules.ToDictionary(r => r.Name, r => (Occurred: 0, Expected: r.Times));
        var bufferAfterChecks = true;
        var stagingBuffer = new List<Token>();
        stagingBuffer.AddRange(tokens);
        var transformedBuffer = new List<Token>();

        foreach (var transformer in transformers)
        {
            var hasScopePattern = transformer.Rule.ScopePattern.Length > 0;
            var inScope = !hasScopePattern;
            uint? scopeIndent = null;
            var scopeWaiter = transformer.Rule.CreateMultiTokenWaiterForScope();

            foreach (var token in stagingBuffer)
            {
                if (inScope && hasScopePattern)
                {
                    // Try to find the scope's base indentation
                    if (scopeIndent == null && token.Type == TokenType.Newline)
                    {
                        scopeIndent = token.AssociatedData ?? 0;
                    }
                    // Check if we should leave the scope
                    else if (scopeIndent != null && token.Type == TokenType.Newline)
                    {
                        inScope = (token.AssociatedData ?? 0) >= scopeIndent;
                    }
                }
                else if (hasScopePattern)
                {
                    // We should never reach this case if the scope pattern has Length = 0.
                    if (scopeWaiter.Check(token))
                    {
                        scopeWaiter.Reset();
                        // Latch inScope to true if we match the scope pattern.
                        inScope = true;
                    }
                }

                if (!inScope)
                {
                    transformedBuffer.Add(token);
                    continue;
                }

                transformer.Waiter.Check(token);
                if (transformer.Waiter.Step == 0)
                {
                    transformedBuffer.AddRange(transformer.Buffer);
                    transformer.Buffer.Clear();
                }
                else
                {
                    if (transformer.Rule.Operation.RequiresBuffer())
                    {
                        transformer.Buffer.Add(token);
                        bufferAfterChecks = false;
                    }

                    if (transformer.Waiter.Matched)
                    {
                        transformer.Waiter.Reset();

                        if (transformer.Rule.Operation.YieldTokenBeforeOperation())
                        {
                            transformedBuffer.Add(token);
                            bufferAfterChecks = false;
                        }
                        else
                        {
                            bufferAfterChecks =
                                transformer.Rule.Operation.YieldTokenAfterOperation();
                        }

                        switch (transformer.Rule.Operation)
                        {
                            case Prepend:
                                transformedBuffer.AddRange(transformer.Rule.Tokens);
                                transformedBuffer.AddRange(transformer.Buffer);
                                transformer.Buffer.Clear();
                                break;
                            case ReplaceLast:
                            case Append:
                            case ReplaceAll:
                                transformer.Buffer.Clear();
                                transformedBuffer.AddRange(transformer.Rule.Tokens);
                                break;
                            case None:
                            default:
                                break;
                        }

                        mod.Logger.Information(
                            $"[calico.{name}] Patch {transformer.Rule.Name} OK!");
                        patchOccurrences[transformer.Rule.Name] =
                            patchOccurrences[transformer.Rule.Name] with
                            {
                                Occurred = patchOccurrences[transformer.Rule.Name].Occurred + 1
                            };
                    }
                }

                if (bufferAfterChecks)
                {
                    transformedBuffer.Add(token);
                }
                else
                {
                    bufferAfterChecks = true;
                }
            }

            stagingBuffer.Clear();
            stagingBuffer.AddRange(transformedBuffer);
            transformedBuffer.Clear();
        }

        foreach (var result in patchOccurrences.Where(result =>
                     result.Value.Occurred != result.Value.Expected))
        {
            mod.Logger.Error(
                $"[calico.{name}] Patch {result.Key} FAILED! Times expected={result.Value.Expected}, actual={result.Value.Occurred}");
        }

        return stagingBuffer;
    }
}