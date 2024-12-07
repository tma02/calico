using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using Teemaw.Calico.Util;
using static GDWeave.Godot.TokenType;
using static Teemaw.Calico.Util.WaiterDefinitions;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMods;

public class BushParticleDetectScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new CalicoScriptMod(mod, "BushParticleDetectScriptMod",
            "res://Scenes/Map/Props/bush_particle_detect.gdc", [
                new ScriptPatchDescriptor("globals", CreateGlobalsChecks(),
                    """

                    var calico_player

                    func _ready():
                    	calico_player = $AudioStreamPlayer3D
                    	calico_player.connect("finished", self, "remove_child", [calico_player])
                    	remove_child(calico_player)

                    """),
                new ScriptPatchDescriptor("play", [
                    t => t.Type is Dollar,
                    t => t is IdentifierToken { Name: "AudioStreamPlayer3D" },
                    t => t.Type is Period,
                    t => t is IdentifierToken { Name: "play" },
                    t => t.Type is ParenthesisOpen,
                    t => t.Type is ParenthesisClose,
                ], ScriptTokenizer.Tokenize(
                    """
                    add_child(calico_player)
                    calico_player.play()
                    """, 2), PatchOperation.ReplaceAll),
            ]);
    }
}