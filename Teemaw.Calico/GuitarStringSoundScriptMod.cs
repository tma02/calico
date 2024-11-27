using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class GuitarStringSoundScriptMod(IModInterface mod): IScriptMod
{
    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        var calico_playing_count = 0

        """);
    
    private readonly IEnumerable<Token> _callGuard = ScriptTokenizer.Tokenize(
        """

        if calico_playing_count == 0: return

        """, 1);
    
    private readonly IEnumerable<Token> _incrementPlayingCount = ScriptTokenizer.Tokenize(
        """

        calico_playing_count += 1

        """, 3);
    
    private readonly IEnumerable<Token> _decrementPlayingCount = ScriptTokenizer.Tokenize(
        """

        calico_playing_count -= 1

        """, 3);

    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/guitar_string_sound.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter extendsWaiter = new([
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ]);
        MultiTokenWaiter nodePlayWaiter = new([
            t => t is IdentifierToken { Name: "node" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "play" },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "point" },
            t => t.Type is ParenthesisClose,
        ]);
        MultiTokenWaiter callWaiter = new([
            t => t.Type is PrFunction,
            t => t is IdentifierToken { Name: "_call" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);
        MultiTokenWaiter nodeStopWaiter = new([
            t => t is IdentifierToken { Name: "sound" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "playing" },
            t => t.Type is OpAssign,
            t => t is ConstantToken c && c.Value.Equals(new BoolVariant(false)),
            t => t.Type is ParenthesisClose,
        ]);
        
        mod.Logger.Information($"[calico.GuitarStringSoundScriptMod] Patching {path}");

        foreach (var t in tokens)
        {
            if (extendsWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _globals)
                    yield return t1;
            }
            else if (nodePlayWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _incrementPlayingCount)
                    yield return t1;
            }
            else if (callWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _callGuard)
                    yield return t1;
            }
            else if (nodeStopWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _decrementPlayingCount)
                    yield return t1;
            }
            else
            {
                yield return t;
            }
        }
    }
}