﻿using System.Text.Json.Serialization;

namespace Teemaw.Calico;

public class Config
{
    [JsonInclude] public bool NetworkPatchEnabled = true;
    [JsonInclude] public bool PlayerPatchEnabled = true;
    [JsonInclude] public bool PhysicsPatchEnabled = true;
    [JsonInclude] public bool RemoveDisconnectedPlayerProps = true;
}