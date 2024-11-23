using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;

namespace Teemaw.Calico;

public class PhysicsProcessScriptMod(IModInterface mod): IScriptMod
{

    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        var _calico_frame = 0

        """);

    // TODO: LERP or maybe don't patch players?
    private readonly IEnumerable<Token> _shutter = ScriptTokenizer.Tokenize(
        """

        if _calico_frame % 2 == 1: 
        	_calico_frame = 0
        	return
        _calico_frame += 1

        """, 1);
    
    //public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/fishing_line.gdc";
    public bool ShouldRun(string path)
    {
        return path.StartsWith("res://Scenes/Entities/") && !path.EndsWith("/actor.gdc") &&
               !path.EndsWith("/prop.gdc") && !path.EndsWith("/player.gdc");
    }

    private readonly Dictionary<string, bool> _injectedGlobals = new();

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        // We need new waiters for each Modify call.
        MultiTokenWaiter extendsWaiter = new([
            t => t.Type is TokenType.PrExtends,
            t => t.Type is TokenType.Newline
        ], allowPartialMatch: true);
        
        MultiTokenWaiter classWaiter = new([
            t => t.Type is TokenType.PrClassName,
            t => t.Type is TokenType.Newline
        ], allowPartialMatch: true);
        
        MultiTokenWaiter physicsProcessWaiter = new([
            t => t is { Type: TokenType.PrFunction },
            t => t is IdentifierToken { Name: "_physics_process" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t is IdentifierToken { Name: "delta" },
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon
        ]);
        _injectedGlobals[path] = false;
        
        mod.Logger.Information($"Patching {path}");
        foreach (var t in tokens)
        {
            if (extendsWaiter.Check(t))
            {
                _injectedGlobals[path] = true;
                yield return t;
                
                mod.Logger.Information("Injecting globals");
                foreach (var t1 in _globals)
                    yield return t1;
            }
            else if (physicsProcessWaiter.Check(t) && _injectedGlobals[path])
            {
                yield return t;
                
                mod.Logger.Information("Injecting _physics_process shutter");
                foreach (var t1 in _shutter)
                    yield return t1;
            }
            else
            {
                yield return t;
            }
        }
    }
}