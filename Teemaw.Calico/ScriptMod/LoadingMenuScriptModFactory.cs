using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public class LoadingMenuScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LoadingMenuScriptMod")
            .Patching("res://Scenes/Menus/Loading Menu/loading_menu.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var calico_ticks = 0
                    var calico_epsilon = 0

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("epsilon_increment")
                .Matching(CreateFunctionDefinitionPattern("_on_Timer_timeout"))
                .Do(Append)
                .With(
                    """

                    calico_ticks += 1
                    if calico_ticks >= 4:
                    	calico_ticks = 0
                    	if Network.LOBBY_MEMBERS.size() - calico_epsilon > 0:
                    		calico_epsilon += 1

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("join_guard")
                .Matching(CreateGdSnippetPattern("if packets_recieved >= 6 or packets_recieved >= (Network.LOBBY_MEMBERS.size() - 1):"))
                .Do(ReplaceAll)
                .With("if packets_recieved >= 6 or packets_recieved >= (Network.LOBBY_MEMBERS.size() - calico_epsilon):")
            )
            .Build();
    }
}