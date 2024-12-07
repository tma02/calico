using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static Teemaw.Calico.Util.PatchType;

namespace Teemaw.Calico.Util;

/// <summary>
/// An IScriptMod implementation that handles patching through the provided list of ScriptPatchDescriptors.
/// </summary>
/// <param name="mod">IModInterface of the current mod.</param>
/// <param name="name">The name of this script mod. Used for logging.</param>
/// <param name="scriptPath">The GD res:// path of the script which will be patched.</param>
/// <param name="patches">A list of patches to perform. Multiple descriptors which patch at the same locus is not supported.</param>
public class CalicoScriptMod(IModInterface mod, string name, string scriptPath, ScriptPatchDescriptor[] patches)
    : IScriptMod
{
    public bool ShouldRun(string path) => path == scriptPath;

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        var waiters = patches.Select(patch =>
                (Patch: patch, Waiter: patch.CreateMultiTokenWaiter(), Buffer: new List<Token>()))
            .ToList();
        mod.Logger.Information($"[calico.{name}] Patching {path}");

        var patchResults = patches.ToDictionary(p => p.GetName(), _ => false);
        var yieldAfter = true;

        foreach (var t in tokens)
        {
            waiters.ForEach(w => w.Waiter.Check(t));
            foreach (var w in waiters.Where(w => w.Waiter.Step == 0))
            {
                // Flush any tokens in the buffer if we are out of the match
                foreach (var bufferedToken in w.Buffer)
                {
                    mod.Logger.Information(bufferedToken.ToString());
                    yield return bufferedToken;
                }
                w.Buffer.Clear();
                // We didn't buffer the current token so we can leave yieldAfter as what it is.
            }
            foreach (var w in waiters.Where(w => w.Waiter.Step > 0))
            {
                w.Buffer.Add(t);
                yieldAfter = false;

                if (!w.Waiter.Matched)
                {
                    continue;
                }
                w.Waiter.Reset();

                switch (w.Patch.GetPatchType())
                {
                    case ReplaceFinal:
                        w.Buffer.RemoveAt(w.Buffer.Count - 1);

                        goto case Append;
                    case Append:
                        foreach (var bufferedToken in w.Buffer)
                        {
                            mod.Logger.Information(bufferedToken.ToString());
                            yield return bufferedToken;
                        }

                        goto case ReplaceAll;
                    case ReplaceAll:
                        // All other patch cases terminate here to clear the buffer and return the patch tokens.
                        w.Buffer.Clear();
                        foreach (var patchToken in w.Patch.GetTokens())
                        {
                            mod.Logger.Information(patchToken.ToString());
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

                mod.Logger.Information($"[{name}] Patch {w.Patch.GetName()} OK!");
                patchResults[w.Patch.GetName()] = true;
            }

            if (yieldAfter)
            {
                mod.Logger.Information(t.ToString());
                yield return t;
            }
            else
            {
                yieldAfter = true;
            }
        }

        foreach (var result in patchResults.Where(result => !result.Value))
        {
            mod.Logger.Error($"[{name}] Patch {result.Key} FAILED!");
        }
    }
}