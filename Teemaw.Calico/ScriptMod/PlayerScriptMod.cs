using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using Teemaw.Calico.Util;
using static GDWeave.Godot.TokenType;
using ScriptTokenizer = Teemaw.Calico.Util.ScriptTokenizer;

namespace Teemaw.Calico.ScriptMod;

/// <summary>
/// Handles script patching for player.gd.
/// NOTE: This script mod does not use <see cref="LexicalTransformer.TransformationRule"/>! This pattern should not be
/// copied for new script mods!
/// </summary>
/// <param name="mod"></param>
/// <param name="config"></param>
public class PlayerScriptMod(IModInterface mod, Config config) : IScriptMod
{
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        """

        var calico_emote_anim = false
        var calico_emote_anim_b = false
        var calico_old_accessory = []

        func calico_cosmetic_data_needs_update(new_cosmetic_data):
        	for key in PlayerData.FALLBACK_COSM.keys():
        		if key == "accessory":
        			if calico_old_accessory.size() != new_cosmetic_data[key].size(): return true
        			for item in new_cosmetic_data[key]:
        				if !calico_old_accessory.has(item): return true
        				if calico_old_accessory.count(item) != new_cosmetic_data[key].count(item): return true
        		elif !new_cosmetic_data.has(key) || !cosmetic_data.has(key):
        			return true
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
        $sound_emit.queue_free()
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
        $body/player_body/Armature/Skeleton/head_dog.queue_free()
        $body/player_body/Armature/Skeleton/head_cat.queue_free()
        $body/player_body/Armature/Skeleton/tool_placeholder.queue_free()

        """, 1);

    private static readonly IEnumerable<Token> AfterAnimTreeDupe = ScriptTokenizer.Tokenize(
        """

        calico_emote_anim = anim_tree.tree_root.get_node("emote_anim")
        calico_emote_anim_b = anim_tree.tree_root.get_node("emote_anim_b")

        """, 1);

    private static readonly IEnumerable<Token> GuardCreateCosmetics = ScriptTokenizer.Tokenize(
        // The original script has a blank line here. We replicate this to improve compatibility with other mods.
        """


        if !calico_cosmetic_data_needs_update(data):
        	print("[calico] Skipping unnecessary cosmetic update")
        	return
        calico_old_accessory = data["accessory"].duplicate()

        """, 1);
    
    private static readonly IEnumerable<Token> SmoothCameraGlobals = ScriptTokenizer.Tokenize(
        """

        var calico_title_mesh
        var calico_last_physics_origin
        
        func calico_body_interpolate(delta):
        	var body_origin = $body.global_transform.origin
        	if body_origin.distance_squared_to(global_transform.origin) > 16:
        		$body.global_transform = global_transform.translated(Vector3.DOWN)
        		return
        	var weight = Engine.get_physics_interpolation_fraction()
        	var virtual_origin = global_transform.translated(Vector3.DOWN).origin
        	$body.global_transform.origin = calico_last_physics_origin.linear_interpolate(virtual_origin, weight)
        	$body.scale = scale
        	var body_rotation = $body.rotation
        	$body.rotation.x = lerp_angle(body_rotation.x, -rotation.x, weight)
        	$body.rotation.y = lerp_angle(body_rotation.y, rotation.y - PI, weight)
        	$body.rotation.z = lerp_angle(body_rotation.z, -rotation.z, weight)

        """);

    private static readonly IEnumerable<Token> CallSmoothCameraUpdate = ScriptTokenizer.Tokenize(
        // The sequence in which these calls are made is very important. Unfortunately we are calling title._process
        // twice per cycle
        """
        
        calico_body_interpolate(delta)
        if controlled:
        	calico_camera_update(delta)

        """, 1);

    private static readonly IEnumerable<Token> SmoothCameraOnReady = ScriptTokenizer.Tokenize(
        """

        $body.set_as_toplevel(true)
        $body.global_transform = self.global_transform
        calico_last_physics_origin = global_transform.translated(Vector3.DOWN).origin
        calico_title_mesh = $title
        calico_title_mesh.calico_setup($body, Vector3(0, 3, 0))
        
        """, 1);
    
    private static readonly IEnumerable<Token> SmoothCameraOnPhysicsProcess = ScriptTokenizer.Tokenize(
        """

        calico_last_physics_origin = global_transform.translated(Vector3.DOWN).origin

        """, 1);

    private static readonly IEnumerable<Token> CameraUpdate = ScriptTokenizer.Tokenize(
        // Note the tabs for indent tokenization
        """
        
        	return

        func calico_camera_update(delta):
        	
        """);

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

        // This waiter is intended to trigger a match somewhere inside `_update_cosmetics` after the cosmetic data is
        // ready (missing/bad values replaced with fallbacks), but before the `cosmetic_data` is actually set.
        // Include the assignment to `data` to maintain compatability with other mods patching this area.
        MultiTokenWaiter updateCosmeticsGuardWaiter = new([
            t => t is IdentifierToken { Name: "data" },
            t => t.Type is OpAssign,
            t => t is IdentifierToken { Name: "PlayerData" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "FALLBACK_COSM" },
            t => t.Type is Period,
            t => t is IdentifierToken { Name: "duplicate" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
        ]);

        var inProcessAnimation = false;
        var skipNextToken = false;
        List<Token> inProcessAnimationTokens = [];

        mod.Logger.Information($"[calico.PlayerScriptMod] Patching {path}");

        var patchFlags = new Dictionary<string, bool>
        {
            ["process_animation"] = false,
            ["globals"] = false,
            ["ready"] = false,
            ["anim_tree_dupe"] = false,
            ["setup_not_controlled"] = false,
            ["cosmetics_update"] = false
        };

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
                        mod.Logger.Information("[calico.PlayerScriptMod] Patching buffered _process_animation");
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

                        patchFlags["process_animation"] = true;
                        mod.Logger.Information("[calico.PlayerScriptMod] process_animation patch OK");
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
                patchFlags["globals"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] globals patch OK");
            }
            else if (readyWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in OnReady)
                    yield return t1;
                patchFlags["ready"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] ready patch OK");
            }
            else if (afterAnimTreeDupeWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in AfterAnimTreeDupe)
                    yield return t1;
                patchFlags["anim_tree_dupe"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] anim_tree_dupe patch OK");
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
                patchFlags["setup_not_controlled"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] setup_not_controlled patch OK");
            }
            else if (updateCosmeticsGuardWaiter.Check(t))
            {
                yield return t;

                foreach (var t1 in GuardCreateCosmetics) yield return t1;
                patchFlags["cosmetics_update"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] cosmetics_update patch OK");
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
                mod.Logger.Error($"[calico.PlayerScriptMod] FAIL: {patch.Key} patch not applied");
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

        MultiTokenWaiter primaryActionHoldWaiter = new([
            t => t is IdentifierToken { Name: "primary_hold_timer" },
            t => t is { Type: OpAssignAdd },
            t => t is ConstantToken c && c.Value.Equals(new IntVariant(1)),
        ]);

        MultiTokenWaiter rotationLerpWaiter = new([
            t => t is IdentifierToken { Name: "rotation" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "y" },
            t => t is { Type: OpAssign },
            t => t is { Type: BuiltInFunc, AssociatedData: (uint?) BuiltinFunction.MathLerpAngle },
            t => t is { Type: ParenthesisOpen },
            t => t is IdentifierToken { Name: "rotation" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "y" },
            t => t is { Type: Comma },
            t => t is IdentifierToken { Name: "rot_help" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "rotation" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "y" },
            t => t is { Type: Comma },
            t => t is ConstantToken c && c.Value.Equals(new RealVariant(0.2)),
        ]);

        mod.Logger.Information($"[calico.PlayerScriptMod] Patching {path}");

        var patchFlags = new Dictionary<string, int>
        {
            ["animation_duration"] = 0,
            ["primary_action_rate"] = 0,
            ["rotation_lerp"] = 0,
        };

        foreach (var t in tokens)
        {
            if (animationGoalWaiter.Check(t))
            {
                yield return new ConstantToken(new IntVariant(30));
                patchFlags["animation_duration"]++;
                mod.Logger.Information("[calico.PlayerScriptMod] animation_duration patch OK");
            }
            else if (primaryActionHoldWaiter.Check(t))
            {
                yield return new ConstantToken(new IntVariant(2));
                patchFlags["primary_action_rate"]++;
                mod.Logger.Information("[calico.PlayerScriptMod] primary_action_rate patch OK");
            }
            else if (rotationLerpWaiter.Check(t))
            {
                rotationLerpWaiter.Reset();
                // Original is 0.2, at 60fps that's 12/s
                yield return new ConstantToken(new IntVariant(12));
                yield return new Token(OpMul);
                yield return new IdentifierToken("delta");
                patchFlags["rotation_lerp"]++;
                mod.Logger.Information("[calico.PlayerScriptMod] rotation_lerp patch OK");
            }
            else
            {
                yield return t;
            }
        }

        foreach (var patch in patchFlags)
        {
            if (patch.Value == 0)
            {
                mod.Logger.Error($"[calico.PlayerScriptMod] FAIL: {patch.Key} patch not applied");
            }
        }
    }

    private static IEnumerable<Token> ModifyForSmoothCamera(IModInterface mod, string path,
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
            t => t.Type is Colon
        ]);
        
        MultiTokenWaiter processWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_process" },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "delta" },
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);

        MultiTokenWaiter cameraUpdateWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_camera_update" },
            t => t.Type is ParenthesisOpen,
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);

        MultiTokenWaiter rotationTransformWaiter = new([
            t => t is IdentifierToken { Name: "rot_help" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "global_transform" },
            t => t is { Type: Period },
            t => t is IdentifierToken { Name: "origin" },
            t => t.Type is OpAssign,
            t => t is IdentifierToken { Name: "global_transform" },
        ]);
        
        MultiTokenWaiter physicsProcessWaiter = new([
            t => t is { Type: PrFunction },
            t => t is IdentifierToken { Name: "_physics_process" },
            t => t.Type is ParenthesisOpen,
            t => t is IdentifierToken { Name: "delta" },
            t => t.Type is ParenthesisClose,
            t => t.Type is Colon
        ]);

        mod.Logger.Information($"[calico.PlayerScriptMod] Patching {path}");

        var patchFlags = new Dictionary<string, bool>
        {
            ["smooth_camera_globals"] = false,
            ["smooth_camera_on_ready"] = false,
            ["smooth_camera_physics_process"] = false,
            ["call_smooth_camera"] = false,
            ["camera_update"] = false
        };

        List<Token> inCameraUpdateTokens = [];
        var inCameraUpdate = false;

        foreach (var t in tokens)
        {
            if (inCameraUpdate)
            {
                inCameraUpdateTokens.Add(t);
                if (t.Type is not Newline || t.AssociatedData is not null) continue;
                inCameraUpdate = false;
                // We're about to leave the func, process the buffered tokens then return all of them.
                mod.Logger.Information("[calico.PlayerScriptMod] Patching buffered _camera_update");
                var replacedTokens = TokenUtil.ReplaceTokens(inCameraUpdateTokens,
                    ScriptTokenizer.Tokenize("var push = global_transform.basis.z * cam_push_cur"),
                    ScriptTokenizer.Tokenize("var push = $body.global_transform.basis.z * cam_push_cur"));
                replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                    ScriptTokenizer.Tokenize("var cam_zoom_lerp = 0.4"),
                    ScriptTokenizer.Tokenize("var cam_zoom_lerp = 24 * delta"));
                replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                    ScriptTokenizer.Tokenize("var cam_base_pos = global_transform.origin + push + sit_add"),
                    ScriptTokenizer.Tokenize("var cam_base_pos = $body.global_transform.origin - push + sit_add + Vector3.UP"));
                // Looks a bit weird, a hack to move this line up
                replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                    ScriptTokenizer.Tokenize("cam_base.global_transform.origin = cam_base_pos"),
                    []);
                replacedTokens = TokenUtil.ReplaceTokens(replacedTokens,
                    [new Token(PrVar), new IdentifierToken("cam_speed"), new Token(OpAssign), new ConstantToken(new RealVariant(0.08))],
                    // The old speed is 0.08/frame, at 60fps this is 4.8/s
                    ScriptTokenizer.Tokenize("""
                                             
                                             cam_base.global_transform.origin = cam_base_pos
                                             var cam_speed = 4.8 * delta
                                             
                                             """, 1));
                foreach (var t1 in replacedTokens)
                    yield return t1;

                patchFlags["camera_update"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] controlled_process patch OK");
            }
            else if (extendsWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in SmoothCameraGlobals) yield return t1;
                patchFlags["smooth_camera_globals"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] smooth_camera_globals patch OK");
            }
            else if (readyWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in SmoothCameraOnReady) yield return t1;
                patchFlags["smooth_camera_on_ready"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] smooth_camera_on_ready patch OK");
            }
            else if (processWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in CallSmoothCameraUpdate) yield return t1;
                patchFlags["call_smooth_camera"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] call_smooth_camera patch OK");
            }
            else if (physicsProcessWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in SmoothCameraOnPhysicsProcess) yield return t1;
                patchFlags["smooth_camera_physics_process"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] smooth_camera_physics_process patch OK");
            }
            else if (cameraUpdateWaiter.Check(t))
            {
                yield return t;
                foreach (var t1 in CameraUpdate) yield return t1;
                inCameraUpdate = true;
                patchFlags["camera_update"] = true;
                mod.Logger.Information("[calico.PlayerScriptMod] camera_update patch OK");
            }
            else if (rotationTransformWaiter.Check(t))
            {
                yield return new Token(Dollar);
                yield return new IdentifierToken("body");
                yield return new Token(Period);
                // global_transform
                yield return t;
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
                mod.Logger.Error($"[calico.PlayerScriptMod] FAIL: {patch.Key} patch not applied");
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

        mod.Logger.Information(
            $"[calico.PlayerScriptMod] ReducePhysicsUpdatesEnabled={config.ReducePhysicsUpdatesEnabled}");
        if (config.ReducePhysicsUpdatesEnabled)
            currentTokens = ModifyForPhysicsHalfSpeed(mod, path, currentTokens).ToList();

        mod.Logger.Information(
            $"[calico.PlayerScriptMod] SmoothCameraEnabled={config.SmoothCameraEnabled}");
        if (config.SmoothCameraEnabled)
            currentTokens = ModifyForSmoothCamera(mod, path, currentTokens).ToList();

        return currentTokens;
    }
}