using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class GlobalsScriptMod(IModInterface mod, Config config): IScriptMod
{
    private readonly IEnumerable<Token> _onReadyRenderMultiThread = ScriptTokenizer.Tokenize(
        """

        print("[calico] Enabling multi-thread rendering...")
        ProjectSettings.set_setting("rendering/driver/threads/thread_model", 2)

        """, 1);
    
    private readonly IEnumerable<Token> _onReadyPhysicsFps = ScriptTokenizer.Tokenize(
        """

        print("[calico] Setting physics FPS...")
        ProjectSettings.set_setting("physics/common/physics_fps", 30)

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

        foreach (var t in tokens)
        {
            if (readyWaiter.Check(t))
            {
                yield return t;
                if (config.MultiThreadRenderingEnabled)
                {
                    foreach (var t1 in _onReadyRenderMultiThread)
                        yield return t1;
                }
                if (config.PhysicsHalfSpeedEnabled)
                {
                    foreach (var t1 in _onReadyPhysicsFps)
                        yield return t1;
                }
            }
            else
            {
                yield return t;
            }
        }
    }
}