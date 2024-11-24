using GDWeave;

namespace Teemaw.Calico;

public class Mod : IMod {

    public Mod(IModInterface modInterface) {
        modInterface.RegisterScriptMod(new SteamNetworkScriptMod(modInterface));
        modInterface.Logger.Information("Registered Steam network script patches");
        modInterface.RegisterScriptMod(new PhysicsProcessScriptMod(modInterface));
        modInterface.Logger.Information("Registered physics patches");
        modInterface.RegisterScriptMod(new PlayerScriptMod(modInterface));
        modInterface.Logger.Information("Registered Player script patches");
    }

    public void Dispose() {
        // Cleanup anything you do here
    }
}
