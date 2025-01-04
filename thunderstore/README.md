# Calico

Calico; anti-lag improvements & client optimizations for WEBFISHING.

## About

Calico aims to improve the performance of WEBFISHING in multiplayer scenarios.

### Improvements & optimizations

* Dedicated thread for compression and sending packets
* Dedicated thread for receiving and decompressing packets
* GPU instancing of common meshes (all trees, logs, bushes, mushrooms, etc.)
* Smoothing camera panning/motion by decoupling it from physics updates
* Reducing physics update rate
* Skipping cosmetics loading for players that have not changed
* Optimize player animation updates
* Other player processing optimizations (caught item, held item, sound effects)
* QoL features
  * Grant other users moderation permissions to kick, ban, and unban
  * Lobby IDs which cannot be spoofed for joining lobbies
* Other bug fixes

## Install

### Thunderstore (Recommended)

Visit the [Thunderstore page for Calico](https://thunderstore.io/c/webfishing/p/Teemaw/Calico/).

### Manual

1. Download the latest [release](https://github.com/tma02/calico/releases/latest) zip
2. Unzip into your WEBFISHING directory such that the `Teemaw.Calico` folder ends up in `WEBFISHING\GDWeave\mods\`
3. Optionally edit the configuration file in `WEBFISHING\GDWeave\configs\Teemaw.Calico.json`
4. You're done!

## Configuration

The `Teemaw.Calico.json` configuration file has the following schema and default values:

```json
{
  "DynamicZonesEnabled": true,
  "LobbyQolEnabled": true,
  "MapSoundOptimizationsEnabled": true,
  "MeshGpuInstancingEnabled": true,
  "MultiThreadNetworkingEnabled": true,
  "PlayerOptimizationsEnabled": true,
  "ReducePhysicsUpdatesEnabled": false,
  "SmoothCameraEnabled": true,
  "ZzCompatOverrideMayCauseCrash": false
}
```

### `DynamicZonesEnabled`

Normally, the game will load and hold all zones (shop, aquarium, islands, etc.) in the scene tree regardless of where
your character currently is. Enabling this will dynamically attach and detach zones from the scene tree as you move
between them.

Files modified:

* `res://Scenes/Map/main_map.gdc`
* `res://Scenes/Map/Tools/transition_zone.gdc`

### `LobbyQolEnabled`

This brings a few QoL features to lobbies:
* Peer moderators - hosts running Calico can grant other Calico users permissions to kick, ban, and unban.
* Lobby IDs - unique to each lobby and cannot be spoofed.
  * A new button in the Esc menu lets you see and copy this ID.
  * Other players with Calico installed can join with this ID.
* Fixes a bug in vanilla where player names are blank in some places.

Files modified:

* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/HUD/Esc Menu/esc_menu.gdc`
* `res://Scenes/HUD/Playerlist/player_entry.gd`
* `res://Scenes/HUD/Playerlist/playerlist.gd`
* `res://Scenes/Menus/Main Menu/main_menu.gdc`
* `res://Scenes/Singletons/SteamNetwork.gdc`

### `MapSoundOptimizationsEnabled`

This enables optimizations relating to sound effects of the map.

File modified:

* `res://Scenes/Map/Props/bush_particle_detect.gdc`

### `MeshGpuInstancingEnabled`

Enabling this will reduce the number of GPU draw calls by combining calls for multiple copies of the same mesh into a
single call. Game objects which currently benefit from this include trees, bushes, mushrooms, water, etc.

File modified:

* `res://Scenes/Map/main_map.gdc`

### `MultiThreadNetworkingEnabled`

This enables dedicated send and receive threads for sending and reading network packets. Packet compression and
decompression are also offloaded from the main thread.

File modified:

* `res://Scenes/Singletons/SteamNetwork.gdc`

### `PlayerOptimizationsEnabled`

This enables patching of player scripts to optimize cosmetic loading, animation updates, sound effects, etc.

Files modified:

* `res://Scenes/Entities/actor.gdc`
* `res://Scenes/Entities/Player/guitar_string_sound.gdc`
* `res://Scenes/Entities/Player/held_item.gdc`
* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/Entities/Player/sound_manager.gdc`

### `ReducePhysicsUpdatesEnabled` (Disabled by default)

> [!IMPORTANT]  
> It's highly recommended to set `SmoothCameraEnabled` to `true` if this option is enabled. Normally, the game camera's
> movement is tied to the physics update rate. If you enable reduced physics updates without smooth camera, it may feel
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
if your framerate is faster than the physics update rate. Normally, the game runs physics at 60Hz. Without this option
enabled, it may feel like your game is locked to 60fps during camera panning or player movement. This is much more
noticeable with the reduced physics update rate of `ReducePhysicsUpdatesEnabled`, so is highly recommended to be enabled
along with `ReducePhysicsUpdatesEnabled`.

Files modified:

* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/Entities/Player/player_headhud.gdc`
* `res://Scenes/Entities/Player/tail_root.gdc`

### `ZzCompatOverrideMayCauseCrash`

By default, Calico will check your loaded mods list against a list of known incompatible mods. This allows Calico to
disable certain patches that would have caused a crash. However, Calico cannot know if a previously conflicting mod has
been updated with a fix.

If a previously conflicting mod has been fixed, setting this option to `true` will override the compatibility checks.
Calico will then faithfully respect the patches enabled in your config.

Files modified when set to `false`:

* `res://Scenes/Menus/Splash/splash.gdc`

(There are no modified files when set to `true`.)

## Troubleshooting

If Calico is causing your game to crash, it's likely that there is a conflict with another mod. Here are some general
guidelines to help get your game running by disabling some of Calico's features.

> [!TIP]  
> If you are experiencing a conflict using Calico with some other mod, feel free to open an
> [issue](https://github.com/tma02/calico/issues/new/choose) or PR. While we work on a fix, try the following.

### I have a guitar mod
Try disabling `PlayerOptimizationsEnabled`.

### I have a fishing mod
Try disabling `ReducePhysicsUpdatesEnabled`.

### I have a custom map
Try disabling `DynamicZonesEnabled`, `MeshGpuInstancingEnabled`, `MapSoundOptimizationsEnabled`, or some combination of
the above.

### I have other camera mods
Try disabling `SmoothCameraEnabled`, `ReducePhysicsUpdatesEnabled`, or both.

### I have other networking related mods
Try disabling `MultiThreadNetworkingEnabled`.

## Building

To build the project, you need to set the `GDWeavePath` environment variable to your game install's GDWeave directory (
e.g. `G:\games\steam\steamapps\common\WEBFISHING\GDWeave`). This can also be done in Rider with
`File | Settings | Build, Execution, Deployment | Toolset and Build | MSBuild global properties` or with a .user file in
Visual Studio.
