using System.IO;

namespace Server.Tests.Terrain;

/// <summary>
/// Boots a test host on generated data instead of client files, so every machine and CI see the same world.
/// </summary>
public static class SyntheticWorld
{
    /// <summary>Where the generated data files are written: the test host's output, overwritten every run.</summary>
    public static string DataDirectory => Path.Combine(Core.BaseDirectory, "SyntheticData");

    /// <summary>
    /// Writes and loads the generated tiledata.mul from <see cref="DataDirectory"/>, which becomes the only data
    /// directory, and seeds the multi footprints. Run before anything reads <see cref="TileData"/>.
    /// </summary>
    public static void LoadData()
    {
        Directory.CreateDirectory(DataDirectory);
        SyntheticTileData.WriteTo(DataDirectory);
        SyntheticTileData.LoadInto(DataDirectory);
        TestMultis.Seed();
    }

    /// <summary>Stamps the prefab scenes. Run once Felucca and Trammel are registered.</summary>
    public static void StampTerrain() => Prefabs.StampAll();
}
