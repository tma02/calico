using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class HeldItemScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("HeldItemScriptMod")
            .Patching("res://Scenes/Entities/Player/held_item.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("physics_process")
                .Matching(CreateFunctionDefinitionPattern("_physics_process", ["delta"]))
                .Do(Append)
                .With(
                    """
                    
                    return
                    """, 1
                )
            )
            .Build();
    }
}