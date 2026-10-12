using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The remembered roads of one map kept on disk between boots, beside the graph's own file (<see cref="NavGraphFile"/>), so
/// that a restart forgets nothing the shard has walked (<see cref="NavRouteMemory"/>).
///
/// <para>
/// <b>Written and read off the game loop.</b> What is written is a list of arrays that never change once a road is made
/// (<see cref="NavRouteMemory.Records"/>), taken on the loop and written by a worker; a file is read by a worker into the
/// same form and laid into the memory on the loop (<see cref="NavRouteMemory.Adopt"/>). Nothing in the file is trusted: a
/// road is proved against the graph by its clusters' signatures the first time it is used, and the map files it was drawn
/// from are fingerprinted as the graph's are — a file of other map files is not read at all.
/// </para>
/// </summary>
public static class NavRouteFile
{
    private const uint Magic = 0x5256414E; // "NAVR"

    private const int Format = 1;

    public static string PathFor(Map map) =>
        Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", $"navroutes-{map.MapID}.nvr");

    public static void Write(string path, ulong fingerprint, int width, int height, IReadOnlyList<NavRouteRecord> routes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = path + ".tmp";

        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
        using (var w = new BinaryWriter(stream))
        {
            w.Write(Magic);
            w.Write(Format);
            w.Write(fingerprint);
            w.Write(width);
            w.Write(height);
            w.Write(routes.Count);

            for (var i = 0; i < routes.Count; i++)
            {
                var r = routes[i];
                var n = r.X.Length;

                w.Write(r.Goal);
                w.Write(r.Uses);
                w.Write(n);

                for (var t = 0; t < n; t++)
                {
                    w.Write(r.X[t]);
                    w.Write(r.Y[t]);
                    w.Write(r.Z[t]);
                    w.Write(r.S[t]);
                    w.Write(r.Cum[t]);
                }

                w.Write(r.RunCluster.Length);

                for (var k = 0; k < r.RunCluster.Length; k++)
                {
                    w.Write(r.RunCluster[k]);
                    w.Write(r.RunStart[k]);
                    w.Write(r.RunSig[k]);
                }
            }
        }

        File.Move(temp, path, true);
    }

    /// <summary>The roads in a file, or null when there is none, it is of another format, or of other map files.</summary>
    public static List<NavRouteRecord> Read(string path, ulong fingerprint, int width, int height)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        using var r = new BinaryReader(stream);

        if (r.ReadUInt32() != Magic || r.ReadInt32() != Format || r.ReadUInt64() != fingerprint
            || r.ReadInt32() != width || r.ReadInt32() != height)
        {
            return null;
        }

        var count = r.ReadInt32();

        if (count < 0 || count > 1 << 20)
        {
            return null;
        }

        var routes = new List<NavRouteRecord>(count);

        for (var i = 0; i < count; i++)
        {
            var goal = r.ReadInt32();
            var uses = r.ReadInt32();
            var n = r.ReadInt32();

            if (n <= 0 || n > 1 << 16)
            {
                return null;
            }

            var record = new NavRouteRecord
            {
                Goal = goal,
                Uses = uses,
                X = new short[n],
                Y = new short[n],
                Z = new sbyte[n],
                S = new byte[n],
                Cum = new int[n]
            };

            for (var t = 0; t < n; t++)
            {
                record.X[t] = r.ReadInt16();
                record.Y[t] = r.ReadInt16();
                record.Z[t] = r.ReadSByte();
                record.S[t] = r.ReadByte();
                record.Cum[t] = r.ReadInt32();
            }

            var runs = r.ReadInt32();

            if (runs <= 0 || runs > n)
            {
                return null;
            }

            record.RunCluster = new int[runs];
            record.RunStart = new int[runs + 1];
            record.RunSig = new ulong[runs];

            for (var k = 0; k < runs; k++)
            {
                record.RunCluster[k] = r.ReadInt32();
                record.RunStart[k] = r.ReadInt32();
                record.RunSig[k] = r.ReadUInt64();
            }

            record.RunStart[runs] = n;
            routes.Add(record);
        }

        return routes;
    }

    /// <summary>A file's roads, or an empty list with the reason when it cannot be read. For the worker that reads it at boot.</summary>
    public static List<NavRouteRecord> TryRead(string path, ulong fingerprint, int width, int height, out string error)
    {
        error = null;

        try
        {
            return Read(path, fingerprint, width, height);
        }
        catch (Exception e)
        {
            error = e.Message;

            return null;
        }
    }
}
