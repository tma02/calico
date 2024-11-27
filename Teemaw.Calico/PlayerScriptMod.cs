using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class PlayerScriptMod(IModInterface mod, Config config) : IScriptMod
{
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        """

        var calico_emote_anim = false
        var calico_emote_anim_b = false

        func calico_cosmetic_data_needs_update(new_cosmetic_data):
        	for key in PlayerData.FALLBACK_COSM.keys():
        		if key == "accessory":
        			if cosmetic_data[key].size() != new_cosmetic_data[key].size(): return true
        			for item in cosmetic_data[key]:
        				if !new_cosmetic_data[key] || !new_cosmetic_data[key].has(item): return true
        				if cosmetic_data[key].count(item) != new_cosmetic_data[key].count(item): return true
        		elif cosmetic_data[key] != new_cosmetic_data[key]:
        			return true
        	return false

        func calico_caught_item_needs_update(new_caught):
        	if new_caught.empty() != caught_item.empty():
        		return true
        	if !new_caught.keys().has("id") || !new_caught.keys().has("size") || !new_caught.keys().has("quality"):
        		return false
        	return new_caught["id"] != caught_item["id"] || new_caught["size"] != caught_item["size"] || new_caught["quality"] != caught_item["quality"]

        """);

    private static readonly IEnumerable<Token> SetupNotControlled = ScriptTokenizer.Tokenize(
        """

        $CollisionShape.disabled = true
        $cam_base.queue_free()
        $cam_pivot.queue_free()
        $SpringArm.queue_free()
        $fishing_update.queue_free()
        $prop_ray.queue_free()
        $bobber_preview.queue_free()
        $detection_zones.queue_free()
        $interact_range.queue_free()
        $catch_cam_position.queue_free()
        $camera_freecam_anchor.queue_free()
        $water_detect.queue_free()
        $raincloud_check.queue_free()
        $local_range.queue_free()
        $rot_help.queue_free()
        $lean_help.queue_free()
        $safe_check.queue_free()
        $paint_node.queue_free()
        $fish_catch_timer.queue_free()
        $step_timer.queue_free()
        $image_update.queue_free()
        $rain_timer.queue_free()
        $metaldetect_timer.queue_free()
        $cosmetic_refresh.queue_free()

        """, 1);

    private static readonly IEnumerable<Token> OnReady = ScriptTokenizer.Tokenize(
        """

        $Viewport.disable_3d = true
        $Viewport.usage = 0

        """, 1);

    private static readonly IEnumerable<Token> AfterAnimTreeDupe = ScriptTokenizer.Tokenize(
        """

        calico_emote_anim = anim_tree.tree_root.get_node("emote_anim")
        calico_emote_anim_b = anim_tree.tree_root.get_node("emote_anim_b")

        """, 1);

    private static readonly IEnumerable<Token> GuardCreateCosmetics = ScriptTokenizer.Tokenize(
        // Note the tab for indent tokenization
        """

        if !calico_cosmetic_data_needs_update(data):
        	print("[calico] Skipping unnecessary cosmetic update")
        	return

        """, 1);

    public bool ShouldRun(string path) => path == "res://Scenes/Entities/Player/player.gdc";

    private static IEnumerable<Token> ModifyForPlayerOptimizations(IModInterface mod, string path,
        IEnumerable<Token> tokens)
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

        MultiTokenWaiter afterAnimTreeDupeWaiter = new([
            t => t is IdentifierToken { Name: "anim_tree" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "tree_root" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "duplicate" },
            t => t.Type is ParenthesisOpen,
            t => t is ConstantToken c && c.Value.Equals(new BoolVariant(true)),
            t => t.Type is ParenthesisClose,
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
        var skipNextToken = false;
        List<Token> inProcessAnimationTokens = [];

        mod.Logger.Information($"[calico.PlayerScriptMod] Patching {path}");
        foreach (var t in tokens)
        {
            if (skipNextToken)
            {
                skipNextToken = false;
                continue;
            }

            if (inProcessAnimation)
            {
                switch (t)
                {
                    case { Type: Newline, AssociatedData: null }:
                        inProcessAnimation = false;
                        inProcessAnimationTokens.Add(t);
                        // We're about to leave the func, process the buffered tokens then return all of them.
                        mod.Logger.Information("Patching assignments in _process_animation");
                        var replacedTokens = TokenUtil.ReplaceTokens(inProcessAnimationTokens,
                            ScriptTokenizer.Tokenize("if animation_data[\"caught_item\"] != caught_item:"),
                            ScriptTokenizer.Tokenize(
                                "if calico_caught_item_needs_update(animation_data[\"caught_item\"]):"));
                        replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                            ScriptTokenizer.Tokenize("var root = anim_tree.tree_root"),
                            []);
                        replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                            ScriptTokenizer.Tokenize("var node = root.get_node(\"emote_anim\")"),
                            []);
                        replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                            ScriptTokenizer.Tokenize("if node.animation != animation_data[\"emote\"]:"),
                            ScriptTokenizer.Tokenize("if calico_emote_anim.animation != animation_data[\"emote\"]:"));
                        replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                            ScriptTokenizer.Tokenize("var node_b = root.get_node(\"emote_anim_b\")"),
                            []);
                        replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                            ScriptTokenizer.Tokenize("node.set_animation(animation_data[\"emote\"])"),
                            ScriptTokenizer.Tokenize("calico_emote_anim.set_animation(animation_data[\"emote\"])"));
                        replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                            ScriptTokenizer.Tokenize("node_b.set_animation(animation_data[\"emote\"])"),
                            ScriptTokenizer.Tokenize("calico_emote_anim_b.set_animation(animation_data[\"emote\"])"));
                        foreach (var t1 in replacedTokens)
                            yield return t1;

                        break;
                    default:
                        inProcessAnimationTokens.Add(t);
                        break;
                }
            }
            else if (extendsWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in Globals)
                    yield return t1;
            }
            else if (readyWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in OnReady)
                    yield return t1;
            }
            else if (afterAnimTreeDupeWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in AfterAnimTreeDupe)
                    yield return t1;
            }
            else if (processAnimationWaiter.Check(t))
            {
                yield return t;

                mod.Logger.Information("[calico.PlayerScriptMod] Entering _process_animation");
                inProcessAnimation = true;
            }
            else if (setupNotControlledWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in SetupNotControlled) yield return t1;
            }
            else if (updateCosmeticsGuardWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in GuardCreateCosmetics) yield return t1;
            }
            else
            {
                yield return t;
            }
        }
    }

    private static IEnumerable<Token> ModifyForPhysicsHalfSpeed(IModInterface mod, string path,
        IEnumerable<Token> tokens)
    {
        MultiTokenWaiter animationGoalWaiter = new([
            t => t is IdentifierToken { Name: "anim" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "length" },
            t => t.Type is OpMul,
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(60)),
        ]);

        mod.Logger.Information($"[calico.PlayerScriptMod] Patching {path}");

        foreach (var t in tokens)
        {
            if (animationGoalWaiter.Check(t))
            {
                yield return new ConstantToken(new IntVariant(30));
            }
            else
            {
                yield return t;
            }
        }
    }

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
        var currentTokens = tokens.ToList();
    
        mod.Logger.Information(
            $"[calico.PlayerScriptMod] PlayerOptimizationsEnabled={config.PlayerOptimizationsEnabled}");
        if (config.PlayerOptimizationsEnabled)
            currentTokens = ModifyForPlayerOptimizations(mod, path, currentTokens).ToList();
        
        mod.Logger.Information($"[calico.PlayerScriptMod] PhysicsHalfSpeedEnabled={config.ReducePhysicsUpdatesEnabled}");
        if (config.ReducePhysicsUpdatesEnabled)
            currentTokens = ModifyForPhysicsHalfSpeed(mod, path, currentTokens).ToList();
        
        return currentTokens;
    }
}
