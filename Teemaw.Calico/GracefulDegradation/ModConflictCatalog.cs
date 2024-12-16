using GDWeave;
using static Teemaw.Calico.GracefulDegradation.CompatScope;

namespace Teemaw.Calico.GracefulDegradation;

public static class ModConflictCatalog
{
    private static readonly Dictionary<CompatScope, string[]> KnownConflicts = new()
    {
        { MULTITHREAD_NETWORKING, ["Meepso.NLag"] },
        { CAMERA_UPDATE, ["hideri.SmoothCam"] }
    };

    /// <summary>
    /// Returns a list of loaded mods that have a known conflict with the given scope.
    /// </summary>
    /// <param name="mi"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    public static string[] GetLoadedConflicts(IModInterface mi, CompatScope scope)
    {
        var knownConflicts = KnownConflicts[scope];
        return knownConflicts.Union(mi.LoadedMods).ToArray();
    }

    /// <summary>
    /// Checks that there are no loaded conflicting mods for the provided scope.
    /// </summary>
    /// <param name="mi"></param>
    /// <param name="scope"></param>
    /// <returns>
    /// True if there is at least one loaded mod which conflicts with the provided scope.
    /// </returns>
    public static bool NoConflicts(IModInterface mi, CompatScope scope)
    {
        return GetLoadedConflicts(mi, scope).Length == 0;
    }
}