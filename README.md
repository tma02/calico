# Calico

Calico; anti-lag improvements & client optimizations for WEBFISHING.

## About

Calico improves the performance of WEBFISHING in multiplayer scenarios. In lobbies with ~15 players we've seen a 
framerate of 40-60+ FPS (frametimes went from ~60ms to ~15ms). The bulk of this performance comes from introducing
threading to the network and player animation processing.

### Improvements & optimizations
* Dedicated thread for compression and sending packets
* Dedicated thread for receiving and decompressing packets
* Dedicated thread per player for processing animation calculations
* Filtering of movement packets for players/props (actors) that are not in motion
* Filtering of cosmetic loading for players that have not changed
* Other minor improvements to player handling

## Building

To build the project, you need to set the `GDWeavePath` environment variable to your game install's GDWeave directory (e.g. `G:\games\steam\steamapps\common\WEBFISHING\GDWeave`). This can also be done in Rider with `File | Settings | Build, Execution, Deployment | Toolset and Build | MSBuild global properties` or with a .user file in Visual Studio.
