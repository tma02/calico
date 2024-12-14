using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static GDWeave.Godot.TokenType;
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
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var calico_most_players
                    
                    func calico_sort_lobbies_desc(a, b):
                    	return Steam.getNumLobbyMembers(a) > Steam.getNumLobbyMembers(b)
                    
                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    code.max_length = 14
                    calico_most_players = $"%hidenames".duplicate()
                    calico_most_players.text = "MOST USERS"
                    var calico_original_label = $"%hidenames".get_parent().get_node("Label")
                    calico_original_label.text = "Filters"
                    var calico_tt = calico_original_label.get_node("TooltipNode").duplicate()
                    calico_tt.header = "Calico: Most Users"
                    calico_tt.body = "Sorts the lobby list by user count."
                    calico_most_players.add_child(calico_tt)
                    $"%hidenames".get_parent().add_child(calico_most_players)
                    var calico_lobby_scroll_bar = $lobby_browser/Panel/Panel2/ScrollContainer/_v_scroll
                    calico_lobby_scroll_bar.get_stylebox("grabber").content_margin_top = 20

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("lobby_list")
                .Matching(CreateFunctionDefinitionPattern("_lobby_list_returned", ["lobbies"]))
                .Do(Append)
                .With(
                    """
                    
                    if calico_most_players.pressed:
                    	lobbies.sort_custom(self, "calico_sort_lobbies_desc")

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("lobby_list_cap")
                .Matching(CreateGdSnippetPattern("if created_lobbies.has(lobby): return"))
                .Do(ReplaceLast)
                .With(new Token(CfContinue))
            )
            .Build();
    }
}