using GDWeave;
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

                    """
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
	            .Matching(CreateGdSnippetPattern("if lobbies.size() > 0:"))
	            .Do(ReplaceAll)
	            .With("if lobbies.size() > 0 && !use_calicode:")
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("search_for_lobby_calicode")
                .ScopedTo(CreateFunctionDefinitionPattern("_search_for_lobby", ["code"]))
                .Matching(CreateGdSnippetPattern("if lobby_found != - 1:"))
                .Do(ReplaceAll)
                .With(
                    """

                    if lobbies.size() == 0:
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

                    if lobby_found > - 1:

                    """, 1
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("send_p2p_packet_ignore_banned")
                .ScopedTo(CreateGdSnippetPattern(
                    config.MultiThreadNetworkingEnabled
                        ? "func _calico_send_P2P_packet_on_thread(packet):"
                        : "func _send_P2P_Packet(packet_data, target = \"all\", type = 0, channel = 0):"))
                .Matching(CreateGdSnippetPattern(
                    """
                    for MEMBER in LOBBY_MEMBERS:
                    	Steam.sendP2PPacket(MEMBER["steam_id"], PACKET_DATA, SEND_TYPE, CHANNEL)
                    """, 2
                ))
                .Do(ReplaceAll)
                .With(
                    """
                    for MEMBER in LOBBY_MEMBERS:
                    	if !FORCE_DISCONNECT_PLAYERS.has(MEMBER["steam_id"]):
                    		Steam.sendP2PPacket(MEMBER["steam_id"], PACKET_DATA, SEND_TYPE, CHANNEL)
                    """, 2
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("lobby_chat_update_join")
                .ScopedTo(CreateFunctionDefinitionPattern("_on_Lobby_Chat_Update",
                    ["lobby_id", "changed_id", "making_change_id", "chat_state"]))
                .Matching(CreateGdSnippetPattern(
                    """
                    _delayed_chat_update_message(making_change_id, "%u joined the game.", 1.5)
                    emit_signal("_user_connected", making_change_id)
                    """, 2
                ))
                .Do(ReplaceAll)
                .With(
                    """

                    if !FORCE_DISCONNECT_PLAYERS.has(int(making_change_id)):
                    	_delayed_chat_update_message(making_change_id, "%u joined the game.", 1.5)
                    	emit_signal("_user_connected", making_change_id)

                    """, 2
                )
            )
            .AddRule(new TransformationRuleBuilder()
                .Named("lobby_chat_update_leave")
                .ScopedTo(CreateFunctionDefinitionPattern("_on_Lobby_Chat_Update",
                    ["lobby_id", "changed_id", "making_change_id", "chat_state"]))
                .Matching(CreateGdSnippetPattern(
                    """
                    _recieve_safe_message(making_change_id, "ffeed5", "%u left the game.", false)
                    emit_signal("_user_disconnected", making_change_id)

                    Steam.closeP2PSessionWithUser(making_change_id)
                    """, 2
                ))
                .Do(ReplaceAll)
                .With(
                    """

                    if Steam.closeP2PSessionWithUser(making_change_id):
                    	_recieve_safe_message(making_change_id, "ffeed5", "%u left the game.", false)
                    	emit_signal("_user_disconnected", making_change_id)

                    """, 2
                )
            )
            .Build();
    }
}