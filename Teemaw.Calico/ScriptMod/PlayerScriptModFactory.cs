using GDWeave;
using GDWeave.Godot;
using GDWeave.Godot.Variants;
using GDWeave.Modding;
using Teemaw.Calico.LexicalTransformer;
using static Teemaw.Calico.LexicalTransformer.Operation;
using static Teemaw.Calico.LexicalTransformer.TransformationPatternFactory;

namespace Teemaw.Calico.ScriptMod;

public static class PlayerScriptModFactory
{
    public static IScriptMod Create(IModInterface mod, Config config)
    {
        return new TransformationRuleScriptModBuilder()
            .ForMod(mod)
            .Named("PlayerScriptMod")
            .Patching("res://Scenes/Entities/Player/player.gdc")
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_globals")
                .When(config.PlayerOptimizationsEnabled)
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
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

                    """
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_ready")
                .When(config.PlayerOptimizationsEnabled)
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    $Viewport.disable_3d = true
                    $Viewport.usage = 0
                    $body/player_body/Armature/Skeleton/head_dog.queue_free()
                    $body/player_body/Armature/Skeleton/head_cat.queue_free()
                    $body/player_body/Armature/Skeleton/tool_placeholder.queue_free()

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_anim_tree_dupe")
                .When(config.PlayerOptimizationsEnabled)
                .Matching(CreateGdSnippetPattern("anim_tree.tree_root.duplicate(true)"))
                .Do(Append)
                .With(
                    """

                    calico_emote_anim = anim_tree.tree_root.get_node("emote_anim")
                    calico_emote_anim_b = anim_tree.tree_root.get_node("emote_anim_b")

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_caught_item")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("if animation_data[\"caught_item\"] != caught_item:"))
                .Do(ReplaceAll)
                .With("if calico_caught_item_needs_update(animation_data[\"caught_item\"]):"))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_root")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("var root = anim_tree.tree_root"))
                .Do(ReplaceAll)
                .With([]))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_node")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("var node = root.get_node(\"emote_anim\")"))
                .Do(ReplaceAll)
                .With([]))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_use_cached_node")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("if node.animation != animation_data[\"emote\"]:"))
                .Do(ReplaceAll)
                .With("if calico_emote_anim.animation != animation_data[\"emote\"]:"))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_node_b")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("var node_b = root.get_node(\"emote_anim_b\")"))
                .Do(ReplaceAll)
                .With([]))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_set_node")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("node.set_animation(new_anim)"))
                .Do(ReplaceAll)
                .With("calico_emote_anim.set_animation(new_anim)"))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_process_animation_set_node_b")
                .When(config.PlayerOptimizationsEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_process_animation"))
                .Matching(CreateGdSnippetPattern("node_b.set_animation(new_anim)"))
                .Do(ReplaceAll)
                .With("calico_emote_anim_b.set_animation(new_anim)"))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_setup_not_controlled")
                .When(config.PlayerOptimizationsEnabled)
                .Matching(CreateFunctionDefinitionPattern("_setup_not_controlled"))
                .Do(Append)
                .With(
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

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("player_opt_update_cosmetics_guard")
                .When(config.PlayerOptimizationsEnabled)
                .Matching(CreateGdSnippetPattern(
                    // Match the newline after duplicate()
                    """
                    data = PlayerData.FALLBACK_COSM.duplicate()

                    """
                ))
                .Do(Append)
                .With(
                    // The original script has a blank line here. We replicate this to improve compatibility with other
                    // mods.
                    """


                    if !calico_cosmetic_data_needs_update(data):
                    	print("[calico] Skipping unnecessary cosmetic update")
                    	return
                    calico_old_accessory = data["accessory"].duplicate()

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("physics_animation_duration")
                .When(config.ReducePhysicsUpdatesEnabled)
                .Matching(CreateGdSnippetPattern("anim.length * 60"))
                .Do(ReplaceLast)
                .With(new ConstantToken(new IntVariant(30))))
            .AddRule(new TransformationRuleBuilder()
                .Named("physics_primary_action_hold")
                .When(config.ReducePhysicsUpdatesEnabled)
                .Matching(CreateGdSnippetPattern("primary_hold_timer += 1"))
                .Do(ReplaceLast)
                .With(new ConstantToken(new IntVariant(2))))
            .AddRule(new TransformationRuleBuilder()
                .Named("physics_rotation_lerp")
                .When(config.ReducePhysicsUpdatesEnabled)
                .Matching(CreateGdSnippetPattern("rotation.y = lerp_angle(rotation.y, rot_help.rotation.y, 0.2"))
                .Do(ReplaceLast)
                .With("min(12 * delta, 1)")
                .ExpectTimes(3))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_globals")
                .When(config.SmoothCameraEnabled)
                .Matching(CreateGlobalsPattern())
                .Do(Append)
                .With(
                    """

                    var calico_title_mesh
                    var calico_last_physics_origin

                    func calico_body_interpolate(delta):
                    	var body_origin = $body.global_transform.origin
                    	if body_origin.distance_squared_to(global_transform.origin) > 16:
                    		$body.global_transform = global_transform.translated(Vector3.DOWN)
                    		return
                    	var weight = min(Engine.get_physics_interpolation_fraction(), 1)
                    	var virtual_origin = global_transform.translated(Vector3.DOWN).origin
                    	$body.global_transform.origin = calico_last_physics_origin.linear_interpolate(virtual_origin, weight)
                    	$body.scale = scale
                    	var body_rotation = $body.rotation
                    	$body.rotation.x = lerp_angle(body_rotation.x, -rotation.x, weight)
                    	$body.rotation.y = lerp_angle(body_rotation.y, rotation.y - PI, weight)
                    	$body.rotation.z = lerp_angle(body_rotation.z, -rotation.z, weight)

                    """
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_ready")
                .When(config.SmoothCameraEnabled)
                .Matching(CreateFunctionDefinitionPattern("_ready"))
                .Do(Append)
                .With(
                    """

                    $body.set_as_toplevel(true)
                    $body.global_transform = self.global_transform
                    calico_last_physics_origin = global_transform.translated(Vector3.DOWN).origin
                    calico_title_mesh = $title
                    calico_title_mesh.calico_setup($body, Vector3(0, 3, 0))

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_physics_process")
                .When(config.SmoothCameraEnabled)
                .Matching(CreateFunctionDefinitionPattern("_physics_process", ["delta"]))
                .Do(Append)
                .With(
                    """

                    calico_last_physics_origin = global_transform.translated(Vector3.DOWN).origin

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_process")
                .When(config.SmoothCameraEnabled)
                .Matching(CreateFunctionDefinitionPattern("_process", ["delta"]))
                .Do(Append)
                .With(
                    // The order of these calls is important. If swapped, the camera would be rigged to the player's
                    // position in the previous frame, leading to jitter.
                    """

                    calico_body_interpolate(delta)
                    if controlled:
                    	calico_camera_update(delta)

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_rotation_transform")
                .When(config.SmoothCameraEnabled)
                .Matching(CreateGdSnippetPattern("rot_help.global_transform.origin = global_transform"))
                .Do(ReplaceLast)
                .With("$body.global_transform"))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_push_pos")
                .When(config.SmoothCameraEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_camera_update"))
                .Matching(CreateGdSnippetPattern("var push = global_transform.basis.z * cam_push_cur"))
                .Do(ReplaceAll)
                .With("var push = $body.global_transform.basis.z * cam_push_cur"))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_zoom_lerp")
                .When(config.SmoothCameraEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_camera_update"))
                .Matching(CreateGdSnippetPattern("var cam_zoom_lerp = 0.4"))
                .Do(ReplaceAll)
                .With("var cam_zoom_lerp = min(24 * delta, 1)"))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_base_pos")
                .When(config.SmoothCameraEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_camera_update"))
                .Matching(CreateGdSnippetPattern("var cam_base_pos = global_transform.origin + push + sit_add"))
                .Do(ReplaceAll)
                .With("var cam_base_pos = $body.global_transform.origin - push + sit_add + Vector3.UP"))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_base_pos_remove")
                .When(config.SmoothCameraEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_camera_update"))
                .Matching(CreateGdSnippetPattern("cam_base.global_transform.origin = cam_base_pos"))
                .Do(ReplaceAll)
                .With([]))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_new_base_pos")
                .When(config.SmoothCameraEnabled)
                .ScopedTo(CreateFunctionDefinitionPattern("_camera_update"))
                .Matching([
                    t => t.Type is TokenType.PrVar,
                    t => t is IdentifierToken { Name: "cam_speed" },
                    t => t.Type is TokenType.OpAssign,
                    t => t is ConstantToken constant && constant.Value.Equals(new RealVariant(0.08))
                ])
                .Do(ReplaceAll)
                .With(
                    """

                    cam_base.global_transform.origin = cam_base_pos
                    var cam_speed = min(4.8 * delta, 1)

                    """, 1
                ))
            .AddRule(new TransformationRuleBuilder()
                .Named("smooth_camera_camera_update")
                .When(config.SmoothCameraEnabled)
                .Matching(CreateFunctionDefinitionPattern("_camera_update"))
                .Do(Append)
                .With(
                    """
                    
                    	return

                    func calico_camera_update(delta):
                    	
                    """
                ))
            .Build();
    }
}