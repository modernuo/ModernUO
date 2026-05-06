namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Outcome categories for StaticWalkabilityCache.TryGetMask. Used for telemetry
/// and to drive the slow-path fallthrough decision in callers.
/// </summary>
public enum CacheHitKind : byte
{
    Hit = 0,                         // clean default-walker answer from the resident chunk
    Miss_NotBuilt = 1,               // chunk wasn't resident; built and returned
    Miss_DirtyRebuild = 2,           // version mismatch; rebuilt and returned
    Fallthrough_MultiZ = 3,          // cell has multiple walkable surfaces; caller must use slow path
    Fallthrough_OffMap = 4,          // out of bounds
    Fallthrough_SourceZMismatch = 5, // |loc.Z - BakedSourceZ| > StepHeight; cache answer would diverge
}
