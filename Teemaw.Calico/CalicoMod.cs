using System.Reflection;
using GDWeave;
using Teemaw.Calico.ScriptMod;
using Teemaw.Calico.ScriptMod.GracefulDegradation;
using Teemaw.Calico.ScriptMod.LobbyQol;

namespace Teemaw.Calico;

public class CalicoMod : IMod
{
    public CalicoMod(IModInterface mi)
    {
        mi.Logger.Information($"[calico.Mod] Version is {GetAssemblyVersion()}");

        var configFile = mi.ReadConfig<ConfigFileSchema>();
        var config = new Config(mi, configFile);

        mi.Logger.Information($"[calico.Mod] Loaded config was   {configFile}");
        mi.Logger.Information($"[calico.Mod] Running with config {config}");

        if (config.ZzCompatOverrideMayCauseCrash)
        {
            mi.Logger.Warning("[calico.Mod] WARNING! WARNING! WARNING!");
            mi.Logger.Warning("[calico.Mod] ZzCompatOverrideMayCauseCrash=True, MAY CAUSE CRASH!");
        }

        mi.RegisterScriptMod(GracefulDegradationSplashScriptModFactory.Create(mi, config, configFile));
        mi.RegisterScriptMod(new SteamNetworkScriptMod(mi, config));
        mi.RegisterScriptMod(PlayerHeadHudScriptModFactory.Create(mi, config));
        mi.RegisterScriptMod(TailRootScriptModFactory.Create(mi, config));

        if (config.PlayerOptimizationsEnabled || config.ReducePhysicsUpdatesEnabled || config.SmoothCameraEnabled)
        {
            mi.RegisterScriptMod(PlayerScriptModFactory.Create(mi, config));
        }

        if (config.PlayerOptimizationsEnabled)
        {
            mi.RegisterScriptMod(GuitarStringSoundScriptModFactory.Create(mi));
            mi.RegisterScriptMod(HeldItemScriptModFactory.Create(mi));
            mi.RegisterScriptMod(SoundManagerScriptModFactory.Create(mi));
            mi.RegisterScriptMod(ActorScriptModFactory.Create(mi));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            mi.RegisterScriptMod(Fishing3ScriptModFactory.Create(mi));
        }

        if (config.MeshGpuInstancingEnabled || config.DynamicZonesEnabled)
        {
            mi.RegisterScriptMod(MainMapScriptModFactory.Create(mi, config));
        }

        if (config.ReducePhysicsUpdatesEnabled)
        {
            mi.RegisterScriptMod(GlobalsScriptModFactory.Create(mi));
            mi.RegisterScriptMod(PlayerFaceScriptModFactory.Create(mi));
            mi.RegisterScriptMod(PlayerHudScriptModFactory.Create(mi));
        }

        if (config.DynamicZonesEnabled)
        {
            mi.RegisterScriptMod(TransitionZoneScriptModFactory.Create(mi));
        }

        if (config.MapSoundOptimizationsEnabled)
        {
            mi.RegisterScriptMod(BushParticleDetectScriptModFactory.Create(mi));
        }

        if (config.LobbyQolEnabled)
        {
            mi.RegisterScriptMod(LobbyQolSteamNetworkScriptModFactory.Create(mi, config));
            mi.RegisterScriptMod(LobbyQolMainMenuScriptModFactory.Create(mi));
            mi.RegisterScriptMod(LobbyQolEscMenuScriptModFactory.Create(mi));
            mi.RegisterScriptMod(LobbyQolPlayerScriptModFactory.Create(mi));
            mi.RegisterScriptMod(LobbyQolPlayerlistScriptModFactory.Create(mi));
            mi.RegisterScriptMod(LobbyQolPlayerEntryScriptModFactory.Create(mi));
        }
    }

    public void Dispose()
    {
        // We don't have anything to clean up
    }

    public static string GetAssemblyVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion != null ? attribute.InformationalVersion : "unknown";
    }
}