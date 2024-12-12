using GDWeave;

namespace Teemaw.Calico.Util;

public static class WeaveUtil
{
    public static bool IsModLoaded(IModInterface modInterface, string modName) =>
        modInterface.LoadedMods.Contains(modName);
}