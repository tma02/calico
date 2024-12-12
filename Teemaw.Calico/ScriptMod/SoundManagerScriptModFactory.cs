using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;
using static Teemaw.Calico.Util.WeaveUtil;

namespace Teemaw.Calico.ScriptMod;

public static class SoundManagerScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("SoundManagerScriptMod")
            .Patching("res://Scenes/Entities/Player/sound_manager.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    $"""

                     const CALICO_PERSIST = [{
                         // This is the first place we dynamically determine patch content based on state available to
                         // GDWeave. I'm not yet certain that this is the best pattern to follow moving forward.
                         new Func<string>(() => {
                             List<string> persistedPlayerNodes = ["dive_scrape", "reel_slow", "reel_fast"];
                             if (IsModLoaded(mod, "Sulayre.Lure"))
                             {
                                 // Lure expects bark_cat in the node tree as a template
                                 persistedPlayerNodes.Add("bark_cat");
                             }

                             var array = persistedPlayerNodes
                                 .Append("bark_cat")
                                 .Select(node => "\"" + node + "\"")
                                 .ToArray();

                             return string.Join(", ", array);
                         })()
                     }]
                     var calico_players = {/* Hack: {} can't be escaped in a raw string */"{}"}

                     func _ready():
                     	print("[calico] caching player sfx")
                     	for child in get_children():
                     		if (child is AudioStreamPlayer3D || child is AudioStreamPlayer) && !CALICO_PERSIST.has(child.name):
                     			calico_players[child.name] = child
                     			calico_players[child.name].connect("finished", self, "calico_remove_child", [child.name])
                     			remove_child(child)

                     func calico_remove_child(id):
                     	print("[calico] Cleaning up sfx ", id)
                     	remove_child(calico_players[id])

                     func calico_get_player_or_null(id):
                     	if !calico_players.has(id):
                     		return get_node_or_null(id)
                     	if calico_players[id].get_parent() == null:
                     		add_child(calico_players[id])
                     	return calico_players[id]

                     """))
            .AddRule(new TransformationRuleBuilder()
                .Named("get_node_or_null")
                .Matching(CreateGdSnippetPattern("var node = get_node_or_null"))
                .Do(ReplaceLast)
                .With(new IdentifierToken("calico_get_player_or_null")))
            .Build();
    }
}