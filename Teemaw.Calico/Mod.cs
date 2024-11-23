using GDWeave;

namespace Teemaw.Calico;

public class Mod : IMod {

    public Mod(IModInterface modInterface) {
        modInterface.RegisterScriptMod(new SteamNetworkScriptMod(modInterface));
        modInterface.Logger.Information("Registered Steam network script");
        modInterface.RegisterScriptMod(new PhysicsProcessScriptMod(modInterface));
        modInterface.Logger.Information("Registered physics patches");
    }

    public void Dispose() {
        // Cleanup anything you do here
    }
}
