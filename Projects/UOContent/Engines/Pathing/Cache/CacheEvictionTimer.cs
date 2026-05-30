using System;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Periodic backstop that enforces the StaticWalkabilityCache resident-chunk cap.
/// Steady-state cost is a single early-return; only fires real work when the cache
/// has overflowed MaxResidentChunks. Runs on the game thread; no locking required.
/// </summary>
public class CacheEvictionTimer : Timer
{
    private static CacheEvictionTimer _instance;

    public static void Configure()
    {
        if (_instance != null)
        {
            return;
        }

        _instance = new CacheEvictionTimer();
        _instance.Start();
    }

    private CacheEvictionTimer() : base(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)) { }

    protected override void OnTick()
    {
        StepCache.Instance.EnforceLruCap();
    }
}
