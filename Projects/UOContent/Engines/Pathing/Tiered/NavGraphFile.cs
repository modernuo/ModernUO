using System;
using System.Collections.Generic;
using System.IO;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// A navigation graph kept on disk between boots, so a restart does not draw the whole map again.
///
/// <para>
/// <b>Written and read off the game loop, and never touching the world there.</b> The graph is the shard's own data, not
/// game state: a copy of its arrays is taken on the loop (<see cref="NavGraph.Snapshot"/>), written by a worker thread, and
/// a file is read by a worker thread into the same form and handed back to the loop (<see cref="NavGraph.Restore"/>).
/// The map files the graph was drawn from are fingerprinted as the step cache's own bake is
/// (<see cref="Cache.StepCacheFile.ComputeFingerprint"/>), on the loop; a file drawn from other map files is not read.
/// </para>
///
/// <para>
/// <b>Houses change between boots.</b> The file lists the clusters that had a house in or beside them when it was
/// written; on reading, those and the clusters beside any house standing now are torn down and drawn again.
/// </para>
/// </summary>
public static class NavGraphFile
{
    private const uint Magic = 0x4E415647;

    private const int Format = 1;

    public static string PathFor(Map map) =>
        Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", $"navgraph-{map.MapID}.nvg");

    public static void Write(string path, NavGraphSnapshot snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = path + ".tmp";

        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
        using (var w = new BinaryWriter(stream))
        {
            w.Write(Magic);
            w.Write(Format);
            w.Write(snapshot.Fingerprint);
            w.Write(snapshot.Width);
            w.Write(snapshot.Height);
            w.Write(snapshot.High);

            for (var i = 0; i < snapshot.High; i++)
            {
                w.Write(snapshot.X[i]);
                w.Write(snapshot.Y[i]);
                w.Write(snapshot.Z[i]);
                w.Write(snapshot.S[i]);
                w.Write(snapshot.Cluster[i]);
                w.Write(snapshot.Refs[i]);
            }

            w.Write(snapshot.Clusters.Length);

            foreach (var c in snapshot.Clusters)
            {
                if (c == null)
                {
                    w.Write((byte)0);

                    continue;
                }

                w.Write((byte)1);
                w.Write(c.Built);
                Ints(w, c.Nodes);
                Ints(w, c.InterFrom);
                Ints(w, c.InterTo);
                Ints(w, c.InterCost);
                Pairs(w, c.East);
                Pairs(w, c.South);
                Ints(w, c.Start);
                Ints(w, c.To);
                Ints(w, c.Cost);
            }

            Ints(w, snapshot.Housed);
            w.Write(snapshot.Gates);
        }

        File.Move(temp, path, true);
    }

    public static NavGraphSnapshot Read(string path, ulong fingerprint, int width, int height)
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

        var high = r.ReadInt32();
        var snapshot = new NavGraphSnapshot
        {
            Fingerprint = fingerprint,
            Width = width,
            Height = height,
            High = high,
            X = new short[high],
            Y = new short[high],
            Z = new sbyte[high],
            S = new byte[high],
            Cluster = new int[high],
            Refs = new int[high]
        };

        for (var i = 0; i < high; i++)
        {
            snapshot.X[i] = r.ReadInt16();
            snapshot.Y[i] = r.ReadInt16();
            snapshot.Z[i] = r.ReadSByte();
            snapshot.S[i] = r.ReadByte();
            snapshot.Cluster[i] = r.ReadInt32();
            snapshot.Refs[i] = r.ReadInt32();
        }

        var clusters = r.ReadInt32();

        snapshot.Clusters = new NavClusterSnapshot[clusters];

        for (var i = 0; i < clusters; i++)
        {
            if (r.ReadByte() == 0)
            {
                continue;
            }

            snapshot.Clusters[i] = new NavClusterSnapshot
            {
                Built = r.ReadBoolean(),
                Nodes = ReadInts(r),
                InterFrom = ReadInts(r),
                InterTo = ReadInts(r),
                InterCost = ReadInts(r),
                East = ReadPairs(r),
                South = ReadPairs(r),
                Start = ReadInts(r),
                To = ReadInts(r),
                Cost = ReadInts(r)
            };
        }

        snapshot.Housed = ReadInts(r);
        snapshot.Gates = r.ReadInt64();

        return snapshot;
    }

    private static void Ints(BinaryWriter w, int[] values)
    {
        w.Write(values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            w.Write(values[i]);
        }
    }

    private static int[] ReadInts(BinaryReader r)
    {
        var values = new int[r.ReadInt32()];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = r.ReadInt32();
        }

        return values;
    }

    private static void Pairs(BinaryWriter w, (int A, int B)[] pairs)
    {
        if (pairs == null)
        {
            w.Write(-1);

            return;
        }

        w.Write(pairs.Length);

        for (var i = 0; i < pairs.Length; i++)
        {
            w.Write(pairs[i].A);
            w.Write(pairs[i].B);
        }
    }

    private static (int A, int B)[] ReadPairs(BinaryReader r)
    {
        var n = r.ReadInt32();

        if (n < 0)
        {
            return null;
        }

        var pairs = new (int A, int B)[n];

        for (var i = 0; i < n; i++)
        {
            pairs[i] = (r.ReadInt32(), r.ReadInt32());
        }

        return pairs;
    }
}

/// <summary>A navigation graph's arrays, copied out of it on the loop for a worker thread to write, or read back from a file.</summary>
public sealed class NavGraphSnapshot
{
    public ulong Fingerprint;

    public int Width;

    public int Height;

    public int High;

    public short[] X;

    public short[] Y;

    public sbyte[] Z;

    public byte[] S;

    public int[] Cluster;

    public int[] Refs;

    public NavClusterSnapshot[] Clusters;

    /// <summary>Clusters that had a multi in or beside them when the snapshot was taken.</summary>
    public int[] Housed = [];

    public long Gates;
}

public sealed class NavClusterSnapshot
{
    public bool Built;

    public int[] Nodes;

    public int[] InterFrom;

    public int[] InterTo;

    public int[] InterCost;

    public (int A, int B)[] East;

    public (int A, int B)[] South;

    public int[] Start;

    public int[] To;

    public int[] Cost;
}
