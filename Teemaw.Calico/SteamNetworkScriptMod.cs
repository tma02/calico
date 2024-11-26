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

    private readonly MultiTokenWaiter _sendP2PPacketWaiter = new([
        t => t is { Type: TokenType.PrFunction },
        t => t is IdentifierToken { Name: "_send_P2P_Packet" },
        t => t.Type is TokenType.ParenthesisOpen,
        t => t is IdentifierToken { Name: "packet_data" },
        t => t is IdentifierToken { Name: "target" },
        t => t is IdentifierToken { Name: "type" },
        t => t is IdentifierToken { Name: "channel" },
        t => t.Type is TokenType.ParenthesisClose,
        t => t.Type is TokenType.Colon
    ], allowPartialMatch: true);

    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
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

    private readonly IEnumerable<Token> _onReady = ScriptTokenizer.Tokenize(
        """

        if !STEAM_ENABLED: return
        RECV_NET_MUTEX = Mutex.new()
        SEND_NET_MUTEX = Mutex.new()
        RECV_NET_THREAD = Thread.new()
        SEND_NET_THREAD = Thread.new()
        print("[calico1] Starting receiver network thread...")
        RECV_NET_THREAD.start(self, "_calico_recv_net_thread")
        print("[calico] Starting sender network thread...")
        SEND_NET_THREAD.start(self, "_calico_send_net_thread")

        """, 1);

    private readonly IEnumerable<Token> _onProcess = ScriptTokenizer.Tokenize(
        // Note the tab character instead of spaces. This is required by the tokenizer.
        """
        
        RECV_NET_MUTEX.lock()
        for packet in CALICO_RECV_PACKET_QUEUE:
        	_calico_process_P2P_packet_on_main(packet)
        CALICO_RECV_PACKET_QUEUE.clear()
        RECV_NET_MUTEX.unlock()

        """, 1);

    private readonly IEnumerable<Token> _onHandlePacket = ScriptTokenizer.Tokenize(
        """

        CALICO_RECV_PACKET_QUEUE.append({"PACKET_SIZE": PACKET_SIZE, "PACKET_SENDER": PACKET_SENDER, "DATA": DATA, "type": type, "from_host": from_host})

        """, 2);

    private readonly IEnumerable<Token> _networkThreadFunctionSignatureTokens = ScriptTokenizer.Tokenize(
        "func _calico_recv_net_process(delta): ");

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

    // TODO: mutex lock anything that the original send func interacts with
    private readonly IEnumerable<Token> _onSend = ScriptTokenizer.Tokenize(
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

    private readonly IEnumerable<Token> _lockMutex = ScriptTokenizer.Tokenize(
        """
        
        SEND_NET_MUTEX.lock()
        
        """, 1);

    private readonly IEnumerable<Token> _unlockMutex = ScriptTokenizer.Tokenize(
        """
        
        SEND_NET_MUTEX.unlock()
        
        """, 1);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Singletons/SteamNetwork.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        mod.Logger.Information($"[SteamNetworkScript] Patching {path}");
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
                yield return new IdentifierToken("RECV_NET_MUTEX");
                yield return new Token(TokenType.Newline);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("RECV_NET_THREAD");
                yield return new Token(TokenType.Newline);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("SEND_NET_MUTEX");
                yield return new Token(TokenType.Newline);
                yield return new Token(TokenType.PrVar);
                yield return new IdentifierToken("SEND_NET_THREAD");
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
                // End the _process function here
                yield return new Token(TokenType.Newline);

                // Then add func signature for our thread before tokens in original _process
                mod.Logger.Information(string.Join(", ", _networkThreadFunctionSignatureTokens));
                foreach (var t1 in _networkThreadFunctionSignatureTokens)
                    yield return t1;
            }
            else if (_actuallyHandlePacketWaiter.Check(t))
            {
                yield return t;

                // Fill body for enqueuing received packets
                mod.Logger.Information(string.Join(", ", _onHandlePacket));
                foreach (var t1 in _onHandlePacket)
                    yield return t1;
                // End the _read_P2P_Packet function here
                yield return new Token(TokenType.Newline);

                // Then add func for our thread before tokens in original func
                mod.Logger.Information(string.Join(", ", _packetHandlerFunction));
                foreach (var t1 in _packetHandlerFunction)
                    yield return t1;
            }
            else if (_sendP2PPacketWaiter.Check(t))
            {
                yield return t;
                
                // Fill body for enqueuing packets for send
                mod.Logger.Information(string.Join(", ", _onSend));
                foreach (var t1 in _onSend)
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
                // The next t is the newline closing this function, we want this in the func so don't yield it yet.
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