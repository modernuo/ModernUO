namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Outcome of <see cref="StepCache.TryGetMask"/>. Drives both telemetry and the caller's
/// decision to fall back to the slow path. Ordering is load-bearing: 0-2 are usable answers,
/// 3+ are fallthroughs, and <see cref="StepMask.IsHit"/> tests that boundary.
/// </summary>
public enum CacheHitKind : byte
{
    Hit = 0,                         // served from the resident chunk
    Miss_NotBuilt = 1,               // chunk wasn't resident; built and returned
    Miss_DirtyRebuild = 2,           // chunk was stale; rebuilt and returned
    Fallthrough_MultiZ = 3,          // stacked walkable surfaces, none matching the query Z
    Fallthrough_OffMap = 4,          // out of bounds
    Fallthrough_SourceZMismatch = 5, // |query Z - baked SourceZ| > StepHeight; a cached answer would diverge
    Fallthrough_NotBuilt = 6,        // first touch of an unbuilt chunk; the promotion gate defers the build
    Fallthrough_Multi = 7,           // a multi (house/boat) covers this cell or its halo
}
