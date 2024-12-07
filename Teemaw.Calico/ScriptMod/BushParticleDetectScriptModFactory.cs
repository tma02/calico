using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class BushParticleDetectScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("BushParticleDetectScriptMod")
            .Patching("res://Scenes/Map/Props/bush_particle_detect.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var calico_player

                    func _ready():
                    	calico_player = $AudioStreamPlayer3D
                    	calico_player.connect("finished", self, "remove_child", [calico_player])
                    	remove_child(calico_player)

                    """))
            .AddRule(new TransformationRuleBuilder()
                .Named("play")
                .Matching(CreateGdSnippetPattern("$AudioStreamPlayer3D.play()"))
                .Do(ReplaceAll)
                .With(
                    """
                    add_child(calico_player)
                    calico_player.play()
                    """, indent: 2
                ))
            .Build();
    }
}