# Calico

Calico; anti-lag improvements & client optimizations for WEBFISHING.

## About

Calico aims to improve the performance of WEBFISHING in multiplayer scenarios. This is achieved by introducing threading
to networking code, as well as numerous other optimizations.

### Improvements & optimizations
* Dedicated thread for compression and sending packets
* Dedicated thread for receiving and decompressing packets
* Skipping cosmetics loading for players that have not changed
* Optimize player animation updates
* Optimize player guitar processing (this happens even if you're not playing a guitar)
* Other player optimizations

## Install

1. Download the release zip
2. Unzip into your WEBFISHING directory such that the `Teemaw.Calico` folder ends up in `WEBFISHING\GDWeave\mods\`
3. Optionally edit the configuration file in `WEBFISHING\GDWeave\configs\Teemaw.Calico.json`
4. You're done!

## Configuration
The `Teemaw.Calico.json` configuration file has the following schema and default values:
```json
{
  "NetworkPatchEnabled": true,
  "PlayerPatchEnabled": true,
  "PhysicsPatchEnabled": false,
  "RemoveDisconnectedPlayerProps": true
}
```

### `NetworkPatchEnabled`

This enables patching of the networking script to use threads for sending and receiving packets.

File modified:
* `res://Scenes/Singletons/SteamNetwork.gdc`

### `PlayerPatchEnabled`

This enables patching of player scripts to optimize cosmetic loading, animation updates, guitar processing, and more.

Files modified:
* `res://Scenes/Entities/Player/player.gdc`
* `res://Scenes/Entities/Player/guitar_string_sound.gdc`

### `PhysicsPatchEnabled`

This enables patching of actor scripts to run only half of the scripted physics process.

Files modified:
* All scripts under `res://Scenes/Entities/` EXCEPT:
    * `player.gdc`
    * `actor.gdc`
    * `prop.gdc`

### `RemoveDisconnectedPlayerProps`

This enables patching of the world script to remove player spawned props when they disconnect.

File modified:
* `res://Scenes/World/world.gdc`
