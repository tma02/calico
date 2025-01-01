using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.LobbyQol;

public static class LobbyQolMainMenuScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LobbyQolMainMenuScriptMod")
            .Patching("res://Scenes/Menus/Main Menu/main_menu.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    code.max_length = 14
                    
                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("lobby_player_count")
                .ScopedTo(CreateFunctionDefinitionPattern("_lobby_list_returned", ["lobbies"]))
                .Matching(CreateGdSnippetPattern("var lobby_num_members = Steam.getLobbyData(lobby, \"count\")"))
                .Do(Append)
                .With(
                    """
                    
                    lobby_num_members = min(lobby_num_members, Steam.getNumLobbyMembers(lobby))

                    """, 2
                )
            )
            .Build();
    }
}