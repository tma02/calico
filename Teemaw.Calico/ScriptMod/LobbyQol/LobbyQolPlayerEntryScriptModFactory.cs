using GDWeave;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.LobbyQol;

public static class LobbyQolPlayerEntryScriptModFactory
{
    public static IScriptMod Create(IModInterface mod)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LobbyQolPlayerEntryScriptMod")
            .Patching("res://Scenes/HUD/Playerlist/player_entry.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var calico_mod_button

                    func calico_updatemods():
                    	calico_update_mod_label()
                    	calico_update_mod_button()

                    func calico_update_mod_label():
                    	if held_data["steam_id"] == Network.KNOWN_GAME_MASTER:
                    		player_name.text = "[host] " + str(held_data["steam_name"])
                    	else if Network.calico_is_mod(held_data["steam_id"]):
                    		player_name.text = "[mod] " + str(held_data["steam_name"])
                    	else:
                    		player_name.text = str(held_data["steam_name"])
                    
                    func calico_update_mod_button():
                    	if Network.calico_is_mod(held_data["steam_id"]):
                    		calico_mod_button.text = "-M"
                    		calico_mod_button.get_node("TooltipNode4").header = "Remove Moderator"
                    		calico_mod_button.get_node("TooltipNode4").body = "Revokes this player's Calico moderation permissions. (The [mod] tag is only visible to other Calico users.)"
                    	else:
                    		calico_mod_button.text = "+M"
                    		calico_mod_button.get_node("TooltipNode4").header = "Give Moderator"
                    		calico_mod_button.get_node("TooltipNode4").body = "If this player has Calico installed, grants this player moderation permissions."

                    func calico_on_mod_pressed():
                    	if !Network.GAME_MASTER:
                    		return
                    	if Network.calico_is_mod(held_data["steam_id"]):
                    		Network.calico_host_remove_mod(held_data["steam_id"])
                    	else:
                    		Network.calico_host_add_mod(held_data["steam_id"])

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("kick_ban_buttons")
                .Matching(CreateGdSnippetPattern(
                    """
                    func _on_kick_pressed(): if Network.GAME_MASTER: Network._kick_player(held_data["steam_id"])
                    func _on_ban_pressed(): if Network.GAME_MASTER: Network._ban_player(held_data["steam_id"])
                    """
                ))
                .Do(ReplaceAll)
                .With(
                    """

                    func _on_kick_pressed():
                    	if Network.GAME_MASTER:
                    		Network._kick_player(held_data["steam_id"])
                    	else: 
                    		Network.calico_remote_kick(held_data["steam_id"])
                    func _on_ban_pressed():
                    	if Network.GAME_MASTER:
                    		Network._ban_player(held_data["steam_id"])
                    	else: 
                    		Network.calico_remote_ban(held_data["steam_id"])

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("setup")
                .ScopedTo(CreateGdSnippetPattern("func _setup(data, type = 0):"))
                .Matching(CreateGdSnippetPattern(
                    """
                    $Panel / HBoxContainer / member / kick.disabled = !Network.GAME_MASTER || data["steam_id"] == Network.STEAM_ID
                    $Panel / HBoxContainer / member / ban.disabled = !Network.GAME_MASTER || data["steam_id"] == Network.STEAM_ID
                    """, 1
                ))
                .Do(ReplaceAll)
                .With(
                    """

                    var calico_is_mod = Network.calico_is_mod(data["steam_id"])
                    $Panel / HBoxContainer / member / kick.disabled = !(Network.GAME_MASTER || calico_is_mod) || data["steam_id"] == Network.STEAM_ID
                    $Panel / HBoxContainer / member / ban.disabled = !(Network.GAME_MASTER || calico_is_mod) || data["steam_id"] == Network.STEAM_ID
                    if type == 0 && Network.calico_is_mod(data["steam_id"]) && data["steam_id"] != Network.KNOWN_GAME_MASTER: player_name.text = "[mod] " + str(data["steam_name"])
                    calico_mod_button = $"%member".get_node("ban").duplicate()
                    calico_mod_button.icon = null
                    calico_mod_button.disconnect("pressed", self, "_on_ban_pressed")
                    calico_mod_button.disabled = !Network.GAME_MASTER || data["steam_id"] == Network.STEAM_ID
                    calico_update_mod_button()
                    $"%member".add_child(calico_mod_button)
                    calico_mod_button.connect("pressed", self, "calico_on_mod_pressed")
                    Network.connect("calico_mod_updatemods", self, "calico_updatemods")

                    """, 1
                )
            )
            .Build();
    }
}