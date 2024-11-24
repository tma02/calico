using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;

namespace Teemaw.Calico;

public class PlayerScriptMod(IModInterface mod) : IScriptMod
{
    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        var CALICO_MUTEX = false
        var CALICO_THREAD = false
        var CALICO_THREAD_RUN = false
        var THREAD_LOCKS = 0
        var SET_LOCKS = 0
        var CALICO_TITLE_CANVASLAYER = {}

        func _calico_cosmetic_data_needs_update(new_cosmetic_data):
        	for key in PlayerData.FALLBACK_COSM.keys():
        		if key == "accessory":
        			if cosmetic_data[key].size() != new_cosmetic_data[key].size(): return true
        			for item in cosmetic_data[key]:
        				if !new_cosmetic_data[key] || !new_cosmetic_data[key].has(item): return true
        				if cosmetic_data[key].count(item) != new_cosmetic_data[key].count(item): return true
        		elif cosmetic_data[key] != new_cosmetic_data[key]:
        			return true
        	return false
        	
        func _calico_thread_process():
        	while CALICO_THREAD_RUN:
        		CALICO_MUTEX.lock()
        		_calico_process_animation()
        		CALICO_MUTEX.unlock()
        		print("u")
        		OS.delay_msec(62 - Time.get_ticks_msec() % 62)

        func _exit_tree():
        	if CALICO_THREAD_RUN:
        		print("[calico] Waiting for player thread to finish...")
        		CALICO_THREAD_RUN = false
        		CALICO_THREAD.wait_to_finish()

        func set(property, value):
        	if property == "shared_animation_data" && CALICO_THREAD_RUN:
        		CALICO_MUTEX.lock()
        		set_indexed(property, value)
        		CALICO_MUTEX.unlock()
        		print("su")
        	else:
        		set_indexed(property, value)
        
        """);

    private readonly IEnumerable<Token> _onPhysicsProcess = ScriptTokenizer.Tokenize(
        // Note the tabs(!) for indent tokenization
        """
        	
        	if controlled:
        		_calico_physics_process()
        	if !controlled:
        		_process_sounds()

        """);

    private readonly IEnumerable<Token> _calicoPhysicsProcess = ScriptTokenizer.Tokenize(
        """

        func _calico_physics_process():
        	_calico_process_animation()
        	_process_sounds()
        	return
        	
        func _calico_do_not_call():
        	
        """);

    private readonly IEnumerable<Token> _processAnimation = ScriptTokenizer.Tokenize(
        """
        
        	return
        
        func _calico_process_animation():
        	
        """);

    private readonly IEnumerable<Token> _setupNotControlled = ScriptTokenizer.Tokenize(
        """

        $CollisionShape.disabled = true
        $cam_base.queue_free()
        $cam_pivot.queue_free()
        $SpringArm.queue_free()
        $fishing_update.queue_free()
        $prop_ray.queue_free()

        """, 1);
    
    
    // if !CALICO_THREAD_RUN:
    //     print("[calico] Starting player thread")
    //     CALICO_MUTEX = Mutex.new()
    //     CALICO_THREAD = Thread.new()
    //     CALICO_THREAD_RUN = true
    //     CALICO_THREAD.start(self, "_calico_thread_process")

    private readonly IEnumerable<Token> _onReady = ScriptTokenizer.Tokenize(
        """

        $Viewport.disable_3d = true
        $Viewport.usage = 0

        """, 1);

    private readonly IEnumerable<Token> _guardCreateCosmetics = ScriptTokenizer.Tokenize(
        // Note the tab for indent tokenization
        """

        if !_calico_cosmetic_data_needs_update(data):
        	print("[calico] Skipping unnecessary cosmetic update")
        	return

