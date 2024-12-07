using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.Util;
using static Teemaw.Calico.Util.WaiterDefinitions;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMods;

public static class BushParticleDetectScriptModFactory
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
                new ScriptPatchDescriptor("play", CreateSnippetChecks("$AudioStreamPlayer3D.play()"),
                    ScriptTokenizer.Tokenize(
                        """
                        add_child(calico_player)
                        calico_player.play()
                        """, 2), PatchOperation.ReplaceAll),
            ]);
    }
}