using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.LobbyQol;

public static class LobbyQolPlayerlistScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LobbyQolPlayerlistScriptMod")
            .Patching("res://Scenes/HUD/Playerlist/playerlist.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("visibility")
                .Matching(CreateGdSnippetPattern(
                    """
                    banlist.visible = Network.GAME_MASTER
                    banlabel.visible = Network.GAME_MASTER
                    bansep.visible = Network.GAME_MASTER
                    no_ban.visible = Network.GAME_MASTER && Network.WEB_LOBBY_REJECTS.size() <= 0
                    """, 1
                ))
                .Do(ReplaceAll)
                .With(
                    """

                    calico_updatemods()

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """
                    
                    Network.connect("calico_mod_updatemods", self, "calico_updatemods")
                    
                    """, 1
                )
                .Build()
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """
                    func calico_updatemods():
                    	var calico_show_banlist = Network.GAME_MASTER || Network.calico_is_mod(Network.STEAM_ID)
                    	banlist.visible = calico_show_banlist
                    	banlabel.visible = calico_show_banlist
                    	bansep.visible = calico_show_banlist
                    	no_ban.visible = calico_show_banlist && Network.WEB_LOBBY_REJECTS.size() <= 0
                    """
                )
                .Build()
            )
            .Build();
    }
}