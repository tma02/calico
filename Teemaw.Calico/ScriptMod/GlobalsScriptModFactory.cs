using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class GlobalsScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("GlobalsScriptMod")
            .Patching("res://Scenes/Singletons/globals.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    print("[calico] Setting physics FPS...")
                    Engine.set_iterations_per_second(30)

                    """, 1
                )
            )
            .Build();
    }
}