using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMods;

public class SteamNetworkScriptMod(IModInterface mod) : IScriptMod
{
    private readonly MultiTokenWaiter _extendsWaiter = new([
        t => t.Type is PrExtends,
        t => t.Type is Identifier,
        t => t.Type is Newline
    ]);

    private readonly MultiTokenWaiter _readyWaiter = new([
        t => t is { Type: PrFunction },
        t => t is IdentifierToken { Name: "_ready" },
        t => t.Type is ParenthesisOpen,
        t => t.Type is ParenthesisClose,
        t => t.Type is Colon,
    ]);

    private readonly MultiTokenWaiter _processWaiter = new([
        t => t is { Type: PrFunction },
        t => t is IdentifierToken { Name: "_process" },
        t => t.Type is ParenthesisOpen,
        t => t is IdentifierToken { Name: "delta" },
        t => t.Type is ParenthesisClose,
        t => t is IdentifierToken { Name: "run_callbacks" },
        t => t.Type is ParenthesisOpen,
        t => t.Type is ParenthesisClose,
    ], allowPartialMatch: true);

    private readonly MultiTokenWaiter _packetFlushStartWaiter = new([
        t => t is { Type: PrFunction },
        t => t is IdentifierToken { Name: "_packet_flush" },
        t => t.Type is ParenthesisOpen,
        t => t.Type is ParenthesisClose,
        t => t.Type is Colon
    ]);

    private readonly MultiTokenWaiter _packetFlushEndWaiter = new([
        t => t is { Type: PrFunction },
        t => t is IdentifierToken { Name: "_packet_flush" },
        t => t.Type is ParenthesisOpen,
        t => t.Type is ParenthesisClose,
        t => t.Type is Colon,
        t => t is IdentifierToken { Name: "FLUSH_PACKET_INFORMATION" },
        t => t.Type is OpAssign,
        t => t.Type is Newline
    ], allowPartialMatch: true);

    // A bit sketchy since we're trying to match over so many tokens.
    private readonly MultiTokenWaiter _actuallyHandlePacketWaiter = new([
        t => t is { Type: PrFunction },
        t => t is IdentifierToken { Name: "_read_P2P_Packet" },
        t => t.Type is ParenthesisOpen,
        // ...
        t => t is IdentifierToken { Name: "readP2PPacket" },
        // ...
        t => t is IdentifierToken { Name: "FLUSH_PACKET_INFORMATION" },
        // ...
        t => t.Type is OpAssignAdd,
        // ...
        t => t.Type is Newline
    ], allowPartialMatch: true);
    
    private readonly MultiTokenWaiter _steamReadP2PPacketWaiter = new([
        t => t is { Type: OpAssign },
        t => t is IdentifierToken { Name: "Steam" },
        t => t.Type is Period,
        t => t is IdentifierToken { Name: "readP2PPacket" },
        t => t.Type is ParenthesisOpen,
        t => t is IdentifierToken { Name: "PACKET_SIZE" },
        t => t.Type is Comma,
        t => t is IdentifierToken { Name: "channel" },
        t => t.Type is ParenthesisClose,
        t => t.Type is Newline
    ]);

