namespace BuildTool.Interactive;

/// <summary>
/// Tracks Ctrl+C press timing to detect double-press for exit.
/// </summary>
public sealed class CancellationTracker
{
    private DateTime _lastCancelTime = DateTime.MinValue;
    private const int DoublePressMs = 500;

    /// <summary>
    /// Checks if this is a double-press (within 500ms of last press).
    /// Also updates the last press time.
    /// </summary>
    public bool IsDoublePress()
    {
        var now = DateTime.UtcNow;
        var isDouble = (now - _lastCancelTime).TotalMilliseconds < DoublePressMs;
        _lastCancelTime = now;
        return isDouble;
    }

    /// <summary>Global instance for app-wide tracking.</summary>
    public static CancellationTracker Instance { get; } = new();
}
