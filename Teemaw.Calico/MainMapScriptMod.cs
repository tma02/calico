﻿using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class MainMapScriptMod(IModInterface mod, Config config): IScriptMod
{
	// TODO: Clean this up somehow -- this was way simpler when it was supposed to be just three tree nodes. 
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        """
        
        var calico_water_ld_mat: Material
        var calico_water_hd_mat: Material
        var calico_water_mmis = []
        
        func _ready():
        	print("[calico] Building mesh instances...")
        	var tree_a_mmi = calico_build_mesh_parented_static_body_mmi($zones/main_zone/trees/tree_a)
        	$zones/main_zone/trees.add_child(tree_a_mmi)
        	var tree_b_mmi = calico_build_mesh_parented_static_body_mmi($zones/main_zone/trees/tree_b)
        	$zones/main_zone/trees.add_child(tree_b_mmi)
        	var tree_c_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/trees/tree_c, "tree_3.tscn", "MeshInstance")
        	var logs_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/trees/tree_c, "log.tscn", "Leaf")
        	$zones/main_zone/trees.add_child(tree_c_mmi)
        	$zones/main_zone/trees.add_child(logs_mmi)
        	var bush_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "bush.tscn", "Leaf")
        	$zones/main_zone/props.add_child(bush_mmi)
        	var reeds_meshinstance_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "reeds.tscn", "MeshInstance")
        	$zones/main_zone/props.add_child(reeds_meshinstance_mmi)
        	var reeds_meshinstance2_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "reeds.tscn", "MeshInstance2")
        	$zones/main_zone/props.add_child(reeds_meshinstance2_mmi)
        	var reeds_meshinstance3_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "reeds.tscn", "MeshInstance3")
        	$zones/main_zone/props.add_child(reeds_meshinstance3_mmi)
        	var reeds_meshinstance4_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "reeds.tscn", "MeshInstance4")
        	$zones/main_zone/props.add_child(reeds_meshinstance4_mmi)
        	var mushroom_1_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "mushroom_1.tscn", "mushroom_1")
        	$zones/main_zone/props.add_child(mushroom_1_mmi)
        	var mushroom_2_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "mushroom_2.tscn", "mushroom_1001")
        	$zones/main_zone/props.add_child(mushroom_2_mmi)
        	var bench_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "bench.tscn", "Cube")
        	$zones/main_zone/props.add_child(bench_mmi)
        	var rock_1_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "rock_1.tscn", "Icosphere")
        	$zones/main_zone/props.add_child(rock_1_mmi)
        	var rock_2_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "rock_2.tscn", "Icosphere001")
        	$zones/main_zone/props.add_child(rock_2_mmi)
        	var rock_3_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "rock_3.tscn", "Icosphere002")
        	$zones/main_zone/props.add_child(rock_3_mmi)
        	var trashcan_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "trashcan.tscn", "trashcan")
        	$zones/main_zone/props.add_child(trashcan_mmi)
        	var fence_icosphere_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "fence.tscn", "Icosphere")
        	$zones/main_zone/props.add_child(fence_icosphere_mmi)
        	var fence_icosphere2_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/props, "fence.tscn", "Icosphere2")
        	$zones/main_zone/props.add_child(fence_icosphere2_mmi)
        	
        	calico_water_ld_mat = preload("res://Assets/Materials/blue.tres")
        	calico_water_hd_mat = preload("res://Assets/Shaders/extreme_water_main.tres")
        	calico_water_mmis = []
        	var extreme_water_small_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "main")
        	$zones/main_zone/props.add_child(extreme_water_small_mmi)
        	calico_water_mmis.append(extreme_water_small_mmi)
        	var extreme_water_small_sand_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "sand")
        	$zones/main_zone/props.add_child(extreme_water_small_sand_mmi)
        	var extreme_water_small_fade_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "sand/fade")
        	$zones/main_zone/props.add_child(extreme_water_small_fade_mmi)
        	var extreme_water_small_fade2_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "sand/fade2")
        	$zones/main_zone/props.add_child(extreme_water_small_fade2_mmi)
        	var extreme_water_small_fade3_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "sand/fade3")
        	$zones/main_zone/props.add_child(extreme_water_small_fade3_mmi)
        	var extreme_water_small_fade4_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "sand/fade4")
        	$zones/main_zone/props.add_child(extreme_water_small_fade4_mmi)
        	var extreme_water_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/ocean_water, "extreme_water.tscn", "main")
        	$zones/main_zone/props.add_child(extreme_water_mmi)
        	calico_water_mmis.append(extreme_water_mmi)
        	var extreme_water_sand_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/ocean_water, "extreme_water.tscn", "sand")
        	$zones/main_zone/props.add_child(extreme_water_sand_mmi)
        	var extreme_water_fade_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/ocean_water, "extreme_water.tscn", "fade")
        	$zones/main_zone/props.add_child(extreme_water_fade_mmi)
        	var extreme_water_fade2_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/ocean_water, "extreme_water.tscn", "fade2")
        	$zones/main_zone/props.add_child(extreme_water_fade2_mmi)
        	var extreme_water_fade3_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/ocean_water, "extreme_water.tscn", "fade3")
        	$zones/main_zone/props.add_child(extreme_water_fade3_mmi)
        	var extreme_water_fade4_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/ocean_water, "extreme_water.tscn", "fade4")
        	$zones/main_zone/props.add_child(extreme_water_fade4_mmi)
        	
        	OptionsMenu.connect("_options_update", self, "calico_water_mat_check")
        	print("[calico] Mesh instances complete!")
        
        func calico_build_mesh_parented_static_body_mmi(parent):
        	var mmi = MultiMeshInstance.new()
        	var mm = MultiMesh.new()
        	mmi.multimesh = mm
        	mm.mesh = parent.get_child(0).mesh.duplicate()
        	for surface_idx in range(parent.get_child(0).get_surface_material_count()):
        		var material = parent.get_child(0).get_surface_material(surface_idx)
        		mm.mesh.surface_set_material(surface_idx, material)
        	mm.transform_format = 1
        	mm.instance_count = parent.get_child_count()
        	var i = 0
        	for tree in parent.get_children():
        		mm.set_instance_transform(i, tree.global_transform)
        		i += 1
        		for child in tree.get_children():
        			if child is StaticBody:
        				var old_global_transform = child.global_transform.scaled(Vector3.ONE)
        				tree.remove_child(child)
        				parent.add_child(child)
        				child.global_transform = old_global_transform
        		parent.remove_child(tree)
        	return mmi
        
        func calico_get_all_children_with_filename(parent, filename):
        	var matching_children = []
        	for child in calico_get_all_children(parent):
        		if child.filename.ends_with(filename):
        			matching_children.append(child)
        	return matching_children
        
        func calico_get_children_with_prefix(parent, prefix):
        	var matching_children = []
        	for child in parent.get_children():
        		if child.name.begins_with(prefix):
        			matching_children.append(child)
        	return matching_children
        	
        func calico_get_all_children(node: Node):
        	var children = []
        	for child in node.get_children():
        		children.append(child)
        		children.append_array(calico_get_all_children(child))
        	return children
        
        func calico_build_node_parented_static_body_mmi(parent, filename, mesh_node_name):
        	var mmi = MultiMeshInstance.new()
        	var mm = MultiMesh.new()
        	mmi.multimesh = mm
        	var children = calico_get_all_children_with_filename(parent, filename)
        	var mesh_instance = children[0].get_node(mesh_node_name)
        	mm.mesh = mesh_instance.mesh.duplicate()
        	for surface_idx in range(mesh_instance.mesh.get_surface_count()):
        		var material = mesh_instance.get_active_material(surface_idx)
        		mm.mesh.surface_set_material(surface_idx, material)
        	mm.transform_format = MultiMesh.TRANSFORM_3D
        	mm.instance_count = children.size()
        	var i = 0
        	for mesh_parent in children:
        		var mesh = mesh_parent.get_node(mesh_node_name)
        		var new_transform = mesh.global_transform
        		mm.set_instance_transform(i, new_transform)
        		for mesh_child in mesh.get_children():
        			if mesh_child is StaticBody:
        				var old_hitbox_transform = mesh_child.global_transform
        				mesh.remove_child(mesh_child)
        				mesh_parent.add_child(mesh_child)
        				mesh_child.global_transform = old_hitbox_transform
        		mesh.queue_free()
        		for child in mesh_parent.get_children():
        			if child.name == "shadow": child.queue_free()
        		i += 1
        	return mmi
        
        func calico_water_mat_check():
        	var use_ld_mat = PlayerData.player_options.water == 0
        	for mmi in calico_water_mmis:
        		calico_update_mmi_surface_material(mmi, calico_water_ld_mat if use_ld_mat else calico_water_hd_mat)
        
        func calico_update_mmi_surface_material(mmi: MultiMeshInstance, material: Material):
        	var mm = mmi.multimesh
        	for i in mm.instance_count:
        		mm.mesh.surface_set_material(i, material)

        """);

    private static readonly IEnumerable<Token> SetZone = ScriptTokenizer.Tokenize(
	    """
	    
	    if calico_zones.empty():
	    	calico_zones["main_zone"] = $zones/main_zone
	    	calico_zones["tent_zone"] = $zones/tent_zone
	    	calico_zones["hub_building_zone"] = $zones/hub_building_zone
	    	calico_zones["aquarium_zone"] = $zones/aquarium_zone
	    	calico_zones["tutorial_zone"] = $zones/tutorial_zone
	    	calico_zones["island_tiny_zone"] = $zones/island_tiny_zone
	    	calico_zones["island_med_zone"] = $zones/island_med_zone
	    	calico_zones["island_big_zone"] = $zones/island_big_zone
	    	calico_zones["void_zone"] = $zones.get_node("void_zone")
	    for child in $zones.get_children():
	    	if child.name != "main_zone":
	    		$zones.remove_child(child)
	    if id != "main_zone":
	    	$zones.add_child(calico_zones[id])
	    
	    """, 1);
    
    private static readonly IEnumerable<Token> GetZone = ScriptTokenizer.Tokenize(
	    """
	    
	    if calico_zones.has(id): return calico_zones[id]
	    
	    """, 1);
    
    private static readonly IEnumerable<Token> DynamicZonesGlobals = ScriptTokenizer.Tokenize(
	    """
	    
	    var calico_zones = {}

	    """);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Map/main_map.gdc";
    
    private static IEnumerable<Token> ModifyForDynamicZones(IModInterface mod, string path, IEnumerable<Token> tokens)
    {
	    MultiTokenWaiter extendsWaiter = new([
		    t => t.Type is PrExtends,
		    t => t.Type is Identifier,
		    t => t.Type is Newline
	    ]);
	    MultiTokenWaiter setZoneWaiter = new([
		    t => t.Type is PrFunction,
		    t => t is IdentifierToken { Name: "_set_zone" },
		    t => t.Type is ParenthesisOpen,
		    t => t is IdentifierToken { Name: "id" },
		    t => t.Type is ParenthesisClose,
		    t => t.Type is Colon
	    ]);
	    MultiTokenWaiter getZoneWaiter = new([
		    t => t.Type is PrFunction,
		    t => t is IdentifierToken { Name: "_get_zone" },
		    t => t.Type is ParenthesisOpen,
		    t => t is IdentifierToken { Name: "id" },
		    t => t.Type is ParenthesisClose,
		    t => t.Type is Colon
	    ]);

	    mod.Logger.Information($"[calico.MainMapScriptMod] Patching for dynamic zones {path}");
	    
	    var patchFlags = new Dictionary<string, bool>
	    {
		    ["dynamic_zones_globals"] = false,
		    ["set_zone"] = false,
		    ["get_zone"] = false
	    };
	    
	    foreach (var t in tokens)
	    {
		    if (extendsWaiter.Check(t))
		    {
			    yield return t;
			    foreach (var t1 in DynamicZonesGlobals)
				    yield return t1;
			    patchFlags["dynamic_zones_globals"] = true;
			    mod.Logger.Information("[calico.MainMapScriptMod] dynamic_zones_globals patch OK");
		    }
		    else if (setZoneWaiter.Check(t))
		    {
			    yield return t;
			    mod.Logger.Information(string.Join("\n", SetZone));
			    foreach (var t1 in SetZone)
				    yield return t1;
			    patchFlags["set_zone"] = true;
			    mod.Logger.Information("[calico.MainMapScriptMod] set_zone patch OK");
		    }
		    else if (getZoneWaiter.Check(t))
		    {
			    yield return t;
			    foreach (var t1 in GetZone)
				    yield return t1;
			    patchFlags["get_zone"] = true;
			    mod.Logger.Information("[calico.MainMapScriptMod] get_zone patch OK");
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
			    mod.Logger.Error($"[calico.MainMapScriptMod] FAIL: {patch.Key} patch not applied");
		    }
	    }
    }

    private static IEnumerable<Token> ModifyForMeshGpuInstancing(IModInterface mod, string path, IEnumerable<Token> tokens)
    {
	    MultiTokenWaiter extendsWaiter = new([
		    t => t.Type is PrExtends,
		    t => t.Type is Identifier,
		    t => t.Type is Newline
	    ]);

	    mod.Logger.Information($"[calico.MainMapScriptMod] Patching {path}");
	    
	    var patchFlags = new Dictionary<string, bool>
	    {
		    ["globals"] = false
	    };
		
	    foreach (var t in tokens)
	    {
		    if (extendsWaiter.Check(t))
		    {
			    yield return t;
			    foreach (var t1 in Globals)
				    yield return t1;
			    patchFlags["globals"] = true;
			    mod.Logger.Information("[calico.MainMapScriptMod] globals patch OK");
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
			    mod.Logger.Error($"[calico.MainMapScriptMod] FAIL: {patch.Key} patch not applied");
		    }
	    }
    }

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
    {
	    var currentTokens = tokens.ToList();

	    mod.Logger.Information(
		    $"[calico.MainMapScriptMod] MeshGpuInstancingEnabled={config.MeshGpuInstancingEnabled}");
	    if (config.MeshGpuInstancingEnabled)
		    currentTokens = ModifyForMeshGpuInstancing(mod, path, currentTokens).ToList();

	    mod.Logger.Information(
		    $"[calico.MainMapScriptMod] DynamicZoneLoadingEnabled={config.DynamicZoneLoadingEnabled}");
	    if (config.DynamicZoneLoadingEnabled)
		    currentTokens = ModifyForDynamicZones(mod, path, currentTokens).ToList();

	    return currentTokens;
    }
}