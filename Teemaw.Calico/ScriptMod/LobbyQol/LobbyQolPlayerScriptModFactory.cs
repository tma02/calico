using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.LobbyQol;

public static class LobbyQolPlayerScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LobbyQolPlayerScriptMod")
            .Patching("res://Scenes/Entities/Player/player.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    func calico_update_username():
                    	var username = str(Network._get_username_from_id(owner_id))
                    	title.label = username

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("update_username")
                .Matching(CreateFunctionDefinitionPattern("_setup_not_controlled"))
                .Do(Append)
                .With(
                    """

                    Network.connect("_members_updated", self, "calico_update_username")

                    """, 1
                )
            )
            .Build();
    }
}