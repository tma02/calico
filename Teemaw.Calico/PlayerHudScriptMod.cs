using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class PlayerHudScriptMod(IModInterface mod): IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/HUD/player_hud.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter interactTimerIncrement = new([
            t => t is IdentifierToken { Name: "interact_timer" },
            t => t.Type is OpAssignAdd,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(2)),
        ]);
        MultiTokenWaiter interactTimerDecrement = new([
            t => t is IdentifierToken { Name: "interact_timer" },
            t => t.Type is OpAssignSub,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(2)),
        ]);
        MultiTokenWaiter dialogCooldownDecrement = new([
            t => t is IdentifierToken { Name: "dialogue_cooldown" },
            t => t.Type is OpAssignSub,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(1)),
        ]);
        
        mod.Logger.Information($"[calico.PlayerHudScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["interact_increment"] = false,
            ["interact_decrement"] = false,
            ["dialog_decrement"] = false,
        };
        
        foreach (var t in tokens)
        {
            if (interactTimerIncrement.Check(t))
            {
                yield return new ConstantToken(new IntVariant(60));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["interact_increment"] = true;
                mod.Logger.Information("[calico.PlayerHudScriptMod] interact_increment patch OK");
            }
            else if (interactTimerDecrement.Check(t))
            {
                yield return new ConstantToken(new IntVariant(60));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["interact_decrement"] = true;
                mod.Logger.Information("[calico.PlayerHudScriptMod] interact_decrement patch OK");
            }
            else if (dialogCooldownDecrement.Check(t))
            {
                yield return new ConstantToken(new IntVariant(60));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["dialog_decrement"] = true;
                mod.Logger.Information("[calico.PlayerHudScriptMod] dialog_decrement patch OK");
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
                mod.Logger.Error($"[calico.PlayerHudScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}