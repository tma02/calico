using GDWeave;

namespace Teemaw.Calico;

public class Mod : IMod {
    public Mod(IModInterface modInterface) {
        var config = modInterface.ReadConfig<Config>();
        if (config.MultiThreadNetworkingEnabled)
        {
            modInterface.RegisterScriptMod(new SteamNetworkScriptMod(modInterface));
        }
        if (config.PlayerOptimizationsEnabled || config.PhysicsHalfSpeedEnabled)
        {
            modInterface.RegisterScriptMod(new PlayerScriptMod(modInterface, config));
        }
        if (config.PlayerOptimizationsEnabled)
        {
            modInterface.RegisterScriptMod(new GuitarStringSoundScriptMod(modInterface));
        }
        if (config.MeshGpuInstancingEnabled)
        {
            modInterface.RegisterScriptMod(new MainMapScriptMod(modInterface));
        }
        if (config.PhysicsHalfSpeedEnabled)
        {
            modInterface.RegisterScriptMod(new GlobalsScriptMod(modInterface));
        }
    }

    public void Dispose() {
        // We don't have anything to clean up
    }
}
