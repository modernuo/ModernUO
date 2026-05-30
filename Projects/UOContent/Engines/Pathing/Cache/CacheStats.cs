namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Snapshot of StaticWalkabilityCache counters. Returned by GetStats() and consumed
/// by the [PathCacheStats admin command. All counters are monotonic except ResidentChunks.
/// </summary>
public readonly struct CacheStats(
    int residentChunks,
    long hits,
    long missesNotBuilt,
    long missesDirtyRebuild,
    long fallthroughMultiZ,
    long fallthroughOffMap,
    long fallthroughSourceZMismatch,
    long evictionsByLruCap,
    long buildsTotal
)
{
    public readonly int ResidentChunks = residentChunks;
    public readonly long Hits = hits;
    public readonly long MissesNotBuilt = missesNotBuilt;
    public readonly long MissesDirtyRebuild = missesDirtyRebuild;
    public readonly long FallthroughMultiZ = fallthroughMultiZ;
    public readonly long FallthroughOffMap = fallthroughOffMap;
    public readonly long FallthroughSourceZMismatch = fallthroughSourceZMismatch;
    public readonly long EvictionsByLruCap = evictionsByLruCap;
    public readonly long BuildsTotal = buildsTotal;
}
