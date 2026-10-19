using System.IO;
using Server.Engines.Pathing.Cache;
using Server.Tests.Terrain;
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

        const int probeId = TestTiles.Wall;
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

    /// <summary>
    /// The other half of the contract: the fingerprint does change when the file does, so a client patch
    /// that shifts one tile flag invalidates a bake made against the old file.
    /// </summary>
    [Fact]
    public void TileDataFileHash_ChangesWhenOneFlagChanges()
    {
        var directory = Directory.CreateTempSubdirectory("modernuo-fingerprint-").FullName;
        try
        {
            var original = SyntheticTileData.WriteTo(Directory.CreateDirectory(Path.Combine(directory, "a")).FullName);
            var rewritten = SyntheticTileData.WriteTo(Directory.CreateDirectory(Path.Combine(directory, "b")).FullName);
            var patched = SyntheticTileData.WriteTo(
                Directory.CreateDirectory(Path.Combine(directory, "c")).FullName,
                flags => flags[TestTiles.Wall] ^= TileFlag.NoShoot
            );

            Assert.Equal(StepCacheFile.HashTileDataFile(original), StepCacheFile.HashTileDataFile(rewritten));
            Assert.NotEqual(StepCacheFile.HashTileDataFile(original), StepCacheFile.HashTileDataFile(patched));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
