using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMods;

public class GlobalsScriptMod(IModInterface mod): IScriptMod
{
    private static readonly IEnumerable<Token> OnReadyPhysicsFps = ScriptTokenizer.Tokenize(
        """

        print("[calico] Setting physics FPS...")
        Engine.set_iterations_per_second(30)

        """, 1);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Singletons/globals.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter readyWaiter = new([
            t => t.Type is PrFunction,
            t => t is IdentifierToken { Name: "_ready" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);
        
        mod.Logger.Information($"[calico.GlobalsScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["ready"] = false
        };

        foreach (var t in tokens)
        {
            if (readyWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in OnReadyPhysicsFps)
                    yield return t1;
                patchFlags["ready"] = true;
                mod.Logger.Information("[calico.GlobalsScriptMod] _ready patch OK");
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
                mod.Logger.Error($"[calico.GlobalsScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}