using System.Text.Json.Serialization;

namespace Teemaw.Calico;

public class Config
{
    [JsonInclude] public bool MeshGpuInstancingEnabled = true;
    [JsonInclude] public bool MultiThreadNetworkingEnabled = true;
    [JsonInclude] public bool PhysicsHalfSpeedEnabled = true;
    [JsonInclude] public bool PlayerOptimizationsEnabled = true;
}