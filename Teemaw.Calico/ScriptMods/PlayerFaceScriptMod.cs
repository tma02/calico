using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico.ScriptMods;

public class PlayerFaceScriptMod(IModInterface mod): IScriptMod
{
    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/Face/player_face.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter resetTimeWaiter = new([
            t => t is IdentifierToken { Name: "reset_time" },
            t => t.Type is OpAssignSub,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(1)),
        ]);
        MultiTokenWaiter blinkTimeWaiter = new([
            t => t is IdentifierToken { Name: "blink_time" },
            t => t.Type is OpAssignSub,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(1)),
        ]);
        MultiTokenWaiter emoteTimeWaiter = new([
            t => t is IdentifierToken { Name: "emote_time" },
            t => t.Type is OpAssignSub,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(1)),
        ]);
        
        mod.Logger.Information($"[calico.PlayerFaceScript] Patching {path}");
        
        var patchFlags = new Dictionary<string, int>
        {
            ["reset_time"] = 0,
            ["blink_time"] = 0,
            ["emote_time"] = 0,
        };
        
        foreach (var t in tokens)
        {
            if (resetTimeWaiter.Check(t))
            {
                yield return new ConstantToken(new IntVariant(60));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["reset_time"]++;
                mod.Logger.Information("[calico.PlayerFaceScript] reset_time patch");
            }
            else if (blinkTimeWaiter.Check(t))
            {
                blinkTimeWaiter.Reset();
                yield return new ConstantToken(new IntVariant(60));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["blink_time"]++;
                mod.Logger.Information("[calico.PlayerFaceScript] blink_time patch");
            }
            else if (emoteTimeWaiter.Check(t))
            {
                yield return new ConstantToken(new IntVariant(60));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["emote_time"]++;
                mod.Logger.Information("[calico.PlayerFaceScript] emote_time patch");
            }
            else
            {
                yield return t;
            }
        }
        
        foreach (var patch in patchFlags)
        {
            if (patch.Value == 0)
            {
                mod.Logger.Error($"[calico.PlayerFaceScript] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}