        """, 1);

    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/player.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        MultiTokenWaiter extendsWaiter = new([
            t => t.Type is TokenType.PrExtends,
            t => t.Type is TokenType.Identifier,
            t => t.Type is TokenType.Newline
        ]);

        MultiTokenWaiter readyWaiter = new([
            t => t is { Type: TokenType.PrFunction },
            t => t is IdentifierToken { Name: "_ready" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon,
        ]);

        MultiTokenWaiter physicsProcessWaiter = new([
            t => t is { Type: TokenType.PrFunction },
            t => t is IdentifierToken { Name: "_physics_process" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t is IdentifierToken { Name: "delta" },
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon,
        ]);

        MultiTokenWaiter processAnimationWaiter = new([
            t => t is { Type: TokenType.PrFunction },
            t => t is IdentifierToken { Name: "_process_animation" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon,
        ]);

        MultiTokenWaiter setupNotControlledWaiter = new([
            t => t is { Type: TokenType.PrFunction },
            t => t is IdentifierToken { Name: "_setup_not_controlled" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon,
        ]);

        MultiTokenWaiter updateCosmeticsGuardWaiter = new([
            t => t is { Type: TokenType.PrFunction },
            t => t is IdentifierToken { Name: "_update_cosmetics" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t is IdentifierToken { Name: "data" },
            t => t.Type is TokenType.ParenthesisClose,
            t => t.Type is TokenType.Colon,
            // ...
            t => t is IdentifierToken { Name: "FALLBACK_COSM" },
            t => t.Type is TokenType.Period,
            t => t is IdentifierToken { Name: "duplicate" },
            t => t.Type is TokenType.ParenthesisOpen,
            t => t.Type is TokenType.ParenthesisClose,
        ], allowPartialMatch: true);
        var inProcessAnimation = false;
        var skipNextToken = false;

        mod.Logger.Information($"[PlayerScript] Patching {path}");
        foreach (var t in tokens)
        {
            mod.Logger.Information(t.ToString());
            if (skipNextToken)
            {
                skipNextToken = false;
                continue;
            }
            if (inProcessAnimation)
            {
                switch (t)
                {
                    case IdentifierToken { Name: "set" }:
                        yield return new IdentifierToken("set_deferred");
                        break;
                    case IdentifierToken { Name: "set_animation" }:
                        yield return new IdentifierToken("call_deferred");
                        yield return new Token(TokenType.ParenthesisOpen);
                        yield return new ConstantToken(new StringVariant("set_animation"));
                        yield return new Token(TokenType.Comma);
                        skipNextToken = true;
                        break;
                    case IdentifierToken { Name: "set_bone_custom_pose" }:
                        yield return new IdentifierToken("call_deferred");
                        yield return new Token(TokenType.ParenthesisOpen);
                        yield return new ConstantToken(new StringVariant("set_bone_custom_pose"));
                        yield return new Token(TokenType.Comma);
                        skipNextToken = true;
                        break;
                    case { Type: TokenType.Newline, AssociatedData: null }:
                        inProcessAnimation = false;
                        yield return t;
                        break;
                    default:
                        yield return t;
                        break;
                }
            }
            else if (extendsWaiter.Check(t))
            {
                yield return t;
                mod.Logger.Information(string.Join(", ", _globals));
                foreach (var t1 in _globals)
                    yield return t1;
            }
            else if (readyWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _onReady)
                    yield return t1;
            }
            // else if (physicsProcessWaiter.Check(t))
            // {
            //     yield return t;
            //     mod.Logger.Information(string.Join(", ", _onPhysicsProcess));
            //     foreach (var t1 in _onPhysicsProcess)
            //         yield return t1;
            //     mod.Logger.Information(string.Join(", ", _calicoPhysicsProcess));
            //     foreach (var t1 in _calicoPhysicsProcess)
            //         yield return t1;
            // }
            // else if (processAnimationWaiter.Check(t))
            // {
            //     yield return t;
            //     mod.Logger.Information(string.Join(", ", _processAnimation));
            //     foreach (var t1 in _processAnimation)
	           //      yield return t1;
            //
            //     inProcessAnimation = true;
            // }
            else if (setupNotControlledWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _setupNotControlled) yield return t1;
            }
            else if (updateCosmeticsGuardWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in _guardCreateCosmetics) yield return t1;
            }
            else
            {
                yield return t;
            }
        }
    }
}