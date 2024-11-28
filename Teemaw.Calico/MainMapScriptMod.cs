using GDWeave;
using GDWeave.Godot;
using GDWeave.Modding;
using static GDWeave.Godot.TokenType;

namespace Teemaw.Calico;

public class MainMapScriptMod(IModInterface mod): IScriptMod
{
	// TODO: Clean this up somehow -- this was way simpler when it was supposed to be just three tree nodes. 
    private static readonly IEnumerable<Token> Globals = ScriptTokenizer.Tokenize(
        """

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
        	var extreme_water_small_mmi = calico_build_node_parented_static_body_mmi($zones/main_zone/lake_water, "extreme_water_small.tscn", "main")
        	$zones/main_zone/props.add_child(extreme_water_small_mmi)
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
        	for tree in children:
        		var new_transform = tree.get_node(mesh_node_name).global_transform
        		mm.set_instance_transform(i, new_transform)
        		tree.get_node(mesh_node_name).queue_free()
        		for child in tree.get_children():
        			if child.name == "shadow": child.queue_free()
        		i += 1
        	return mmi

        """);
    
    public bool ShouldRun(string path) => path == "res://Scenes/Map/main_map.gdc";

    public IEnumerable<Token> Modify(string path, IEnumerable<Token> tokens)
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
}