using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class TailRootScriptMod(IModInterface mod): IScriptMod
{
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        // Note the tabs for indent tokenization
        """
        
        func _process(delta):
        	swish.global_transform.origin = rot.global_transform.origin
        
        """);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/tail_root.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter extendsWaiter = new([
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ]);
        
        mod.Logger.Information($"[calico.TailRootScriptMod] Patching {path}");
        
        var patchFlags = new Dictionary<string, bool>
        {
            ["globals"] = false,
        };
        
        foreach (var t in tokens)
        {
            if (extendsWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in Globals) yield return t1;
                patchFlags["globals"] = true;
                mod.Logger.Information("[calico.TailRootScriptMod] globals patch OK");
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
                mod.Logger.Error($"[calico.TailRootScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}