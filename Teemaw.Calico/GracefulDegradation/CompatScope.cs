namespace Teemaw.Calico.GracefulDegradation;

public enum CompatScope
{
    /// <summary>
    /// Conflicts with multithreaded networking.
    /// </summary>
    MULTITHREAD_NETWORKING,
    /// <summary>
    /// Conflicts with the player camera update process.
    /// </summary>
    CAMERA_PHYSICS,
}