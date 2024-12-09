﻿using System.Text.Json.Serialization;

namespace Teemaw.Calico;

public class Config
{
    [JsonInclude] public bool DynamicZonesEnabled = true;
    [JsonInclude] public bool LoadingWaitTimeoutEnabled = true;
    [JsonInclude] public bool LobbyIdsEnabled = false;
    [JsonInclude] public bool MapSoundOptimizationsEnabled = true;
    [JsonInclude] public bool MeshGpuInstancingEnabled = true;
    [JsonInclude] public bool MultiThreadNetworkingEnabled = true;
    [JsonInclude] public bool PlayerOptimizationsEnabled = true;
    [JsonInclude] public bool ReducePhysicsUpdatesEnabled = true;
    [JsonInclude] public bool SmoothCameraEnabled = true;
    
    public override string ToString()
    {
        return $"DynamicZonesEnabled={DynamicZonesEnabled}, " + 
               $"LoadingWaitTimeoutEnabled={LoadingWaitTimeoutEnabled}, " +
               $"LobbyIdsEnabled={LobbyIdsEnabled}, " +
               $"MapSoundOptimizationsEnabled={MapSoundOptimizationsEnabled}, " +
               $"MeshGpuInstancingEnabled={MeshGpuInstancingEnabled}, " +
               $"MultiThreadNetworkingEnabled={MultiThreadNetworkingEnabled}, " +
               $"PlayerOptimizationsEnabled={PlayerOptimizationsEnabled}, " +
               $"ReducePhysicsUpdatesEnabled={ReducePhysicsUpdatesEnabled}, " +
               $"SmoothCameraEnabled={SmoothCameraEnabled}";
    }
}