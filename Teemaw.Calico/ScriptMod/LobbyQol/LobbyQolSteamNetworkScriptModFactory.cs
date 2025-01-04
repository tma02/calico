using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod.LobbyQol;

public static class LobbyQolSteamNetworkScriptModFactory
{
    public static IScriptMod Create(IModInterface mod, Config config)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("LobbyQolSteamNetworkScriptMod")
            .Patching("res://Scenes/Singletons/SteamNetwork.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("globals")
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var CALICO_LOBBY_ID = ""
                    const CALICO_BASE36_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"

                    func calico_decimal_to_base36(decimal_num):
                    	if decimal_num == 0:
                    		return "0"
                    	
                    	var result = ""
                    	var num = int(decimal_num)
                    	
                    	while num > 0:
                    		var remainder = num % 36
                    		result = CALICO_BASE36_CHARS[remainder] + result
                    		num = num / 36
                    	
                    	var formatted = ""
                    	var count = 0
                    	for i in range(result.length()):
                    		if count > 0 && count % 3 == 0 && result[i] != "-":
                    			formatted += "-"
                    		formatted += result[i]
                    		count += 1
                    	
                    	return formatted

                    func calico_base36_to_decimal(base36_str):
                    	var result = 0
                    	var power = 0
                    	
                    	base36_str = base36_str.replace("-", "")
                    	
                    	for i in range(base36_str.length()):
                    		var char = base36_str[i].to_upper()
                    		var value = CALICO_BASE36_CHARS.find(char)
                    		
                    		if value == -1:
                    			push_error("Invalid base36 character: " + char)
                    			return 0
                    		
                    		result = result * 36 + value
                    	
                    	return result

                    func calico_persona_state_change(steam_id, flags):
                    	_get_lobby_members(false)
                    	
                    var calico_mods = []
                    signal calico_mod_updatemods

                    func calico_get_safe_username_from_id(user_id):
                    	var username = _get_username_from_id(user_id)
                    	username = username.replace("[", "")
                    	username = username.replace("]", "")
                    	return username

                    func calico_host_share_mods():
                    	for user_id in calico_mods:
                    		_send_P2P_Packet({"type": "^^calico_mod_addmod", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_host_share_bans():
                    	for mod_id in calico_mods:
                    		for banned_id in WEB_LOBBY_REJECTS:
                    			_send_P2P_Packet({"type": "^^calico_mod_addmod", "user_id": banned_id}, str(mod_id), 2, CHANNELS.GAME_STATE)

                    func calico_add_mod(user_id):
                    	if calico_mods.has(user_id): return
                    	calico_mods.append(user_id)
                    	emit_signal("calico_mod_updatemods")

                    func calico_host_add_mod(user_id):
                    	calico_add_mod(user_id)
                    	_send_P2P_Packet({"type": "^^calico_mod_addmod", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_remove_mod(user_id):
                    	if !calico_mods.has(user_id): return
                    	calico_mods.erase(user_id)
                    	emit_signal("calico_mod_updatemods")

                    func calico_host_remove_mod(user_id):
                    	calico_remove_mod(user_id)
                    	_send_P2P_Packet({"type": "^^calico_mod_removemod", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_is_mod(user_id):
                    	return user_id == KNOWN_GAME_MASTER || calico_mods.has(user_id)

                    func calico_remote_kick(user_id):
                    	if calico_is_mod(STEAM_ID):
                    		_send_P2P_Packet({"type": "^^calico_mod_kick", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_remote_ban(user_id):
                    	if calico_is_mod(STEAM_ID):
                    		_send_P2P_Packet({"type": "^^calico_mod_ban", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_remote_unban(user_id):
                    	if calico_is_mod(STEAM_ID):
                    		_send_P2P_Packet({"type": "^^calico_mod_unban", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_share_ban(user_id):
                    	if calico_is_mod(STEAM_ID):
                    		_send_P2P_Packet({"type": "^^calico_mod_ban", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    func calico_share_unban(user_id):
                    	if calico_is_mod(STEAM_ID):
                    		_send_P2P_Packet({"type": "^^calico_mod_unban", "user_id": user_id}, "all", 2, CHANNELS.GAME_STATE)

                    """
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("ready")
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    Steam.connect("persona_state_change", self, "calico_persona_state_change")

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("code_lobby_created")
                .Matching(CreateGdSnippetPattern("LOBBY_CODE = code"))
                .Do(Append)
                .With(
                    """

                    CALICO_LOBBY_ID = calico_decimal_to_base36(int(lobby_id))
                    print(CALICO_LOBBY_ID)

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("code_lobby_joined")
                .Matching(CreateGdSnippetPattern("LOBBY_CODE = Steam.getLobbyData(lobby_id, \"code\")"))
                .Do(Append)
                .With(
                    """

                    CALICO_LOBBY_ID = calico_decimal_to_base36(lobby_id)
                    print(CALICO_LOBBY_ID)

                    """, 2
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("search_for_lobby_decode")
                .ScopedTo(CreateFunctionDefinitionPattern("_search_for_lobby", ["code"]))
                .Matching(CreateGdSnippetPattern("code = code.to_upper()"))
                .Do(Append)
                .With(
                    """

                    var calicode = code
                    var use_calicode = false
                    if code.count("-") == 3:
                    	calicode = calico_base36_to_decimal(code)
                    	use_calicode = true
                    	print("[calico] Calicode decoded as ", calicode)

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("search_for_lobby_skip_code")
                .ScopedTo(CreateFunctionDefinitionPattern("_search_for_lobby", ["code"]))
                .Matching(CreateGdSnippetPattern("if sanitized_list.size() > 0:"))
                .Do(ReplaceAll)
                .With("if sanitized_list.size() > 0 && !use_calicode:")
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("search_for_lobby_calicode")
                .ScopedTo(CreateFunctionDefinitionPattern("_search_for_lobby", ["code"]))
                .Matching(CreateGdSnippetPattern("if lobby_found == - 1:"))
                .Do(ReplaceAll)
                .With(
                    """

                    if sanitized_list.size() == 0:
                    	print("[calico] game could not find lobby, trying Calicode")
                    	var LOBBY_PLAYERS = Steam.getNumLobbyMembers(calicode)
                    	var LOBBY_MAX_PLAYERS = Steam.getLobbyData(calicode, "cap")
                    	var LOBBY_VERSION = Steam.getLobbyData(calicode, "version")
                    	
                    	print(LOBBY_PLAYERS)
                    	print(LOBBY_MAX_PLAYERS)
                    	print(LOBBY_VERSION)
                    	
                    	lobby_found = calicode
                    	if LOBBY_PLAYERS >= int(LOBBY_MAX_PLAYERS): lobby_found = - 2
                    	if str(LOBBY_VERSION) != str(Globals.GAME_VERSION): lobby_found = - 3
                    	if LOBBY_PLAYERS == 0: lobby_found = -1

                    if lobby_found == - 1:

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("moderator_packets")
                .ScopedTo(config.MultiThreadNetworkingEnabled
                    ? CreateGdSnippetPattern("func _calico_process_P2P_packet_on_main(packet):")
                    : CreateGdSnippetPattern("func _read_P2P_Packet(message_data = {}):"))
                .Matching(CreateGdSnippetPattern("match type:"))
                .Do(Append)
                .With(
                    """

                    "^^calico_mod_kick":
                    	if calico_is_mod(PACKET_SENDER):
                    		_update_chat(calico_get_safe_username_from_id(PACKET_SENDER) + " is kicking " + calico_get_safe_username_from_id(DATA["user_id"]))
                    	if !GAME_MASTER: return
                    	if calico_is_mod(PACKET_SENDER):
                    		_kick_player(DATA["user_id"])
                    "^^calico_mod_ban":
                    	if calico_is_mod(PACKET_SENDER):
                    		_update_chat(calico_get_safe_username_from_id(PACKET_SENDER) + " is banning " + calico_get_safe_username_from_id(DATA["user_id"]))
                    	if !GAME_MASTER: return
                    	if calico_is_mod(PACKET_SENDER):
                    		_ban_player(DATA["user_id"])
                    "^^calico_mod_unban":
                    	if calico_is_mod(PACKET_SENDER):
                    		_update_chat(calico_get_safe_username_from_id(PACKET_SENDER) + " is unbanning " + calico_get_safe_username_from_id(DATA["user_id"]))
                    	if !GAME_MASTER: return
                    	if calico_is_mod(PACKET_SENDER):
                    		_unban_player(DATA["user_id"])

                    "^^calico_mod_addmod":
                    	if !from_host: return
                    	calico_add_mod(DATA["user_id"])
                    "^^calico_mod_removemod":
                    	if !from_host: return
                    	calico_remove_mod(DATA["user_id"])
                    "^^calico_mod_ban":
                    	if !from_host: return
                    	WEB_LOBBY_REJECTS.append(DATA["user_id"])
                    "^^calico_mod_unban":
                    	if !from_host: return
                    	WEB_LOBBY_REJECTS.erase(DATA["user_id"])
                    """, 3
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("moderation_share")
                .ScopedTo(config.MultiThreadNetworkingEnabled
                    ? CreateGdSnippetPattern("func _calico_process_P2P_packet_on_main(packet):")
                    : CreateGdSnippetPattern("func _read_P2P_Packet(message_data = {}):"))
                .Matching(CreateGdSnippetPattern("\"new_player_join\":"))
                .Do(Append)
                .With(
                    """

                    if GAME_MASTER:
                    	calico_host_share_mods()
                    	calico_host_share_bans()
                    
                    """, 2)
            )
            .Build();
    }
}