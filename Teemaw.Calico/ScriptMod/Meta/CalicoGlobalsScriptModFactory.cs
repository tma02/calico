using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.GracefulDegradation.ModConflictCatalog;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.Meta;

public static class CalicoGlobalsScriptModFactory
{
    public static IScriptMod Create(IModInterface mi)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mi)
            .Named("CalicoGlobalsScriptMod")
            .Patching("res://Scenes/Singletons/globals.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("version")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    $"""

                     print("[calico] {CalicoMod.GetAssemblyVersion()}")

                     """, 1
                )
            )
            .Build();
    }
}