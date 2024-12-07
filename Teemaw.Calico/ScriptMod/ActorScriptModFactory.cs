using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class ActorScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("ActorScriptMod")
            .Patching("res://Scenes/Entities/actor.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("network_share_signal_gate")
                .Matching(CreateGdSnippetPattern("Network.connect(\"_network_tick\", self, \"_network_share\")"))
                .Do(ReplaceAll)
                .With(
                    """

                    if controlled:
                    	Network.connect("_network_tick", self, "_network_share")

                    """, 1))
            .Build();
    }
}