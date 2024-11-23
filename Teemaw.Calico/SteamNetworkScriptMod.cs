using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;

namespace Teemaw.Calico;

public class SteamNetworkScriptMod(IModInterface mod) : IScriptMod
{
    private readonly MultiTokenWaiter _extendsWaiter = new([
        t => t.Type is TokenType.PrExtends,
        t => t.Type is TokenType.Identifier,
        t => t.Type is TokenType.Newline
    ]);

    private readonly MultiTokenWaiter _readyWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_ready" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t.Type is TokenType.ParenthesisClose,
        t => t.Type is TokenType.Colon,
    ]);

    private readonly MultiTokenWaiter _processWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_process" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t is IdentifierToken { Name: "delta" },
        t => t.Type is TokenType.ParenthesisClose,
        t => t is IdentifierToken { Name: "run_callbacks" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t.Type is TokenType.ParenthesisClose,
    ], allowPartialMatch: true);

    private readonly MultiTokenWaiter _packetFlushStartWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_packet_flush" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t.Type is TokenType.ParenthesisClose,
        t => t.Type is TokenType.Colon
    ]);

    private readonly MultiTokenWaiter _packetFlushEndWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_packet_flush" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t.Type is TokenType.ParenthesisClose,
        t => t.Type is TokenType.Colon,
        t => t is IdentifierToken { Name: "FLUSH_PACKET_INFORMATION" },
        t => t.Type is TokenType.OpAssign,
        t => t.Type is TokenType.Newline
    ], allowPartialMatch: true);

    // A bit sketchy since we're trying to match over so many tokens.
    private readonly MultiTokenWaiter _actuallyHandlePacketWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_read_P2P_Packet" },
        t => t.Type is TokenType.ParenthesisOpen,
        // ...
        t => t is IdentifierToken { Name: "readP2PPacket" },
        // ...
        t => t is IdentifierToken { Name: "FLUSH_PACKET_INFORMATION" },
        // ...
        t => t.Type is TokenType.OpAssignAdd,
        // ...
        t => t.Type is TokenType.Newline
    ], allowPartialMatch: true);

    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        var NET_MUTEX
        var NET_THREAD
        var ACTOR_UPDATE_STATE = {}
        var PLAYER_UPDATE_COUNTER = {}
        var PLAYER_ANIMATION_STATE = {}
        var NET_THREAD_RUN = true
        var PACKET_QUEUE = []

        func _exit_tree():
        	NET_THREAD_RUN = false
        	print("[calico] Waiting for network thread to finish...")
        	NET_THREAD.wait_to_finish()
        	
        func _calico_net_thread():
        	while NET_THREAD_RUN:
        		NET_MUTEX.lock()
        		_calico_net_process(0)
        		NET_MUTEX.unlock()
        		OS.delay_msec(62 - get_ticks_msec() % 62)

        """);

    private readonly IEnumerable<Token> _onReady = ScriptTokenizer.Tokenize(
        """

        if !STEAM_ENABLED: return
        NET_MUTEX = Mutex.new()
        NET_THREAD = Thread.new()
        print("[calico] Starting network thread...")
        NET_THREAD.start(self, "_calico_net_thread")

        """, 1);

    private readonly IEnumerable<Token> _onProcess = ScriptTokenizer.Tokenize(
        // Note the tab character instead of spaces. This is required by the tokenizer.
        """

        NET_MUTEX.lock()
        for packet in PACKET_QUEUE:
        	_calico_process_P2P_packet_on_main(packet)
        PACKET_QUEUE.clear()
        NET_MUTEX.unlock()

        """, 1);

    private readonly IEnumerable<Token> _onHandlePacket = ScriptTokenizer.Tokenize(
        """

        if type == "actor_animation_update":
        	if !_validate_packet_information(DATA, ["actor_id", "data"], [TYPE_INT, TYPE_ARRAY]): return
        	var delay = 6
        	if DATA["data"][32] == 5 || DATA["data"][32] == 6:
        		delay = 2
        	if DATA["data"][32] == 19:
        		delay = 0
        	if !PLAYER_UPDATE_COUNTER.keys().has(PACKET_SENDER):
        		PLAYER_UPDATE_COUNTER[PACKET_SENDER] = -1
        	PLAYER_UPDATE_COUNTER[PACKET_SENDER] += 1
        	if PLAYER_UPDATE_COUNTER[PACKET_SENDER] >= delay:
        		PLAYER_UPDATE_COUNTER[PACKET_SENDER] = 0
        	if PLAYER_UPDATE_COUNTER[PACKET_SENDER] != 0:
        		return
        if type == "actor_update":
        	if !_validate_packet_information(DATA, ["actor_id", "pos", "rot"], [TYPE_INT, TYPE_VECTOR3, TYPE_VECTOR3]): return
        	if !ACTOR_UPDATE_STATE.keys().has(DATA["actor_id"]):
        		ACTOR_UPDATE_STATE[DATA["actor_id"]] = { "pos": Vector3.ZERO, "rot": Vector3.ZERO, "count": -1 }
        	if ACTOR_UPDATE_STATE[DATA["actor_id"]].pos.distance_to(DATA["pos"]) < 0.01 && ACTOR_UPDATE_STATE[DATA["actor_id"]].rot.distance_to(DATA["rot"]) < 0.01:
        		return
        	ACTOR_UPDATE_STATE[DATA["actor_id"]].pos = DATA["pos"]
        	ACTOR_UPDATE_STATE[DATA["actor_id"]].rot = DATA["rot"]
        	ACTOR_UPDATE_STATE[DATA["actor_id"]].count += 1
        	if ACTOR_UPDATE_STATE[DATA["actor_id"]].count >= 2:
        		ACTOR_UPDATE_STATE[DATA["actor_id"]].count = 0
        	if ACTOR_UPDATE_STATE[DATA["actor_id"]].count != 0:
        		return
        PACKET_QUEUE.append({"PACKET_SIZE": PACKET_SIZE, "PACKET_SENDER": PACKET_SENDER, "DATA": DATA, "type": type, "from_host": from_host})

        """, 2);

    private readonly IEnumerable<Token> _networkThreadFunctionSignatureTokens = ScriptTokenizer.Tokenize(
        "func _calico_net_process(delta): ");

    private readonly IEnumerable<Token> _packetHandlerFunction = ScriptTokenizer.Tokenize(
        """
        func _calico_process_P2P_packet_on_main(packet):
        	var PACKET_SIZE = packet["PACKET_SIZE"]
        	var PACKET_SENDER = packet["PACKET_SENDER"]
        	var DATA = packet["DATA"]
        	var type = packet["type"]
        	var from_host = packet["from_host"]
        	if true:
        		
        """);

    private readonly IEnumerable<Token> _lockMutex = ScriptTokenizer.Tokenize(
        """
        
        NET_MUTEX.lock()
        
        """, 1);

    private readonly IEnumerable<Token> _unlockMutex = ScriptTokenizer.Tokenize(
        """
        
        NET_MUTEX.unlock()
        
        """, 1);

    public bool ShouldRun(string path) => path == "res://Scenes/Singletons/SteamNetwork.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        foreach (var t in tokens)
        {
            if (_extendsWaiter.Check(t))
            {
                mod.Logger.Information(t.ToString());
                yield return t;

                // Insert our globals
                mod.Logger.Information(string.Join(", ", _globals));
                foreach (var t1 in _globals)
                    yield return t1;
                // TODO: figure out why these don't get declared as part of _globals...
                yield return new Token(TokenType.Newline);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("NET_MUTEX");
                yield return new Token(TokenType.Newline);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("NET_THREAD");
            }
            else if (_readyWaiter.Check(t))
            {
                yield return t;
                mod.Logger.Information(string.Join(", ", _onReady));
                foreach (var t1 in _onReady)
                    yield return t1;
            }
            else if (_processWaiter.Check(t))
            {
                yield return t;
                // Fill new func body for _process
                mod.Logger.Information(string.Join(", ", _onProcess));
                foreach (var t1 in _onProcess)
                    yield return t1;
                yield return new Token(TokenType.Newline);

                // Then add func signature for our thread before tokens in original _process
                mod.Logger.Information(string.Join(", ", _networkThreadFunctionSignatureTokens));
                foreach (var t1 in _networkThreadFunctionSignatureTokens)
                    yield return t1;
            }
            else if (_actuallyHandlePacketWaiter.Check(t))
            {
                yield return t;

                // Fill body for enqueuing
                mod.Logger.Information(string.Join(", ", _onHandlePacket));
                foreach (var t1 in _onHandlePacket)
                    yield return t1;
                yield return new Token(TokenType.Newline);

                // Then add func for our thread before tokens in original func
                mod.Logger.Information(string.Join(", ", _packetHandlerFunction));
                foreach (var t1 in _packetHandlerFunction)
                    yield return t1;
            }
            else if (_packetFlushStartWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in _lockMutex)
                    yield return t1;
            }
            else if (_packetFlushEndWaiter.Check(t))
            {
                foreach (var t1 in _unlockMutex)
                    yield return t1;
                // Yield the last newline after!
                yield return t;
            }
            else
            {
                yield return t;
            }
        }
    }
}