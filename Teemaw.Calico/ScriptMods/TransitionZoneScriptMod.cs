using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico.ScriptMods;

public class TransitionZoneScriptMod(IModInterface mod): IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/Map/Tools/transition_zone.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter getTreeWaiter = new([
            t => t is { Type: PrYield },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "get_tree" },
        ]);
        
        mod.Logger.Information($"[calico.TransitionZoneScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["get_tree"] = false
        };
        
        foreach (var t in tokens)
        {
            if (getTreeWaiter.Check(t))
            {
                yield return new IdentifierToken("actor");
                yield return new Token(Period);
                yield return t;
                patchFlags["get_tree"] = true;
                mod.Logger.Information("[calico.TransitionZoneScriptMod] get_tree patch OK");
            }
            else
            {
                yield return t;
            }
        }
        
        foreach (var patch in patchFlags)
        {
            if (!patch.Value)
            {
                mod.Logger.Error($"[calico.TransitionZoneScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}