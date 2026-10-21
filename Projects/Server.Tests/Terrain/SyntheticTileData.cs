using System;
using System.IO;
using System.Text;

namespace Server.Tests.Terrain;

/// <summary>
/// Writes <see cref="TestTiles"/> as a pre-HS tiledata.mul, the smallest layout <see cref="TileData"/> reads:
/// 0x4000 land records of 26 bytes and 0x4000 item records of 37 bytes, with a 4-byte header before every 32.
/// The file must stay under 1,644,544 bytes or TileData.Load picks the 7.0 layout.
/// </summary>
public static class SyntheticTileData
{
    private const int LandCount = 0x4000;
    private const int ItemCount = 0x4000;
    private const int NameLength = 20;

    public static byte[] Build(Action<TileFlag[]> mutateItemFlags = null)
    {
        var landFlags = new TileFlag[LandCount];
        var landNames = new string[LandCount];
        for (var i = 0; i < TestTiles.Land.Count; i++)
        {
            var tile = TestTiles.Land[i];
            landFlags[tile.Id] = tile.Flags;
            landNames[tile.Id] = tile.Name;
        }

        var itemFlags = new TileFlag[ItemCount];
        var itemNames = new string[ItemCount];
        var itemHeights = new byte[ItemCount];
        for (var i = 0; i < TestTiles.Items.Count; i++)
        {
            var tile = TestTiles.Items[i];
            itemFlags[tile.Id] = tile.Flags;
            itemNames[tile.Id] = tile.Name;
            itemHeights[tile.Id] = tile.Height;
        }

        mutateItemFlags?.Invoke(itemFlags);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        for (var i = 0; i < LandCount; i++)
        {
            if ((i & 0x1F) == 0)
            {
                writer.Write(0); // header
            }

            writer.Write((uint)landFlags[i]);
            writer.Write((short)0); // texture
            WriteName(writer, landNames[i]);
        }

        for (var i = 0; i < ItemCount; i++)
        {
            if ((i & 0x1F) == 0)
            {
                writer.Write(0); // header
            }

            writer.Write((uint)itemFlags[i]);
            writer.Write((byte)0);  // weight
            writer.Write((byte)0);  // quality
            writer.Write((ushort)0); // animation
            writer.Write((byte)0);
            writer.Write((byte)0);  // quantity
            writer.Write(0);
            writer.Write((byte)0);
            writer.Write((byte)0);  // value
            writer.Write(itemHeights[i]);
            WriteName(writer, itemNames[i]);
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Writes tiledata.mul into <paramref name="directory"/> and returns its path.</summary>
    public static string WriteTo(string directory, Action<TileFlag[]> mutateItemFlags = null)
    {
        var path = Path.Combine(directory, "tiledata.mul");
        File.WriteAllBytes(path, Build(mutateItemFlags));
        return path;
    }

    /// <summary>Makes <paramref name="directory"/> the only data directory and loads its tiledata.mul.</summary>
    public static void LoadInto(string directory)
    {
        ServerConfiguration.DataDirectories.Clear();
        ServerConfiguration.DataDirectories.Add(directory);
        TileData.Load();
    }

    private static void WriteName(BinaryWriter writer, string name)
    {
        Span<byte> buffer = stackalloc byte[NameLength];
        buffer.Clear();
        if (name != null)
        {
            Encoding.ASCII.GetBytes(name, buffer);
        }

        writer.Write(buffer);
    }
}
