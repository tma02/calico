using GDWeave;

namespace Teemaw.Calico;

public class Mod : IMod
{
    public Mod(IModInterface modInterface)
    {
        var config = modInterface.ReadConfig<Config>();
        
        modInterface.Logger.Information($"[calico.Mod] Running with config {config}");
        
        if (config.MultiThreadNetworkingEnabled)
        {
            modInterface.RegisterScriptMod(new SteamNetworkScriptMod(modInterface));
        }

        if (config.PlayerOptimizationsEnabled || config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(new PlayerScriptMod(modInterface, config));
        }

        if (config.PlayerOptimizationsEnabled)
        {
            modInterface.RegisterScriptMod(new GuitarStringSoundScriptMod(modInterface));
            modInterface.RegisterScriptMod(new HeldItemScriptMod(modInterface));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(new Fishing3ScriptMod(modInterface));
        }

        if (config.MeshGpuInstancingEnabled)
        {
            modInterface.RegisterScriptMod(new MainMapScriptMod(modInterface));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(new GlobalsScriptMod(modInterface));
        }
    }

    public void Dispose()
    {
        // We don't have anything to clean up
    }
}