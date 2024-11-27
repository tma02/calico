using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class HeldItemScriptMod(IModInterface mod): IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/held_item.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter physicsProcessWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_physics_process" },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "delta" },
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon,
            t => t.Type is Newline,
        ]);
        
        mod.Logger.Information($"[calico.HeldItemScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["physics_process"] = false
        };
        
        foreach (var t in tokens)
        {
            if (physicsProcessWaiter.Check(t))
            {
                yield return t;
                yield return new Token(CfReturn);
                patchFlags["physics_process"] = true;
                mod.Logger.Information("[calico.HeldItemScriptMod] _physics_process patch OK");
            }
            yield return t;
        }
        
        foreach (var patch in patchFlags)
        {
            if (!patch.Value)
            {
                mod.Logger.Error($"[calico.HeldItemScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}