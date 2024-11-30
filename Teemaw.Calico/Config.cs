using System.Text.Json.Serialization;

namespace Teemaw.Calico;

public class Config
{
    [JsonInclude] public bool DynamicZonesEnabled = false;
    [JsonInclude] public bool MeshGpuInstancingEnabled = true;
    [JsonInclude] public bool MultiThreadNetworkingEnabled = true;
    [JsonInclude] public bool PlayerOptimizationsEnabled = true;
    [JsonInclude] public bool ReducePhysicsUpdatesEnabled = true;
    [JsonInclude] public bool SmoothCameraEnabled = true;
    
    public override string ToString()
    {
        return $"DynamicZonesEnabled={DynamicZonesEnabled}, " + 
               $"MeshGpuInstancingEnabled={MeshGpuInstancingEnabled}, " +
               $"MultiThreadNetworkingEnabled={MultiThreadNetworkingEnabled}, " +
               $"PlayerOptimizationsEnabled={PlayerOptimizationsEnabled}, " +
               $"ReducePhysicsUpdatesEnabled={ReducePhysicsUpdatesEnabled}, " +
               $"SmoothCameraEnabled={SmoothCameraEnabled}";
    }
}