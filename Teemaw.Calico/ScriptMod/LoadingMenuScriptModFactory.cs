using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static GDWeave.Godot.TokenType;
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
                // Using an array here since `- 1` is not tokenized correctly yet.
                .Matching([
                    t => t is IdentifierToken { Name: "Network" },
                    t => t.Type is Period,
                    t => t is IdentifierToken { Name: "LOBBY_MEMBERS" },
                    t => t.Type is Period,
                    t => t is IdentifierToken { Name: "size" },
                    t => t.Type is ParenthesisOpen,
                    t => t.Type is ParenthesisClose,
                    t => t.Type is OpSub,
                    t => t is ConstantToken constant && constant.Value.Equals(new IntVariant(1)),
                ])
                .Do(ReplaceLast)
                .With(new IdentifierToken("calico_epsilon"))
            )
            .Build();
    }
}