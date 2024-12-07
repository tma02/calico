using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using Teemaw.Calico.Util;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMod;

public static class BushParticleDetectScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptMod(mod, "BushParticleDetectScriptMod",
            "res://Scenes/Map/Props/bush_particle_detect.gdc", [
                new TransformationRule("globals", CreateGlobalsPattern(),
                    """

                    var calico_player

                    func _ready():
                    	calico_player = $AudioStreamPlayer3D
                    	calico_player.connect("finished", self, "remove_child", [calico_player])
                    	remove_child(calico_player)

                    """),
                new TransformationRule("play", CreateGdSnippetPattern("$AudioStreamPlayer3D.play()"),
                    ScriptTokenizer.Tokenize(
                        """
                        add_child(calico_player)
                        calico_player.play()
                        """, 2), Operation.ReplaceAll),
            ]);
    }
}