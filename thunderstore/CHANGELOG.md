## 0.4.0

* Added loading screen timeout.
    * This will get you past the loading screen when you're unable to connect to another player in the lobby. 
* Added map sound optimizations.
    * This detaches inactive audio player nodes from the scene tree.
* Optimized actor signals with `PlayerOptimizationsEnabled`.
* Optimized player sound effects and guitar audio with `PlayerOptimizationsEnabled`.
    * This detaches inactive audio player nodes from the scene tree. 

## 0.3.4

* Fixed changing accessories with `PlayerOptimizationsEnabled`.

## 0.3.3

* Fixed camera jitter when player is rotated on the X axis (pitch) with `SmoothCameraEnabled`.
* Improved compatibility with some system environments.

## 0.3.2

* Fixed camera jitter when player scale changes with `SmoothCameraEnabled`.

## 0.3.1

* Fixed the direction of player rotation on the Z axis (roll) with `SmoothCameraEnabled`.
    * This fixes the rotation of the player when using Ragdoll for example.
* Improved linearity of the camera with `SmoothCameraEnabled`.
    * This should help with player movement feeling strange/off. If it still feels strange, v-sync may also help.

## 0.3.0

* Added dynamic zones
    * Zones are dynamically detached and attached to the scene tree as you move between them.
    * This is currently disabled by default in the config
* Improved compatibility with other mods that hook into _controlled_process
    * This fixes compatibility with YAAM
* Fixed a crash when `PlayerOptimizationsEnabled` is disabled and `SmoothCameraEnabled` is enabled.
* Fixed the direction of player rotation on the X axis (pitch) with `SmoothCameraEnabled`.
    * This fixes the rotation of the player when using Flyfishing for example.
* Minor changes to `PlayerOptimizationsEnabled`

## 0.2.1

* Fixed some rocks/objects not having collision with `MeshGpuInstancingEnabled`.
* Fixed water not respecting the game's quality setting with `MeshGpuInstancingEnabled`.
* Fixed player tail attachment jitter with `ReducePhysicsUpdatesEnabled` enabled.
    * As of this version, tail attachment is now patched by `SmoothCameraEnabled`. You will need `SmoothCameraEnabled`
      to enable the fix.
* Fixed player rotation speed with `ReducePhysicsUpdatesEnabled`.
* Fixed dialog cooldown speed with `ReducePhysicsUpdatesEnabled`.
* Fixed player size scaling with `SmoothCameraEnabled`.
* Fixed compatibility with SmoothCam with `SmoothCameraEnabled` __set to `false`__.
    * `SmoothCameraEnabled` remains incompatible with SmoothCam.
* Removed a debug log. Sorry for polluting your GDWeave logs!

## 0.2.0

* Added smooth camera mode -- highly recommended if reducing physics updates
* Added water meshes to GPU instancing
* Fixed compatibility with BetterCosmeticDefaulter
* Fixed face animation duration with reduced physics updates

## 0.1.1

* Initial release
