using GDWeave;
using static Teemaw.Calico.GracefulDegradation.CompatScope;
using static Teemaw.Calico.GracefulDegradation.ModConflictCatalog;

namespace Teemaw.Calico;

public class Config(IModInterface mi, ConfigFileSchema configFile)
{
    public bool DynamicZonesEnabled => configFile.DynamicZonesEnabled;
    public bool LoadingWaitTimeoutEnabled => configFile.LoadingWaitTimeoutEnabled;
    public bool LobbyQolEnabled => configFile.LobbyQolEnabled;
    public bool MapSoundOptimizationsEnabled => configFile.MapSoundOptimizationsEnabled;
    public bool MeshGpuInstancingEnabled => configFile.MeshGpuInstancingEnabled;

    public bool MultiThreadNetworkingEnabled => configFile.MultiThreadNetworkingEnabled &&
                                                NoConflicts(mi, MULTITHREAD_NETWORKING);

    public bool PlayerOptimizationsEnabled => configFile.PlayerOptimizationsEnabled;
    public bool ReducePhysicsUpdatesEnabled => configFile.ReducePhysicsUpdatesEnabled &&
                                               NoConflicts(mi, CAMERA_PHYSICS);

    public bool SmoothCameraEnabled => configFile.SmoothCameraEnabled &&
                                       NoConflicts(mi, CAMERA_PHYSICS);
    
    public override string ToString()
    {
        return $"DynamicZonesEnabled={DynamicZonesEnabled}, " +
               $"LoadingWaitTimeoutEnabled={LoadingWaitTimeoutEnabled}, " +
               $"LobbyQolEnabled={LobbyQolEnabled}, " +
               $"MapSoundOptimizationsEnabled={MapSoundOptimizationsEnabled}, " +
               $"MeshGpuInstancingEnabled={MeshGpuInstancingEnabled}, " +
               $"MultiThreadNetworkingEnabled={MultiThreadNetworkingEnabled}, " +
               $"PlayerOptimizationsEnabled={PlayerOptimizationsEnabled}, " +
               $"ReducePhysicsUpdatesEnabled={ReducePhysicsUpdatesEnabled}, " +
               $"SmoothCameraEnabled={SmoothCameraEnabled}";
    }
}