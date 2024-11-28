using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class PlayerHeadHudScriptMod(IModInterface mod): IScriptMod
{
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        // Note the tabs for indent tokenization
        """
        
        func calico_setup(new_parent, new_offset):
        	parent = new_parent
        	offset = new_offset
        
        """);
    
    private static readonly IEnumerable<Token> OnReady = ScriptTokenizer.Tokenize(
        // (1) is a hack to prevent the int from being tokenized as a string
        """

        process_priority = get_parent().process_priority - (1)
        
        """, 1);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/player_headhud.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter extendsWaiter = new([
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ]);
        MultiTokenWaiter readyWaiter = new([
            t => t.Type is PrFunction,
            t => t is IdentifierToken { Name: "_ready" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);
        
        mod.Logger.Information($"[calico.PlayerHeadHudScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["globals"] = false,
            ["ready"] = false,
        };
        
        foreach (var t in tokens)
        {
            if (extendsWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in Globals) yield return t1;
                patchFlags["globals"] = true;
                mod.Logger.Information("[calico.PlayerHeadHudScriptMod] globals patch OK");
            }
            else if (readyWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in OnReady)
                    yield return t1;
                patchFlags["ready"] = true;
                mod.Logger.Information("[calico.PlayerHeadHudScriptMod] _ready patch OK");
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
                mod.Logger.Error($"[calico.PlayerHeadHudScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}