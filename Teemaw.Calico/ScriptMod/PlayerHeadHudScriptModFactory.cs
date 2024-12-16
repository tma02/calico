using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class PlayerHeadHudScriptModFactory
{
    public static IScriptMod Create(IModInterface mod, Config config)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("PlayerHeadHudScriptMod")
            .Patching("res://Scenes/Entities/Player/player_headhud.gdc")
            .When(() => config.SmoothCameraEnabled)
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    func calico_setup(new_parent, new_offset):
                    	parent = new_parent
                    	offset = new_offset

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    // The tokenizer doesn't handle number literals at the end of a line.
                    """

                    process_priority = -1 + get_parent().process_priority

                    """, 1
                )
            )
            .Build();
    }
}