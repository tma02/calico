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

        if (config.PlayerOptimizationsEnabled || config.ReducePhysicsUpdatesEnabled || config.SmoothCameraEnabled)
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

        if (config.MeshGpuInstancingEnabled || config.DynamicZoneLoadingEnabled)
        {
            modInterface.RegisterScriptMod(new MainMapScriptMod(modInterface, config));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(new GlobalsScriptMod(modInterface));
            modInterface.RegisterScriptMod(new PlayerFaceScriptMod(modInterface));
            modInterface.RegisterScriptMod(new PlayerHudScriptMod(modInterface));
        }

        if (config.SmoothCameraEnabled)
        {
            modInterface.RegisterScriptMod(new PlayerHeadHudScriptMod(modInterface));
            modInterface.RegisterScriptMod(new TailRootScriptMod(modInterface));
        }

        if (config.DynamicZoneLoadingEnabled)
        {
            modInterface.RegisterScriptMod(new TransitionZoneScriptMod(modInterface));
        }
    }

    public void Dispose()
    {
        // We don't have anything to clean up
    }
}