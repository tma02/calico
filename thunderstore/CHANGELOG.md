## 0.2.1

* Fixed some rocks/objects not having collision with `MeshGpuInstancingEnabled`.
* Fixed water not respecting the game's quality setting with `MeshGpuInstancingEnabled`.
* Fixed player tail attachment jitter with `ReducePhysicsUpdatesEnabled` enabled.
    * As of this version, tail attachment is now patched by `SmoothCameraEnabled`. You will need `SmoothCameraEnabled`
      to enable the fix.
* Fixed player rotation speed with `ReducePhysicsUpdatesEnabled`.
* Fixed dialog cooldown speed with `ReducePhysicsUpdatesEnabled`.
* Fixed player size scaling with `SmoothCameraEnabled`.
* Fixed compatibility with SmoothCamera with `SmoothCameraEnabled` __set to `false`__.
    * `SmoothCameraEnabled` remains incompatible with SmoothCamera.
* Removed a debug log. Sorry for polluting your GDWeave logs!

## 0.2.0

* Added smooth camera mode -- highly recommended if reducing physics updates
* Added water meshes to GPU instancing
* Fixed compatibility with BetterCosmeticDefaulter
* Fixed face animation duration with reduced physics updates

## 0.1.1

* Initial release
