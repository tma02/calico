using System.Reflection;
using GDWeave;
using Teemaw.Calico.ScriptMod;

namespace Teemaw.Calico;

public class Mod : IMod
{
    public Mod(IModInterface modInterface)
    {
        modInterface.Logger.Information($"[calico.Mod] Version is {GetAssemblyVersion()}");
        
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
            modInterface.RegisterScriptMod(HeldItemScriptModFactory.Create(modInterface));
            modInterface.RegisterScriptMod(SoundManagerScriptModFactory.Create(modInterface));
            modInterface.RegisterScriptMod(ActorScriptModFactory.Create(modInterface));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(Fishing3ScriptModFactory.Create(modInterface));
        }

        if (config.MeshGpuInstancingEnabled || config.DynamicZonesEnabled)
        {
            modInterface.RegisterScriptMod(MainMapScriptModFactory.Create(modInterface, config));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            modInterface.RegisterScriptMod(GlobalsScriptModFactory.Create(modInterface));
            modInterface.RegisterScriptMod(PlayerFaceScriptModFactory.Create(modInterface));
            modInterface.RegisterScriptMod(PlayerHudScriptModFactory.Create(modInterface));
        }

        if (config.SmoothCameraEnabled)
        {
            modInterface.RegisterScriptMod(PlayerHeadHudScriptModFactory.Create(modInterface));
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
    
    private static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion != null ? attribute.InformationalVersion : "unknown";
    }
}