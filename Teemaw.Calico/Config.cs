using GDWeave;
using static Teemaw.Calico.GracefulDegradation.CompatScope;
using static Teemaw.Calico.GracefulDegradation.ModConflictCatalog;

namespace Teemaw.Calico;

public class Config(IModInterface mi, ConfigFileSchema configFile)
{
    public bool DynamicZonesEnabled => configFile.DynamicZonesEnabled;
    public bool LobbyQolEnabled => configFile.LobbyQolEnabled;
    public bool MapSoundOptimizationsEnabled => configFile.MapSoundOptimizationsEnabled;
    public bool MeshGpuInstancingEnabled => configFile.MeshGpuInstancingEnabled;

    public bool MultiThreadNetworkingEnabled => configFile.MultiThreadNetworkingEnabled
                                                && (NoConflicts(mi, MULTITHREAD_NETWORKING)
                                                    || configFile.ZzCompatOverrideMayCauseCrash);

    public bool PlayerOptimizationsEnabled => configFile.PlayerOptimizationsEnabled;

    public bool ReducePhysicsUpdatesEnabled => configFile.ReducePhysicsUpdatesEnabled;

    public bool SmoothCameraEnabled => configFile.SmoothCameraEnabled
                                       && (NoConflicts(mi, CAMERA_PHYSICS)
                                           || configFile.ZzCompatOverrideMayCauseCrash);

    public bool ZzCompatOverrideMayCauseCrash => configFile.ZzCompatOverrideMayCauseCrash;

    public override string ToString()
    {
        return $"DynamicZonesEnabled={DynamicZonesEnabled}, " +
               $"LobbyQolEnabled={LobbyQolEnabled}, " +
               $"MapSoundOptimizationsEnabled={MapSoundOptimizationsEnabled}, " +
               $"MeshGpuInstancingEnabled={MeshGpuInstancingEnabled}, " +
               $"MultiThreadNetworkingEnabled={MultiThreadNetworkingEnabled}, " +
               $"PlayerOptimizationsEnabled={PlayerOptimizationsEnabled}, " +
               $"ReducePhysicsUpdatesEnabled={ReducePhysicsUpdatesEnabled}, " +
               $"SmoothCameraEnabled={SmoothCameraEnabled}, " +
               $"ZzCompatOverrideMayCauseCrash={ZzCompatOverrideMayCauseCrash}";
    }
}