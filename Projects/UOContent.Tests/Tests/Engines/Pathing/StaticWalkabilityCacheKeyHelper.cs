using Server.Engines.Pathing.Cache;

namespace Server.Tests.Pathfinding;

/// <summary>
/// Test-only re-export of StaticWalkabilityCache.EncodeKey (which is internal to UOContent).
/// </summary>
internal static class StaticWalkabilityCacheKeyHelper
{
    public static long EncodeKey(int mapId, int chunkX, int chunkY) =>
        StaticWalkabilityCache.EncodeKey(mapId, chunkX, chunkY);
}
