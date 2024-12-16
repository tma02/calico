using System.Reflection;
using GDWeave;
using static Teemaw.Calico.GracefulDegradation.CompatScope;

namespace Teemaw.Calico.GracefulDegradation;

public static class ModConflictCatalog
{
    private static readonly Dictionary<CompatScope, string[]> KnownConflicts = new()
    {
        { MULTITHREAD_NETWORKING, ["Meepso.NLag"] },
        { CAMERA_PHYSICS, ["hideri.SmoothCam"] }
    };

    private static readonly Dictionary<CompatScope, string[]> CompatScopeFeatures = new()
    {
        { MULTITHREAD_NETWORKING, ["MultiThreadNetworkingEnabled"] },
        { CAMERA_PHYSICS, ["SmoothCameraEnabled", "ReducePhysicsUpdatesEnabled"] }
    };

    private static readonly Dictionary<string, CompatScope[]> FeatureCompatScopes = new()
    {
        { "MultiThreadNetworkingEnabled", [MULTITHREAD_NETWORKING] },
        { "SmoothCameraEnabled", [CAMERA_PHYSICS] },
        { "ReducePhysicsUpdatesEnabled", [CAMERA_PHYSICS] }
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

    public static bool AnyConflicts(IModInterface mi, ConfigFileSchema configFile)
    {
        return FeatureCompatScopes.Where(kv =>
                configFile.GetType().GetField(kv.Key)?.GetValue(configFile) is true)
            .SelectMany(kv => kv.Value)
            .Distinct()
            .Any(scope => !NoConflicts(mi, scope));
    }

    public static string GetConflictMessage(IModInterface mi, ConfigFileSchema configFile)
    {
        var conflicts = (from scope in FeatureCompatScopes
                .Where(kv =>
                    configFile.GetType().GetField(kv.Key)?.GetValue(configFile) is true)
                .SelectMany(kv => kv.Value)
                .Distinct()
                .Where(scope => !NoConflicts(mi, scope))
            let possession = CompatScopeFeatures[scope].Length > 1 ? "have" : "has"
            select $"[{string.Join(", ", CompatScopeFeatures[scope])}]\n{possession} " +
                   $"been disabled due to: " +
                   $"[{string.Join(", ", GetLoadedConflicts(mi, scope))}].").ToList();

        return $"Due to known mod conflicts, Calico could not\npatch certain features which " +
               $"you have enabled in the config.\n{string.Join("\n", conflicts)}\nTo hide this " +
               $"message, either disable the conflicting mods,\nor disable the conflicting " +
               $"features in Calico's config.";
    }
}