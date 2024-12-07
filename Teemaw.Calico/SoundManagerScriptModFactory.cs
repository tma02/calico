using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using Teemaw.Calico.Util;
using static GDWeave.Godot.TokenType;
using static Teemaw.Calico.Util.WaiterDefinitions;

namespace Teemaw.Calico;

public static class SoundManagerScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new CalicoScriptMod(mod, "SoundManagerScriptMod", "res://Scenes/Entities/Player/sound_manager.gdc", [
            new ScriptPatchDescriptor("globals", CreateGlobalsChecks(),
                """

                const CALICO_PERSIST = ["dive_scrape", "reel_slow", "reel_fast"]
                var calico_players = {}

                func _ready():
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

                """),
            new ScriptPatchDescriptor("get_node_or_null", [
                t => t.Type is PrVar,
                t => t is IdentifierToken { Name: "node" },
                t => t.Type is OpAssign,
                t => t is IdentifierToken { Name: "get_node_or_null" },
            ], [new IdentifierToken("calico_get_player_or_null")], PatchType.ReplaceFinal),
        ]);
    }
}