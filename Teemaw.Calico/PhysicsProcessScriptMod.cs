using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;

namespace Teemaw.Calico;

public class PhysicsProcessScriptMod(IModInterface mod): IScriptMod
{
    private readonly MultiTokenWaiter _extendsWaiter = new([
        t => t.Type is TokenType.PrExtends,
        t => t.Type is TokenType.Newline
    ], allowPartialMatch: true);
    
    private readonly MultiTokenWaiter _physicsProcessWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_physics_process" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t is IdentifierToken { Name: "delta" },
        t => t.Type is TokenType.ParenthesisClose,
        t => t.Type is TokenType.Colon
    ]);

    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        var _calico_frame = 0

        """);

    private readonly IEnumerable<Token> _shutter = ScriptTokenizer.Tokenize(
        """

        _calico_frame += 1
        if _calico_frame % 2 == 0: return
        _calico_frame = 0

        """, 1);
    
    //public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/fishing_line.gdc";
    public bool ShouldRun(string path) => true;

    private bool _injectedGlobals = false;

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        foreach (var t in tokens)
        {
            if (_extendsWaiter.Check(t))
            {
                _injectedGlobals = true;
                yield return t;
                
                mod.Logger.Information("Injecting globals");
                // TODO: figure out why _globals doesn't work
                yield return new Token(TokenType.Newline);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("_calico_frame");
                yield return new Token(TokenType.OpAssign);
                yield return new ConstantToken(new IntVariant(0));
                yield return new Token(TokenType.Newline);
            }
            else if (_physicsProcessWaiter.Check(t) && _injectedGlobals)
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