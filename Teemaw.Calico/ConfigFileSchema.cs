using System.Text.Json.Serialization;

namespace Teemaw.Calico;

public class ConfigFileSchema
{
    [JsonInclude] public bool DynamicZonesEnabled = true;
    [JsonInclude] public bool LoadingWaitTimeoutEnabled = true;
    [JsonInclude] public bool LobbyQolEnabled = true;
    [JsonInclude] public bool MapSoundOptimizationsEnabled = true;
    [JsonInclude] public bool MeshGpuInstancingEnabled = true;
    [JsonInclude] public bool MultiThreadNetworkingEnabled = true;
    [JsonInclude] public bool PlayerOptimizationsEnabled = true;
    [JsonInclude] public bool ReducePhysicsUpdatesEnabled = false;
    [JsonInclude] public bool SmoothCameraEnabled = true;
    [JsonInclude] public bool ZzCompatOverrideMayCauseCrash = false;

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
               $"SmoothCameraEnabled={SmoothCameraEnabled}, " +
               $"ZzCompatOverrideMayCauseCrash={ZzCompatOverrideMayCauseCrash}";
    }

    public bool AnyEnabled() =>
        DynamicZonesEnabled || LoadingWaitTimeoutEnabled || LobbyQolEnabled || MapSoundOptimizationsEnabled
        || MeshGpuInstancingEnabled || MultiThreadNetworkingEnabled || PlayerOptimizationsEnabled
        || ReducePhysicsUpdatesEnabled || SmoothCameraEnabled;
}