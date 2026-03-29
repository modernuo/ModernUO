namespace BuildTool.Interactive;

/// <summary>
/// Manages cancellation for interactive mode.
/// When Ctrl+C is pressed, the current token is cancelled, causing prompts to throw OperationCanceledException.
/// The token source is then reset so the next prompt can work.
/// </summary>
public sealed class InteractiveCancellation : IDisposable
{
    private CancellationTokenSource _cts = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the current cancellation token to pass to prompts.
    /// </summary>
    public CancellationToken Token
    {
        get
        {
            lock (_lock)
            {
                return _cts.Token;
            }
        }
    }

    /// <summary>
    /// Signals cancellation (called from Ctrl+C handler).
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            _cts.Cancel();
        }
    }

    /// <summary>
    /// Resets the cancellation token source so new prompts can work.
    /// Call this after catching OperationCanceledException.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    /// <summary>Global instance for app-wide interactive cancellation.</summary>
    public static InteractiveCancellation Instance { get; } = new();

    public void Dispose()
    {
        _cts.Dispose();
    }
}
