using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class StepCacheFingerprintTests
{
    /// <summary>
    /// Regression: the cache fingerprint must hash the on-disk tiledata.mul, NOT the mutable
    /// in-memory <see cref="TileData"/> tables. The server patches item flags/heights at runtime
    /// (ItemFixes, LOSBlocker, PotionKeg, CTF, ...) at nondeterministic lifecycle points, so a
    /// fingerprint taken over the live tables depended on WHEN it was computed: a runtime
    /// [PathBake stamped one value into the .swb and the next startup's Initialize() recomputed a
    /// different one, marking the bake stale and re-baking on every boot. Hashing the file makes
    /// the fingerprint a pure function of the client's tile data, immune to those mutations.
    /// </summary>
    [Fact]
    public void Fingerprint_IgnoresRuntimeTileDataMutation()
    {
        const int mapId = 1; // Trammel — loaded by the test bootstrap.

        var before = StepCacheFile.ComputeFingerprint(mapId);

        const int probeId = 0x2A0;
        var original = TileData.ItemTable[probeId].Flags;
        try
        {
            // Mutate an in-memory item flag the way ItemFixes/CTF/etc. do at runtime. XOR
            // guarantees the value actually changes regardless of the current flag state.
            TileData.ItemTable[probeId].Flags ^= TileFlag.NoShoot;
            Assert.NotEqual(original, TileData.ItemTable[probeId].Flags); // sanity: mutation took

            var after = StepCacheFile.ComputeFingerprint(mapId);

            Assert.Equal(before, after);
        }
        finally
        {
            TileData.ItemTable[probeId].Flags = original;
        }
    }
}
