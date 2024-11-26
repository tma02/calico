using GDWeave;

namespace Teemaw.Calico;

public class Mod : IMod {
    public Mod(IModInterface modInterface) {
        var config = modInterface.ReadConfig<Config>();
        if (config.NetworkPatchEnabled)
        {
            modInterface.RegisterScriptMod(new SteamNetworkScriptMod(modInterface));
            modInterface.Logger.Information("Registered Steam network script patches");
        }
        if (config.PhysicsPatchEnabled)
        {
            modInterface.RegisterScriptMod(new PhysicsProcessScriptMod(modInterface));
            modInterface.Logger.Information("Registered physics patches");
        }
        if (config.PlayerPatchEnabled)
        {
            modInterface.RegisterScriptMod(new PlayerScriptMod(modInterface));
            modInterface.RegisterScriptMod(new GuitarStringSoundScriptMod());
            modInterface.Logger.Information("Registered Player script patches");
        }
        if (config.RemoveDisconnectedPlayerProps)
        {
            modInterface.RegisterScriptMod(new RemoveDisconnectedPlayerPropsScriptMod());
            modInterface.Logger.Information("Registered remove disconnected player props patches");
        }
    }

    public void Dispose() {
        // We don't have anything to clean up
    }
}
