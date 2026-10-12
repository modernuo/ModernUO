using System;
using System.IO;
using System.IO.Compression;

namespace Server.BotAI.V2;

/// <summary>
/// The reach map's clusters kept on disk between boots, so a restart reads a few megabytes on a worker instead of drawing
/// every tile of the map again on the loop.
///
/// <para>
/// <b>Keyed by the map files, checked by the world (01.10.2026).</b> The file carries the engine's fingerprint of the map's own
/// files (<c>TileMatrix.MapFilesFingerprint</c>) and is refused when the client's map changes. What the map files cannot say —
/// a house raised, a decoration moved — each cluster carries as its <see cref="BotReachCluster.Signature"/>, and a boot draws
/// again only the clusters whose houses and items no longer match. Under <c>Data/</c>, never under <c>Saves/</c>: the saves folder
/// is moved into the backups at every save.
/// </para>
///
/// <para>
/// Layout (little-endian, the whole of it deflated): magic, format, map id, fingerprint, width, height, clusters across and
/// down; then per cluster a flag byte (drawn, housed, one class for all its tiles, layers, shore), its signature, its tiles
/// (one byte when all alike), its regions and standing tiles, its layers, its shore bits and its links.
/// </para>
/// </summary>
public static class BotReachFile
{
    private const uint Magic = 0x31484352; // "RCH1"

    private const int Format = 1;

    private const byte Drawn = 1;

    private const byte HousedFlag = 2;

    private const byte Uniform = 4;

    private const byte HasLayers = 8;

    private const byte HasCoast = 16;

    public static string PathFor(Map map) =>
        Path.Combine(Core.BaseDirectory, "Data", $"bot-reach-{map.Name.ToLowerInvariant()}.bin");

    /// <summary>Writes the clusters. Off the loop: the clusters are never changed once made, and nothing here reads the world.</summary>
    public static long Write(string path, int mapId, ulong fingerprint, int width, int height, int clustersX, int clustersY, BotReachCluster[] clusters)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = path + ".tmp";

        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var zip = new ZLibStream(file, CompressionLevel.Fastest);
            using var w = new BinaryWriter(zip);

            w.Write(Magic);
            w.Write(Format);
            w.Write(mapId);
            w.Write(fingerprint);
            w.Write(width);
            w.Write(height);
            w.Write(clustersX);
            w.Write(clustersY);

            for (var c = 0; c < clusters.Length; c++)
            {
                var cluster = clusters[c];

                if (cluster == null)
                {
                    w.Write((byte)0);

                    continue;
                }

                var uniform = true;

                for (var i = 1; i < 256 && uniform; i++)
                {
                    uniform = cluster.Cells[i] == cluster.Cells[0];
                }

                var flags = (byte)(Drawn | (cluster.Housed ? HousedFlag : 0) | (uniform ? Uniform : 0)
                                   | (cluster.Layers != null ? HasLayers : 0) | (cluster.Coast != null ? HasCoast : 0));

                w.Write(flags);
                w.Write(cluster.Signature);

                if (uniform)
                {
                    w.Write(cluster.Cells[0]);
                }
                else
                {
                    w.Write(cluster.Cells);
                }

                w.Write((short)cluster.Regions);
                w.Write((short)cluster.Standing);

                if (cluster.Layers != null)
                {
                    w.Write(cluster.Layers.Length);

                    for (var i = 0; i < cluster.Layers.Length; i++)
                    {
                        w.Write(cluster.Layers[i]);
                    }
                }

                if (cluster.Coast != null)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        w.Write(cluster.Coast[i]);
                    }
                }

                var links = cluster.Links ?? [];

                w.Write(links.Length);

                for (var i = 0; i < links.Length; i++)
                {
                    w.Write(links[i]);
                }
            }
        }

        File.Move(temp, path, true);

        return new FileInfo(path).Length;
    }

    /// <summary>
    /// Reads the clusters, or null with the reason when the file is missing, of another format, of another map, or of map files
    /// that have changed since. Off the loop.
    /// </summary>
    public static BotReachCluster[] Read(string path, int mapId, ulong fingerprint, int width, int height, out string why)
    {
        why = null;

        if (!File.Exists(path))
        {
            why = "no file yet";

            return null;
        }

        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZLibStream(file, CompressionMode.Decompress);
        using var r = new BinaryReader(zip);

        if (r.ReadUInt32() != Magic || r.ReadInt32() != Format)
        {
            why = "another format";

            return null;
        }

        if (r.ReadInt32() != mapId || r.ReadUInt64() != fingerprint)
        {
            why = "the map files have changed";

            return null;
        }

        if (r.ReadInt32() != width || r.ReadInt32() != height)
        {
            why = "another size of map";

            return null;
        }

        var clustersX = r.ReadInt32();
        var clustersY = r.ReadInt32();
        var clusters = new BotReachCluster[clustersX * clustersY];

        for (var c = 0; c < clusters.Length; c++)
        {
            var flags = r.ReadByte();

            if ((flags & Drawn) == 0)
            {
                continue;
            }

            var cluster = new BotReachCluster { Signature = r.ReadInt64(), Housed = (flags & HousedFlag) != 0 };

            if ((flags & Uniform) != 0)
            {
                var cells = new byte[256];

                Array.Fill(cells, r.ReadByte());
                cluster.Cells = BotReachCluster.Shared(cells);
            }
            else
            {
                cluster.Cells = r.ReadBytes(256);
            }

            cluster.Regions = r.ReadInt16();
            cluster.Standing = r.ReadInt16();

            if ((flags & HasLayers) != 0)
            {
                var layers = new int[r.ReadInt32()];

                for (var i = 0; i < layers.Length; i++)
                {
                    layers[i] = r.ReadInt32();
                }

                cluster.Layers = layers;
            }

            if ((flags & HasCoast) != 0)
            {
                cluster.Coast = new ulong[4];

                for (var i = 0; i < 4; i++)
                {
                    cluster.Coast[i] = r.ReadUInt64();
                }
            }

            var links = new int[r.ReadInt32()];

            for (var i = 0; i < links.Length; i++)
            {
                links[i] = r.ReadInt32();
            }

            cluster.Links = links;
            clusters[c] = cluster;
        }

        return clusters;
    }
}
