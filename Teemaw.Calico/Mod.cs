using GDWeave;
using Teemaw.Calico.ScriptMod;

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
            modInterface.RegisterScriptMod(GuitarStringSoundScriptModFactory.Create(modInterface));
            modInterface.RegisterScriptMod(new HeldItemScriptMod(modInterface));
            modInterface.RegisterScriptMod(SoundManagerScriptModFactory.Create(modInterface));
            modInterface.RegisterScriptMod(ActorScriptModFactory.Create(modInterface));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(new Fishing3ScriptMod(modInterface));
        }

        if (config.MeshGpuInstancingEnabled || config.DynamicZonesEnabled)
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
            modInterface.RegisterScriptMod(TailRootScriptModFactory.Create(modInterface));
        }

        if (config.DynamicZonesEnabled)
        {
            modInterface.RegisterScriptMod(TransitionZoneScriptModFactory.Create(modInterface));
        }

        if (config.MapSoundOptimizationsEnabled)
        {
            modInterface.RegisterScriptMod(BushParticleDetectScriptModFactory.Create(modInterface));
        }
    }

    public void Dispose()
    {
        // We don't have anything to clean up
    }
}