    private readonly MultiTokenWaiter _sendP2PPacketWaiter = new([
        t => t is { Type: PrFunction },
        t => t is IdentifierToken { Name: "_send_P2P_Packet" },
        t => t.Type is ParenthesisOpen,
        t => t is IdentifierToken { Name: "packet_data" },
        t => t is IdentifierToken { Name: "target" },
        t => t is IdentifierToken { Name: "type" },
        t => t is IdentifierToken { Name: "channel" },
        t => t.Type is ParenthesisClose,
        t => t.Type is Colon
    ], allowPartialMatch: true);

    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        """
        
        var CALICO_RECV_PACKET_QUEUE = []
        var RECV_NET_MUTEX
        var RECV_NET_THREAD
        var RECV_NET_THREAD_RUN = true
        var CALICO_SEND_PACKET_QUEUE = []
        var SEND_NET_MUTEX
        var SEND_NET_THREAD
        var SEND_NET_THREAD_RUN = true
        var ACTOR_UPDATE_STATE = {}

        func _exit_tree():
        	print("[calico] Waiting for network thread to finish...")
        	RECV_NET_THREAD_RUN = false
        	RECV_NET_THREAD.wait_to_finish()
        	SEND_NET_THREAD_RUN = false
        	SEND_NET_THREAD.wait_to_finish()
        	
        func _calico_recv_net_thread():
        	while RECV_NET_THREAD_RUN:
        		RECV_NET_MUTEX.lock()
        		_calico_recv_net_process(0)
        		RECV_NET_MUTEX.unlock()
        		OS.delay_msec(62 - Time.get_ticks_msec() % 62)
        
        func _calico_send_net_thread():
        	while SEND_NET_THREAD_RUN:
        		SEND_NET_MUTEX.lock()
        		for packet in CALICO_SEND_PACKET_QUEUE:
        			_calico_send_P2P_packet_on_thread(packet)
        		CALICO_SEND_PACKET_QUEUE.clear()
        		SEND_NET_MUTEX.unlock()
        		OS.delay_msec(62 - Time.get_ticks_msec() % 62)

        """);

    private static readonly IEnumerable<Token> OnReady = ScriptTokenizer.Tokenize(
        """

        if !STEAM_ENABLED: return
        RECV_NET_MUTEX = Mutex.new()
        SEND_NET_MUTEX = Mutex.new()
        RECV_NET_THREAD = Thread.new()
        SEND_NET_THREAD = Thread.new()
        print("[calico] Starting receiver network thread...")
        RECV_NET_THREAD.start(self, "_calico_recv_net_thread")
        print("[calico] Starting sender network thread...")
        SEND_NET_THREAD.start(self, "_calico_send_net_thread")

        """, 1);

    private static readonly IEnumerable<Token> OnProcess = ScriptTokenizer.Tokenize(
        // Note the tab character instead of spaces. This is required by the tokenizer.
        """
        
        RECV_NET_MUTEX.lock()
        for packet in CALICO_RECV_PACKET_QUEUE:
        	_calico_process_P2P_packet_on_main(packet)
        CALICO_RECV_PACKET_QUEUE.clear()
        RECV_NET_MUTEX.unlock()

        """, 1);

    private static readonly IEnumerable<Token> OnHandlePacket = ScriptTokenizer.Tokenize(
        """

        CALICO_RECV_PACKET_QUEUE.append({"PACKET_SIZE": PACKET_SIZE, "PACKET_SENDER": PACKET_SENDER, "DATA": DATA, "type": type, "from_host": from_host})

        """, 2);

    private static readonly IEnumerable<Token> AfterSteamReadPacket = ScriptTokenizer.Tokenize(
        """

        if PACKET.empty(): return

        """, 2);

    private static readonly IEnumerable<Token> NetworkThreadFunctionSignatureTokens = ScriptTokenizer.Tokenize(
        "func _calico_recv_net_process(delta): ");

    private static readonly IEnumerable<Token> PacketHandlerFunction = ScriptTokenizer.Tokenize(
        """
        func _calico_process_P2P_packet_on_main(packet):
        	var PACKET_SIZE = packet["PACKET_SIZE"]
        	var PACKET_SENDER = packet["PACKET_SENDER"]
        	var DATA = packet["DATA"]
        	var type = packet["type"]
        	var from_host = packet["from_host"]
        	if true:
        		
        """);

    // TODO: mutex lock anything that the original send func interacts with
    private static readonly IEnumerable<Token> OnSend = ScriptTokenizer.Tokenize(
        // Note the tab character instead of spaces. This is required by the tokenizer.
        """
        	
        	SEND_NET_MUTEX.lock()
        	CALICO_SEND_PACKET_QUEUE.append({ "packet_data": packet_data, "target": target, "type": type, "channel": channel })
        	SEND_NET_MUTEX.unlock()

        func _calico_send_P2P_packet_on_thread(packet):
        	var packet_data = packet["packet_data"]
        	var target = packet["target"]
        	var type = packet["type"]
        	var channel = packet["channel"]
        	
        """);

    private static readonly IEnumerable<Token> LockMutex = ScriptTokenizer.Tokenize(
        """
        
        SEND_NET_MUTEX.lock()
        
        """, 1);

    private static readonly IEnumerable<Token> UnlockMutex = ScriptTokenizer.Tokenize(
        """
        
        SEND_NET_MUTEX.unlock()
        
        """, 1);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Singletons/SteamNetwork.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        mod.Logger.Information($"[calico.SteamNetworkScript] Patching {path}");
        var patchFlags = new Dictionary<string, bool>
        {
            ["globals"] = false,
            ["ready"] = false,
            ["process"] = false,
            ["handle_packet"] = false,
            ["after_steam_read"] = false,
            ["send_packet"] = false,
            ["packet_flush_lock"] = false,
            ["packet_flush_unlock"] = false
        };
        foreach (var t in tokens)
        {
            if (_extendsWaiter.Check(t))
            {
                yield return t;

                // Insert our globals
                foreach (var t1 in Globals)
                    yield return t1;
                patchFlags["globals"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] globals patch OK");
            }
            else if (_readyWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in OnReady)
                    yield return t1;
                patchFlags["ready"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] ready patch OK");
            }
            else if (_processWaiter.Check(t))
            {
                yield return t;
                // Fill new func body for _process
                foreach (var t1 in OnProcess)
                    yield return t1;
                // End the _process function here
                yield return new Token(Newline);

                // Then add func signature for our thread before tokens in original _process
                foreach (var t1 in NetworkThreadFunctionSignatureTokens)
                    yield return t1;
                patchFlags["process"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] process patch OK");
            }
            else if (_actuallyHandlePacketWaiter.Check(t))
            {
                yield return t;

                // Fill body for enqueuing received packets
                foreach (var t1 in OnHandlePacket)
                    yield return t1;
                // End the _read_P2P_Packet function here
                yield return new Token(Newline);

                // Then add func for our thread before tokens in original func
                foreach (var t1 in PacketHandlerFunction)
                    yield return t1;
                patchFlags["handle_packet"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] handle_packet patch OK");
            }
            else if (_steamReadP2PPacketWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in AfterSteamReadPacket) yield return t1;
                patchFlags["after_steam_read"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] after_steam_read patch OK");
            }
            else if (_sendP2PPacketWaiter.Check(t))
            {
                yield return t;
                
                // Fill body for enqueuing packets for send
                foreach (var t1 in OnSend)
                    yield return t1;
                patchFlags["send_packet"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] send_packet patch OK");
            }
            else if (_packetFlushStartWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in LockMutex)
                    yield return t1;
                patchFlags["packet_flush_lock"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] _packet_flush mutex lock patch OK");
            }
            else if (_packetFlushEndWaiter.Check(t))
            {
                // The next t is the newline closing this function, we want this in the func so don't yield it yet.
                foreach (var t1 in UnlockMutex)
                    yield return t1;
                // Yield the last newline after!
                yield return t;
                patchFlags["packet_flush_unlock"] = true;
                mod.Logger.Information("[calico.SteamNetworkScript] _packet_flush mutex unlock patch OK");
            }
            else
            {
                yield return t;
            }
        }
        
        foreach (var patch in patchFlags)
        {
            if (!patch.Value)
            {
                mod.Logger.Error($"[calico.SteamNetworkScript] FAIL: {patch.Key} patch not applied");
            }
        }
    }
}