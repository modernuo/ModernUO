namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Snapshot of <see cref="StepCache"/>'s counters, as returned by GetStats() and reported by
/// the [PathCacheStats command. Every counter is monotonic except ResidentChunks, and all of
/// them reset on Clear() — they count since the last clear, not since startup.
/// </summary>
public readonly struct CacheStats(
    int residentChunks,
    long hits,
    long missesNotBuilt,
    long missesDirtyRebuild,
    long fallthroughMultiZ,
    long fallthroughOffMap,
    long fallthroughSourceZMismatch,
    long fallthroughNotBuilt,
    long fallthroughMulti,
    long multiLocalHits,
    long multiMaskCacheHits,
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
    public readonly long FallthroughNotBuilt = fallthroughNotBuilt;
    public readonly long FallthroughMulti = fallthroughMulti;
    public readonly long MultiLocalHits = multiLocalHits;
    public readonly long MultiMaskCacheHits = multiMaskCacheHits;
    public readonly long EvictionsByLruCap = evictionsByLruCap;
    public readonly long BuildsTotal = buildsTotal;
}
