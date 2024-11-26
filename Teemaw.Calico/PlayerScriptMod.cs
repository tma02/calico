using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class PlayerScriptMod(IModInterface mod) : IScriptMod
{
    private readonly IEnumerable<Token> _globals = ScriptTokenizer.Tokenize(
        """

        var CALICO_MUTEX = false
        var CALICO_THREAD = false
        var CALICO_THREAD_RUN = false
        var CALICO_THREAD_ANIMATION_DATA = []

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
        
        func _calico_thread_process(userdata):
        	while CALICO_THREAD_RUN:
        		CALICO_MUTEX.lock()
        		_calico_process_animation()
        		CALICO_MUTEX.unlock()
        		OS.delay_msec(62 - Time.get_ticks_msec() % 62)

        func _exit_tree():
        	if CALICO_THREAD_RUN:
        		print("[calico] Waiting for player thread to finish...")
        		CALICO_THREAD_RUN = false
        		CALICO_THREAD.wait_to_finish()

        """);

    private readonly IEnumerable<Token> _onPhysicsProcess = ScriptTokenizer.Tokenize(
        // Note the tabs(!) for indent tokenization
        """
        	
        	if controlled:
        		_calico_physics_process()
        	elif CALICO_THREAD_RUN:
        		CALICO_MUTEX.lock()
        		_process_sounds()
        		CALICO_THREAD_ANIMATION_DATA = shared_animation_data.duplicate()
        		_calico_original_process_animation()
        		CALICO_MUTEX.unlock()

        """);

    private readonly IEnumerable<Token> _calicoPhysicsProcess = ScriptTokenizer.Tokenize(
        """

        func _calico_physics_process():
        	_calico_process_animation()
        	_calico_original_process_animation()
        	_process_sounds()
        	return
        	
        func _calico_do_not_call():
        	
        """);

    private readonly IEnumerable<Token> _processAnimation = ScriptTokenizer.Tokenize(
        """
        	
        	return

        func _calico_process_animation():
        	var calico_bobber_transform = bobber.global_transform.scaled(Vector3.ONE)
        	
        """);

    private readonly IEnumerable<Token> _endProcessAnimation = ScriptTokenizer.Tokenize(
        """
        	
        	bobber.set_deferred("global_transform", calico_bobber_transform)

        """);

    private readonly IEnumerable<Token> _originalProcessAnimation = ScriptTokenizer.Tokenize(
        """
        	
        	return

        func _calico_original_process_animation():
        	
        """);

    private readonly IEnumerable<Token> _setupNotControlled = ScriptTokenizer.Tokenize(
        """

        $CollisionShape.disabled = true
        $cam_base.queue_free()
        $cam_pivot.queue_free()
        $SpringArm.queue_free()
        $fishing_update.queue_free()
        $prop_ray.queue_free()
        if !CALICO_THREAD_RUN:
        	print("[calico] Starting player thread")
        	CALICO_MUTEX = Mutex.new()
        	CALICO_THREAD = Thread.new()
        	CALICO_THREAD_RUN = true
        	CALICO_THREAD.start(self, "_calico_thread_process")

        """, 1);

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
            t => t.Type is PrExtends,
            t => t.Type is Identifier,
            t => t.Type is Newline
        ]);

        MultiTokenWaiter readyWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_ready" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon,
        ]);

        MultiTokenWaiter physicsProcessWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_physics_process" },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "delta" },
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon,
        ]);

        MultiTokenWaiter processAnimationWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_process_animation" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon,
        ]);

        MultiTokenWaiter setupNotControlledWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_setup_not_controlled" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon,
        ]);

        MultiTokenWaiter updateCosmeticsGuardWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_update_cosmetics" },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "data" },
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon,
            // ...
            t => t is IdentifierToken { Name: "FALLBACK_COSM" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "duplicate" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
        ], allowPartialMatch: true);
        var inProcessAnimation = false;
        var inProcessAnimationLineCount = 0;
        var skipNextToken = false;
        List<Token> inProcessAnimationTokens = [];

        mod.Logger.Information($"[PlayerScript] Start patching {path}");
        foreach (var t in tokens)
        {
            //mod.Logger.Information(t.ToString());
            if (skipNextToken)
            {
                skipNextToken = false;
                continue;
            }

            if (inProcessAnimation)
            {
                if (t.Type is Newline)
                {
                    inProcessAnimationLineCount += 1;
                    // 32 is where the bulk of anim_tree sets are done. From here we should patch in our thread-safe
                    // version of the rest of the logic.
                    if (inProcessAnimationLineCount == 32)
                    {
                        inProcessAnimation = false;
                        foreach (var t1 in _originalProcessAnimation)
                            yield return t1;
                        continue;
                    }
                }
                switch (t)
                {
                    case IdentifierToken { Name: "set" }:
                        inProcessAnimationTokens.Add(new IdentifierToken("set_deferred"));
                        break;
                    case IdentifierToken { Name: "shared_animation_data" }:
                        inProcessAnimationTokens.Add(new IdentifierToken("CALICO_THREAD_ANIMATION_DATA"));
                        break;
                    case IdentifierToken { Name: "_show_blush" }:
                        inProcessAnimationTokens.Add(new IdentifierToken("call_deferred"));
                        inProcessAnimationTokens.Add(new Token(ParenthesisOpen));
                        inProcessAnimationTokens.Add(new ConstantToken(new StringVariant("_show_blush")));
                        inProcessAnimationTokens.Add(new Token(Comma));
                        skipNextToken = true;
                        break;
                    case IdentifierToken { Name: "_update_caught_item" }:
                        inProcessAnimationTokens.Add(new IdentifierToken("call_deferred"));
                        inProcessAnimationTokens.Add(new Token(ParenthesisOpen));
                        inProcessAnimationTokens.Add(new ConstantToken(new StringVariant("_update_caught_item")));
                        inProcessAnimationTokens.Add(new Token(Comma));
                        skipNextToken = true;
                        break;
                    case IdentifierToken { Name: "set_animation" }:
                        inProcessAnimationTokens.Add(new IdentifierToken("call_deferred"));
                        inProcessAnimationTokens.Add(new Token(ParenthesisOpen));
                        inProcessAnimationTokens.Add(new ConstantToken(new StringVariant("set_animation")));
                        inProcessAnimationTokens.Add(new Token(Comma));
                        skipNextToken = true;
                        break;
                    case IdentifierToken { Name: "set_bone_custom_pose" }:
                        inProcessAnimationTokens.Add(new IdentifierToken("call_deferred"));
                        inProcessAnimationTokens.Add(new Token(ParenthesisOpen));
                        inProcessAnimationTokens.Add(new ConstantToken(new StringVariant("set_bone_custom_pose")));
                        inProcessAnimationTokens.Add(new Token(Comma));
                        skipNextToken = true;
                        break;
                    case { Type: Newline, AssociatedData: null }:
                        inProcessAnimation = false;
                        inProcessAnimationTokens.Add(t);
                        // We're about to leave the func, process the buffered tokens then return all of them.
                        mod.Logger.Information("Patching assignments in _process_animation");
                        var replacedTokens = TokenUtil.ReplaceTokens(inProcessAnimationTokens, [
                            new IdentifierToken("bobber"),
                            new Token(Period),
                            new IdentifierToken("global_transform"),
                        ], [new IdentifierToken("calico_bobber_transform")]);
                        foreach (var t1 in TokenUtil.ReplaceAssignmentsAsDeferred(replacedTokens, ["origin"]))
                        {
                            mod.Logger.Information(t1.ToString());
                            yield return t1;
                        }

                        foreach (var t1 in _endProcessAnimation)
                        {
                            yield return t1;
                        }

                        break;
                    default:
                        inProcessAnimationTokens.Add(t);
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
            else if (physicsProcessWaiter.Check(t))
            {
                yield return t;
                mod.Logger.Information(string.Join(", ", _onPhysicsProcess));
                foreach (var t1 in _onPhysicsProcess)
                    yield return t1;
                mod.Logger.Information(string.Join(", ", _calicoPhysicsProcess));
                foreach (var t1 in _calicoPhysicsProcess)
                    yield return t1;
            }
            else if (processAnimationWaiter.Check(t))
            {
                yield return t;

                mod.Logger.Information("[PlayerScript] Entering _process_animation");
                inProcessAnimation = true;
                mod.Logger.Information(string.Join(", ", _processAnimation));
                foreach (var t1 in _processAnimation)
                    yield return t1;
            }
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