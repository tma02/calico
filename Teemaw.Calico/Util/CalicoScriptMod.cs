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
/// <param name="patches">A list of patches to perform.</param>
public class CalicoScriptMod(IModInterface mod, string name, string scriptPath, ScriptPatchDescriptor[] patches) : IScriptMod
{
    public bool ShouldRun(string path) => path == scriptPath;

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        var waiters = patches.Select(patch => (Patch: patch, Waiter: patch.CreateMultiTokenWaiter()))
            .ToList();
        mod.Logger.Information($"[{name}] Patching {path}");

        var patchResults = patches.ToDictionary(p => p.GetName(), _ => false);
        bool yieldAfter = true;
        
        foreach (var t in tokens)
        {
            mod.Logger.Information(t.ToString());
            foreach (var w in waiters.Where(w => w.Waiter.Check(t)))
            {
                switch (w.Patch.GetPatchType())
                {
                    case Append:
                        yield return t;
                        // We want yieldAfter = false; here, but it's actually assigned in ReplaceFinal during this
                        // code path.

                        goto case ReplaceFinal;
                    case ReplaceFinal:
                        foreach (var patchToken in w.Patch.GetTokens())
                        {
                            yield return patchToken;
                        }
                        yieldAfter = false;

                        break;
                    case None:
                    default:
                        break;
                }

                mod.Logger.Information($"[{name}] Patch {w.Patch.GetName()} OK!");
                patchResults[w.Patch.GetName()] = true;
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
            mod.Logger.Error($"[{name}] Patch {result.Key} FAILED!");
        }
    }
}