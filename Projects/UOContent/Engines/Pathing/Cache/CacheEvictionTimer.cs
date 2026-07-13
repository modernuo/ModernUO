using System;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Periodic backstop that enforces <see cref="StepCache"/>'s resident-chunk cap. Costs a
/// single early-return unless the cache has overflowed MaxResidentChunks.
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

    private CacheEvictionTimer() : base(TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(120)) { }

    protected override void OnTick()
    {
        StepCache.Instance.EnforceLruCap();
    }
}
