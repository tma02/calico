using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class TransitionZoneScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("TransitionZoneScriptMod")
            .Patching("res://Scenes/Map/Tools/transition_zone.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("get_tree")
                .Matching(CreateGdSnippetPattern("yield (get_tree"))
                .Do(ReplaceAll)
                .With("yield (actor.get_tree")
            )
            .Build();
    }
}