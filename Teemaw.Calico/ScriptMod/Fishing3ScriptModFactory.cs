using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class Fishing3ScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("Fishing3ScriptMod")
            .Patching("res://Scenes/Minigames/Fishing3/fishing3.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("main_progress")
                .Matching(CreateGdSnippetPattern("main_progress += reel_speed"))
                .Do(ReplaceAll)
                .With("main_progress += 2 * reel_speed")
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("bad_progress")
                .Matching(CreateGdSnippetPattern("bad_progress += bad_speed"))
                .Do(ReplaceAll)
                .With("bad_progress += 2 * bad_speed")
            )
            .Build();
    }
}