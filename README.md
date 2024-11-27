# Calico

Calico; anti-lag improvements & client optimizations for WEBFISHING.

## About

Calico aims to improve the performance of WEBFISHING in multiplayer scenarios. This is achieved by introducing threading
to networking code, as well as numerous other optimizations.

### Improvements & optimizations

* Dedicated thread for compression and sending packets
* Dedicated thread for receiving and decompressing packets
* GPU instancing of common meshes (all trees, logs, bushes, mushrooms, etc.)
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
  "MeshGpuInstancingEnabled": true,
  "MultiThreadNetworkingEnabled": true,
  "PlayerOptimizationsEnabled": true,
  "ReducePhysicsUpdatesEnabled": true
}
```

### `MeshGpuInstancingEnabled`

This enables patching of the `main_map` script to generate GPU instanced meshes before unloading the individual meshes.

File modified:

* `res://Scenes/Map/main_map.gdc`

### `MultiThreadNetworkingEnabled`

This enables patching of the networking script to use threads for sending and receiving packets.

File modified:

* `res://Scenes/Singletons/SteamNetwork.gdc`

### `PlayerOptimizationsEnabled`

This enables patching of player scripts to optimize cosmetic loading, animation updates, guitar processing, etc.

Files modified:

* `res://Scenes/Entities/Player/guitar_string_sound.gdc`
* `res://Scenes/Entities/Player/held_item.gdc`
* `res://Scenes/Entities/Player/player.gdc`

### `ReducePhysicsUpdatesEnabled`

This enables patching of a few scripts to reduce the physics update rate. This will patch a few processes that are tied
to the old physics update rate such that they feel the same with a reduced update rate.

Files modified:

* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/Minigames/Fishing3/fishing3.gdc`
* `res://Scenes/Singletons/globals.gdc`

> [!NOTE]  
> There are many game processes which are tied to the physics update rate. If you see animations being slow or weird,
> this is probably why. Like any issue you may have with the mod, please feel free to create an issue or PR.

## Building

To build the project, you need to set the `GDWeavePath` environment variable to your game install's GDWeave directory (
e.g. `G:\games\steam\steamapps\common\WEBFISHING\GDWeave`). This can also be done in Rider with
`File | Settings | Build, Execution, Deployment | Toolset and Build | MSBuild global properties` or with a .user file in
Visual Studio.
