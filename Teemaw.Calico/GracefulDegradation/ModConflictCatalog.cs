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

    private static readonly Dictionary<CompatScope, string[]> CompatScopeFeatures = new()
    {
        { MULTITHREAD_NETWORKING, ["MultiThreadNetworkingEnabled"] },
        { CAMERA_UPDATE, ["SmoothCameraEnabled"] }
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
        return knownConflicts.Intersect(mi.LoadedMods).ToArray();
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

    public static bool AnyConflicts(IModInterface mi)
    {
        foreach (var scope in KnownConflicts.Keys)
        {
            if (!NoConflicts(mi, scope))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetConflictMessage(IModInterface mi)
    {
        var conflicts = new List<string>();
        foreach (var scope in KnownConflicts.Keys)
        {
            if (!NoConflicts(mi, scope))
            {
                var possession = CompatScopeFeatures[scope].Length > 1 ? "have" : "has";
                conflicts.Add($"[{string.Join(", ", CompatScopeFeatures[scope])}] {possession} " +
                              $"been disabled due to:\n" +
                              $"[{string.Join(", ", GetLoadedConflicts(mi, scope))}].");
            }
        }

        return $"Due to known mod conflicts, Calico could not\npatch certain features which " +
               $"you have enabled in the config.\n{string.Join("\n", conflicts)}\nTo hide this " +
               $"message, either disable the conflicting mods,\nor disable the conflicting " +
               $"features in Calico's config.";
    }
}