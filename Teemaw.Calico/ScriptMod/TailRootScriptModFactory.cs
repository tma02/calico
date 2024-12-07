using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class TailRootScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("TailRootScriptMod")
            .Patching("res://Scenes/Entities/Player/tail_root.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    func _process(delta):
                    	swish.global_transform.origin = rot.global_transform.origin

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    process_priority = 1 + get_parent().process_priority

                    """, 1
                )
            )
            .Build();
    }
}