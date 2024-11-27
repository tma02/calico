using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class GuitarStringSoundScriptMod(IModInterface mod) : IScriptMod
{
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        """

        var calico_playing_count = 0

        """);

    private static readonly IEnumerable<Token> CallGuard = ScriptTokenizer.Tokenize(
        """

        if calico_playing_count == 0: return

        """, 1);

    private static readonly IEnumerable<Token> IncrementPlayingCount = ScriptTokenizer.Tokenize(
        """

        calico_playing_count += 1

        """, 3);

    private static readonly IEnumerable<Token> DecrementPlayingCount = ScriptTokenizer.Tokenize(
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
            t => t.Type is ParenthesisClose
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
            t => t is ConstantToken c && c.Value.Equals(new BoolVariant(false))
        ]);

        mod.Logger.Information($"[calico.GuitarStringSoundScriptMod] Patching {path}");

        var patchFlags = new Dictionary<string, bool>
        {
            ["globals"] = false,
            ["player"] = false,
            ["call_guard"] = false,
            ["node_stop"] = false
        };

        foreach (var t in tokens)
        {
            if (extendsWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in Globals)
                    yield return t1;
                patchFlags["globals"] = true;
                mod.Logger.Information("[calico.GuitarStringSoundScriptMod] globals patch OK");
            }
            else if (nodePlayWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in IncrementPlayingCount)
                    yield return t1;
                patchFlags["player"] = true;
                mod.Logger.Information("[calico.GuitarStringSoundScriptMod] player patch OK");
            }
            else if (callWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in CallGuard)
                    yield return t1;
                patchFlags["call_guard"] = true;
                mod.Logger.Information("[calico.GuitarStringSoundScriptMod] call guard patch OK");
            }
            else if (nodeStopWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in DecrementPlayingCount)
                    yield return t1;
                patchFlags["node_stop"] = true;
                mod.Logger.Information("[calico.GuitarStringSoundScriptMod] node stop patch OK");
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
                mod.Logger.Error($"[calico.GuitarStringSoundScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}