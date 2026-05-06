using System;
using Server.Logging;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Periodic timer that walks the StaticWalkabilityCache and evicts chunks
/// whose owning sector has been deactivated past the grace period. Runs on
/// the game thread; no locking required.
/// </summary>
public class CacheEvictionTimer : Timer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CacheEvictionTimer));

    public static TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(5);
    public static TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(30);

    private static CacheEvictionTimer _instance;

    public static void Configure()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = new CacheEvictionTimer();
        _instance.Start();

        logger.Information(
            "CacheEvictionTimer configured: sweep every {Sweep}s, grace {Grace}s",
            SweepInterval.TotalSeconds, GracePeriod.TotalSeconds
        );
    }

    private CacheEvictionTimer() : base(SweepInterval, SweepInterval) { }

    protected override void OnTick()
    {
        StaticWalkabilityCache.Instance.RunEvictionSweep(GracePeriod);
    }
}
