# Calico

Calico; anti-lag improvements & client optimizations for WEBFISHING.

## About

Calico aims to improve the performance of WEBFISHING in multiplayer scenarios. This is achieved by introducing threading
to networking code, as well as numerous other optimizations.

### Improvements & optimizations

* Dedicated thread for compression and sending packets
* Dedicated thread for receiving and decompressing packets
* GPU instancing of common meshes (all trees, logs, bushes, mushrooms, etc.)
* Smoothing camera panning/motion by decoupling it from physics updates
* Reducing physics update rate
* Skipping cosmetics loading for players that have not changed
* Optimize player animation updates
* Other player processing optimizations (caught item, held item, guitar)

## Install

1. Download the release zip
2. Unzip into your WEBFISHING directory such that the `Teemaw.Calico` folder ends up in `WEBFISHING\GDWeave\mods\`
3. Optionally edit the configuration file in `WEBFISHING\GDWeave\configs\Teemaw.Calico.json`
4. You're done!

## Configuration

The `Teemaw.Calico.json` configuration file has the following schema and default values:

```json
{
  "DynamicZonesEnabled": false,
  "MeshGpuInstancingEnabled": true,
  "MultiThreadNetworkingEnabled": true,
  "PlayerOptimizationsEnabled": true,
  "ReducePhysicsUpdatesEnabled": true,
  "SmoothCameraEnabled": true
}
```

### `DynamicZonesEnabled` (Experimental)

Normally, the game will load and hold all zones (shop, aquarium, islands, etc.) in the scene tree regardless of where
your character currently is. This will dynamically attach and detach zones from the scene tree as you move between them.

Files modified:

* `res://Scenes/Map/main_map.gdc`
* `res://Scenes/Map/Tools/transition_zone.gdc`

### `MeshGpuInstancingEnabled`

This reduces the number of GPU draw calls by combining draw calls from multiple copies of the same mesh into the same
call. Game objects which currently benefit from this include trees, bushes, mushrooms, water, etc.

File modified:

* `res://Scenes/Map/main_map.gdc`

### `MultiThreadNetworkingEnabled`

This enables dedicated send and receive threads for sending and reading network packets. Packet compression and
decompression are also offloaded from the main thread to these threads.

File modified:

* `res://Scenes/Singletons/SteamNetwork.gdc`

### `PlayerOptimizationsEnabled`

This enables patching of player scripts to optimize cosmetic loading, animation updates, guitar processing, etc.

Files modified:

* `res://Scenes/Entities/Player/guitar_string_sound.gdc`
* `res://Scenes/Entities/Player/held_item.gdc`
* `res://Scenes/Entities/Player/player.gdc`

### `ReducePhysicsUpdatesEnabled`

> [!IMPORTANT]  
> It's highly recommended to set `SmoothCameraEnabled` to `true` if this option is enabled. The game camera's movement
> is normally tied to the physics update rate. If you enable reduced physics updates without smooth camera, it may feel
> like the game is running slower during camera panning or player movement. `SmoothCameraEnabled` will decouple camera
> updates from physics updates.

This reduces the physics update rate to free up CPU cycles for other tasks. This will patch a few processes that are
tied to the normal physics update rate such that they feel the same with a reduced update rate.

There are many game processes which are tied to the physics update rate. If you see animations being slow or weird,
this is probably why. Like any issue you may have with the mod, please feel free to create an issue or PR.

Files modified:

* `res://Scenes/Entities/Player/Face/player_face.gdc`
* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/HUD/playerhud.gdc`
* `res://Scenes/Minigames/Fishing3/fishing3.gdc`
* `res://Scenes/Singletons/globals.gdc`

### `SmoothCameraEnabled`

This option decouples camera position updates from the physics cycle. This will help make the game feel more responsive
if your framerate is faster than the physics update rate. Normally, the game runs physics at 60fps. Without this option
enabled, it may feel like your game is locked to 60fps during camera panning or player movement. This is much more
noticeable with the reduced physics update rate of `ReducePhysicsUpdatesEnabled`, so is highly recommended to be enabled
along with `ReducePhysicsUpdatesEnabled`.

Files modified:

* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/Entities/Player/player_headhud.gdc`
* `res://Scenes/Entities/Player/tail_root.gdc`

## Building

To build the project, you need to set the `GDWeavePath` environment variable to your game install's GDWeave directory (
e.g. `G:\games\steam\steamapps\common\WEBFISHING\GDWeave`). This can also be done in Rider with
`File | Settings | Build, Execution, Deployment | Toolset and Build | MSBuild global properties` or with a .user file in
Visual Studio.
