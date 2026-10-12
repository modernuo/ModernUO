using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Cache;
using Server.Engines.Pathing.Tiered;

namespace Server.Tests.Pathfinding.Tiered;

/// <summary>
/// A synthetic ground for the navigation tests, drawn from rows of characters. Each character names the standable
/// surfaces of its cell:
/// <c>.</c> ground at 0, <c>#</c> and <c>~</c> nothing (wall, deep water), <c>=</c> a bridge deck at 20 over water,
/// <c>+</c> a bridge deck at 20 over ground at 0 (two strata), <c>h</c> high ground at 10, <c>/</c> and <c>r</c> ramps
/// at 5 and 15.
///
/// <para>
/// A step goes to the neighbour's surface nearest the mover's height, allowed when it climbs at most 5 or drops at most
/// 10: so high ground drops to the ground beside it but cannot be climbed back, a ramp joins the two, and a deck is
/// reached only from a deck or a ramp at its own height. The mask is raw, as the engine's is: no corner rule.
/// </para>
/// </summary>
internal sealed class GridNavTerrain : INavTerrain
{
    public const int Climb = 5;

    public const int Drop = 10;

    private readonly sbyte[][] _cells;

    private static readonly int[] _dx = [0, 1, 1, 1, 0, -1, -1, -1];

    private static readonly int[] _dy = [-1, -1, 0, 1, 1, 1, 0, -1];

    public GridNavTerrain(params string[] rows)
    {
        Height = rows.Length;
        Width = rows[0].Length;
        _cells = new sbyte[Width * Height][];

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                _cells[y * Width + x] = Parse(rows[y][x]);
            }
        }
    }

    public GridNavTerrain(int width, int height, char fill)
    {
        Width = width;
        Height = height;
        _cells = new sbyte[width * height][];

        for (var i = 0; i < _cells.Length; i++)
        {
            _cells[i] = Parse(fill);
        }
    }

    public int Width { get; }

    public int Height { get; }

    public static sbyte[] Parse(char c) =>
        c switch
        {
            '.' => [0],
            '=' => [20],
            '+' => [0, 20],
            'h' => [10],
            'r' => [15],
            '/' => [5],
            _ => []
        };

    public void Set(int x, int y, char c) => _cells[y * Width + x] = Parse(c);

    public void Fill(int x1, int y1, int x2, int y2, char c)
    {
        for (var y = y1; y <= y2; y++)
        {
            for (var x = x1; x <= x2; x++)
            {
                Set(x, y, c);
            }
        }
    }

    public sbyte[] At(int x, int y) =>
        x < 0 || y < 0 || x >= Width || y >= Height ? [] : _cells[y * Width + x];

    public int Surfaces(int x, int y, Span<sbyte> zs)
    {
        var cell = At(x, y);
        var n = Math.Min(cell.Length, zs.Length);

        for (var i = 0; i < n; i++)
        {
            zs[i] = cell[i];
        }

        return n;
    }

    public StepMask Step(int x, int y, sbyte z)
    {
        byte mask = 0;
        Span<sbyte> land = stackalloc sbyte[8];

        for (var d = 0; d < 8; d++)
        {
            if (Land(x + _dx[d], y + _dy[d], z, out var landed))
            {
                mask |= (byte)(1 << d);
                land[d] = landed;
            }
        }

        return new StepMask(mask, 0, land[0], land[1], land[2], land[3], land[4], land[5], land[6], land[7], 0, 0, 0, 0, 0, 0, 0, 0);
    }

    /// <summary>The neighbour's surface a mover at <paramref name="z"/> steps onto, if any.</summary>
    public bool Land(int x, int y, int z, out sbyte landed)
    {
        landed = 0;

        var best = int.MaxValue;
        var found = false;

        foreach (var t in At(x, y))
        {
            var rise = t - z;

            if (rise > Climb || -rise > Drop || Math.Abs(rise) >= best)
            {
                continue;
            }

            best = Math.Abs(rise);
            landed = t;
            found = true;
        }

        return found;
    }

    /// <summary>
    /// The exact least cost from one surface to another over the whole grid, by Dijkstra over (x, y, surface) with the
    /// strict corner rule and <see cref="NavCost"/>'s prices: the oracle the planners are measured against. -1 when none.
    /// </summary>
    public int Oracle(int sx, int sy, int sz, int gx, int gy, int gz)
    {
        var start = (sx, sy, Index(sx, sy, sz));
        var goal = (gx, gy, Index(gx, gy, gz));

        if (start.Item3 < 0 || goal.Item3 < 0)
        {
            return -1;
        }

        var dist = new Dictionary<(int, int, int), int> { [start] = 0 };
        var open = new PriorityQueue<(int X, int Y, int S), int>();

        open.Enqueue(start, 0);

        while (open.TryDequeue(out var at, out var cost))
        {
            if (cost > dist[at])
            {
                continue;
            }

            if (at == goal)
            {
                return cost;
            }

            var z = At(at.X, at.Y)[at.S];
            var mask = Step(at.X, at.Y, z);

            for (var d = 0; d < 8; d++)
            {
                if (!mask.IsWalkable((Direction)d))
                {
                    continue;
                }

                if ((d & 1) != 0 && (!mask.IsWalkable((Direction)((d + 7) & 7)) || !mask.IsWalkable((Direction)((d + 1) & 7))))
                {
                    continue;
                }

                var nx = at.X + _dx[d];
                var ny = at.Y + _dy[d];
                var ns = Index(nx, ny, mask.GetWalkZ((Direction)d));

                if (ns < 0)
                {
                    continue;
                }

                var next = (nx, ny, ns);
                var nc = cost + NavCost.Of(d);

                if (!dist.TryGetValue(next, out var old) || nc < old)
                {
                    dist[next] = nc;
                    open.Enqueue(next, nc);
                }
            }
        }

        return -1;
    }

    private int Index(int x, int y, int z)
    {
        var cell = At(x, y);

        for (var i = 0; i < cell.Length; i++)
        {
            if (cell[i] == z)
            {
                return i;
            }
        }

        return -1;
    }
}
