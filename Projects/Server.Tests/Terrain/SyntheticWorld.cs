using System.IO;

namespace Server.Tests.Terrain;

/// <summary>
/// Boots a test host on generated data instead of client files, so every machine and CI see the same world.
/// </summary>
public static class SyntheticWorld
{
    /// <summary>
    /// Writes and loads the generated tiledata.mul from a fresh temp directory, which becomes the only data
    /// directory, and seeds the multi footprints. Run before anything reads <see cref="TileData"/>.
    /// </summary>
    public static void LoadData()
    {
        var directory = Directory.CreateTempSubdirectory("modernuo-synth-").FullName;
        SyntheticTileData.WriteTo(directory);
        SyntheticTileData.LoadInto(directory);
        TestMultis.Seed();
    }

    /// <summary>Stamps the prefab scenes. Run once Felucca and Trammel are registered.</summary>
    public static void StampTerrain() => Prefabs.StampAll();
}
