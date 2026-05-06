using System;
using Server.Logging;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Periodic backstop that enforces the StaticWalkabilityCache resident-chunk cap.
/// Steady-state cost is a single early-return; only fires real work when the cache
/// has overflowed MaxResidentChunks. Runs on the game thread; no locking required.
/// </summary>
public class CacheEvictionTimer : Timer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CacheEvictionTimer));

    public static TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(60);

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
            "CacheEvictionTimer configured: cap-check every {Sweep}s",
            SweepInterval.TotalSeconds
        );
    }

    private CacheEvictionTimer() : base(SweepInterval, SweepInterval) { }

    protected override void OnTick()
    {
        StaticWalkabilityCache.Instance.EnforceLruCap();
    }
}
