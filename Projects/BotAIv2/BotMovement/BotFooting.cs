using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Tiles a walk has proved nobody can stand at, kept for everybody for half an hour.
///
/// <para>
/// <b>The far side's answer was kept by the walk that asked and by nobody else.</b> When a walk has stopped closing,
/// <see cref="BotWalk"/> asks the destination's side whether any tile there takes a body, and "there is nowhere there to
/// stand" drops the walk — correctly, and for that bot only. The miner then bends to another seam, and the next miner
/// is handed the same rock by <see cref="BotOre.Find"/>, which never heard. On 22.09.2026, 18:12–19:18, twenty-five
/// miners in turn walked up to the rock at (1728, 1600, 13), one every two minutes and a half, and every one was told
/// there was nowhere to stand; the place note that might have caught it (<see cref="BotRefused"/>) is cleared by
/// anybody arriving in its eight-tile square, and the seam's other rocks were being worked all hour.
/// </para>
///
/// <para>
/// So the answer is written on the tile, for everybody, and the rock finder passes such a tile over. For half an hour
/// rather than for good: "nowhere to stand" counts what is standing there as well as the ground, and a body in the way
/// moves.
/// </para>
/// </summary>
public static class BotFooting
{
    /// <summary>How long a tile stays written off. Half an hour.</summary>
    public static int RestMs { get; set; } = 1800000;

    /// <summary>The most tiles remembered; the oldest go first past it.</summary>
    public static int Most { get; set; } = 2048;

    private static readonly Dictionary<(Map Map, int X, int Y), long> _tiles = [];

    /// <summary>Tiles written off, and rocks passed over for it.</summary>
    public static long Noted { get; private set; }

    public static long Skipped { get; private set; }

    /// <summary>A walk has just proved nobody can stand at this tile.</summary>
    public static void Note(Map map, Point3D where)
    {
        if (map == null || map == Map.Internal || where == Point3D.Zero)
        {
            return;
        }

        if (_tiles.Count >= Most)
        {
            Sweep(true);
        }

        _tiles[(map, where.X, where.Y)] = Core.TickCount;
        Noted++;
    }

    /// <summary>Whether this tile was proved footless within the last <see cref="RestMs"/>; counted when it was.</summary>
    public static bool Footless(Map map, int x, int y)
    {
        if (_tiles.Count == 0 || !_tiles.TryGetValue((map, x, y), out var at))
        {
            return false;
        }

        if (Core.TickCount - at >= RestMs)
        {
            _tiles.Remove((map, x, y));

            return false;
        }

        Skipped++;

        return true;
    }

    private static readonly List<(Map, int, int)> _stale = [];

    private static void Sweep(bool full)
    {
        var now = Core.TickCount;
        (Map, int, int) oldest = default;
        var oldestAt = long.MaxValue;

        foreach (var (key, at) in _tiles)
        {
            if (now - at >= RestMs)
            {
                _stale.Add(key);
            }
            else if (at < oldestAt)
            {
                oldestAt = at;
                oldest = key;
            }
        }

        for (var i = 0; i < _stale.Count; i++)
        {
            _tiles.Remove(_stale[i]);
        }

        if (_stale.Count == 0 && full && oldestAt != long.MaxValue)
        {
            _tiles.Remove(oldest);
        }

        _stale.Clear();
    }

    public static string Describe() => $"{Noted} tiles found with nowhere to stand, {_tiles.Count} written off now, {Skipped} rocks passed over for it";
}
