using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// The outline of a patch of cells as a few smooth rings: marching squares round the cells, Douglas–Peucker to drop the
/// staircase, Chaikin to round the corners.
///
/// <para>
/// <b>Pure arithmetic on arrays it is handed, and nothing else.</b> No map, no mobile, no static state of the shard: it may run
/// on the loop or off it (<see cref="BotZoneBuild"/>) and gives the same answer either way. The rings are for the eye and for
/// the JSON; nothing that routes or hunts reads them — the raster is the answer to "what is here", and a smoothed corner that
/// cuts two tiles off a patrol must never make a planner believe the two tiles are safe.
/// </para>
///
/// <para>
/// <b>Holes are filled before tracing.</b> The contract allows outer rings only, and a hole inside a patrol area — a pond, a
/// house — is not a place a bot can stand safe in the middle of a pack anyway.
/// </para>
/// </summary>
public static class BotZoneShapes
{
    /// <summary>
    /// The marching-squares segments per case, as the proven table of d3-contour: corners bottom-left 1, bottom-right 2,
    /// top-right 4, top-left 8; points in half-units of the sample grid, relative to the square's top-left sample, each segment
    /// directed so the filled side lies to its left in screen coordinates.
    /// </summary>
    private static readonly int[][] Cases =
    [
        [],
        [2, 3, 1, 2],
        [3, 2, 2, 3],
        [3, 2, 1, 2],
        [2, 1, 3, 2],
        [2, 3, 1, 2, 2, 1, 3, 2],
        [2, 1, 2, 3],
        [2, 1, 1, 2],
        [1, 2, 2, 1],
        [2, 3, 2, 1],
        [1, 2, 2, 1, 3, 2, 2, 3],
        [3, 2, 2, 1],
        [1, 2, 3, 2],
        [2, 3, 3, 2],
        [1, 2, 2, 3],
        []
    ];

    private sealed class Fragment
    {
        public int Start;

        public int End;

        public List<int> Points;
    }

    /// <summary>
    /// Rings round the filled cells of a mask, in map tiles. Cell (i, j) of the mask is map cell (<paramref name="ox"/> + i,
    /// <paramref name="oy"/> + j), four tiles to a side. Each ring comes back as x0, y0, x1, y1, …, not repeating its first
    /// point; at most <paramref name="mostRings"/> of them, largest first, each of at most <paramref name="mostPoints"/> points.
    /// </summary>
    public static List<int[]> Rings(
        byte[] mask, int w, int h, int ox, int oy, double epsilon, int mostPoints, int mostRings, int passes
    )
    {
        List<int[]> rings = [];

        if (w <= 0 || h <= 0)
        {
            return rings;
        }

        // Padded by one on every side, so every blob is closed by the grid's own border.
        var pw = w + 2;
        var ph = h + 2;
        var grid = new byte[pw * ph];

        for (var j = 0; j < h; j++)
        {
            for (var i = 0; i < w; i++)
            {
                if (mask[j * w + i] != 0)
                {
                    grid[(j + 1) * pw + i + 1] = 1;
                }
            }
        }

        FillHoles(grid, pw, ph);

        var traced = Trace(grid, pw, ph);

        if (traced.Count == 0)
        {
            return rings;
        }

        // Largest first, so a cap on rings drops the specks rather than the body.
        List<(double Area, List<(double X, double Y)> Points)> sized = new(traced.Count);

        for (var r = 0; r < traced.Count; r++)
        {
            var half = traced[r];
            var points = new List<(double X, double Y)>(half.Count / 2);

            for (var k = 0; k + 1 < half.Count; k += 2)
            {
                // Half-units of the padded sample grid to tiles: see the note on the class for the arithmetic.
                points.Add(((ox - 1 + half[k] / 2.0) * BotZoneState.Cell, (oy - 1 + half[k + 1] / 2.0) * BotZoneState.Cell));
            }

            if (points.Count >= 2 && points[0] == points[^1])
            {
                points.RemoveAt(points.Count - 1);
            }

            if (points.Count >= 3)
            {
                sized.Add((Math.Abs(Area(points)), points));
            }
        }

        sized.Sort((a, b) => b.Area.CompareTo(a.Area));

        for (var r = 0; r < sized.Count && rings.Count < Math.Max(1, mostRings); r++)
        {
            var ring = Smooth(sized[r].Points, epsilon, mostPoints, passes);

            if (ring.Length >= 6)
            {
                rings.Add(ring);
            }
        }

        return rings;
    }

    /// <summary>Every empty sample the border cannot reach, four ways, becomes filled.</summary>
    private static void FillHoles(byte[] grid, int pw, int ph)
    {
        var outside = new bool[grid.Length];
        var stack = new Stack<int>();

        for (var i = 0; i < pw; i++)
        {
            stack.Push(i);
            stack.Push((ph - 1) * pw + i);
        }

        for (var j = 0; j < ph; j++)
        {
            stack.Push(j * pw);
            stack.Push(j * pw + pw - 1);
        }

        while (stack.Count > 0)
        {
            var at = stack.Pop();

            if (outside[at] || grid[at] != 0)
            {
                continue;
            }

            outside[at] = true;

            var x = at % pw;
            var y = at / pw;

            if (x > 0)
            {
                stack.Push(at - 1);
            }

            if (x < pw - 1)
            {
                stack.Push(at + 1);
            }

            if (y > 0)
            {
                stack.Push(at - pw);
            }

            if (y < ph - 1)
            {
                stack.Push(at + pw);
            }
        }

        for (var i = 0; i < grid.Length; i++)
        {
            if (grid[i] == 0 && !outside[i])
            {
                grid[i] = 1;
            }
        }
    }

    /// <summary>Marching squares with d3's stitching: closed rings in half-units, each ending on its own first point.</summary>
    private static List<List<int>> Trace(byte[] grid, int pw, int ph)
    {
        List<List<int>> done = [];
        Dictionary<int, Fragment> byStart = [];
        Dictionary<int, Fragment> byEnd = [];
        var stride = 2 * pw + 4;

        for (var y = 0; y < ph - 1; y++)
        {
            for (var x = 0; x < pw - 1; x++)
            {
                var tl = grid[y * pw + x];
                var tr = grid[y * pw + x + 1];
                var br = grid[(y + 1) * pw + x + 1];
                var bl = grid[(y + 1) * pw + x];
                var segments = Cases[bl | (br << 1) | (tr << 2) | (tl << 3)];

                for (var s = 0; s + 3 < segments.Length; s += 4)
                {
                    var sx = segments[s] + 2 * x;
                    var sy = segments[s + 1] + 2 * y;
                    var ex = segments[s + 2] + 2 * x;
                    var ey = segments[s + 3] + 2 * y;

                    Stitch(sx, sy, ex, ey, sy * stride + sx, ey * stride + ex, byStart, byEnd, done);
                }
            }
        }

        return done;
    }

    private static void Stitch(
        int sx, int sy, int ex, int ey, int start, int end,
        Dictionary<int, Fragment> byStart, Dictionary<int, Fragment> byEnd, List<List<int>> done
    )
    {
        if (byEnd.TryGetValue(start, out var f))
        {
            if (byStart.TryGetValue(end, out var g))
            {
                byEnd.Remove(f.End);
                byStart.Remove(g.Start);

                if (ReferenceEquals(f, g))
                {
                    f.Points.Add(ex);
                    f.Points.Add(ey);
                    done.Add(f.Points);
                }
                else
                {
                    var joined = new Fragment { Start = f.Start, End = g.End, Points = f.Points };

                    joined.Points.AddRange(g.Points);
                    byStart[joined.Start] = joined;
                    byEnd[joined.End] = joined;
                }
            }
            else
            {
                byEnd.Remove(f.End);
                f.Points.Add(ex);
                f.Points.Add(ey);
                f.End = end;
                byEnd[end] = f;
            }
        }
        else if (byStart.TryGetValue(end, out f))
        {
            if (byEnd.TryGetValue(start, out var g))
            {
                byStart.Remove(f.Start);
                byEnd.Remove(g.End);

                if (ReferenceEquals(f, g))
                {
                    f.Points.Add(ex);
                    f.Points.Add(ey);
                    done.Add(f.Points);
                }
                else
                {
                    var joined = new Fragment { Start = g.Start, End = f.End, Points = g.Points };

                    joined.Points.AddRange(f.Points);
                    byStart[joined.Start] = joined;
                    byEnd[joined.End] = joined;
                }
            }
            else
            {
                byStart.Remove(f.Start);
                f.Points.Insert(0, sy);
                f.Points.Insert(0, sx);
                f.Start = start;
                byStart[start] = f;
            }
        }
        else
        {
            var fresh = new Fragment { Start = start, End = end, Points = [sx, sy, ex, ey] };

            byStart[start] = fresh;
            byEnd[end] = fresh;
        }
    }

    /// <summary>Douglas–Peucker until the ring fits the point cap after smoothing, then Chaikin; rounded to tiles.</summary>
    private static int[] Smooth(List<(double X, double Y)> ring, double epsilon, int mostPoints, int passes)
    {
        passes = Math.Clamp(passes, 0, 4);

        var growth = 1 << passes;
        var cap = Math.Max(3, Math.Max(3, mostPoints) / growth);
        var eps = Math.Max(0.1, epsilon);
        var kept = Simplify(ring, eps);

        for (var tries = 0; kept.Count > cap && tries < 8; tries++)
        {
            eps *= 1.6;
            kept = Simplify(ring, eps);
        }

        if (kept.Count > cap)
        {
            // Still too many: every n-th point, which keeps the shape's extent if not its detail.
            var thinned = new List<(double X, double Y)>(cap);
            var step = kept.Count / (double)cap;

            for (var k = 0; k < cap; k++)
            {
                thinned.Add(kept[(int)(k * step)]);
            }

            kept = thinned;
        }

        for (var p = 0; p < passes && kept.Count >= 3; p++)
        {
            var next = new List<(double X, double Y)>(kept.Count * 2);

            for (var k = 0; k < kept.Count; k++)
            {
                var a = kept[k];
                var b = kept[(k + 1) % kept.Count];

                next.Add((0.75 * a.X + 0.25 * b.X, 0.75 * a.Y + 0.25 * b.Y));
                next.Add((0.25 * a.X + 0.75 * b.X, 0.25 * a.Y + 0.75 * b.Y));
            }

            kept = next;
        }

        var result = new List<int>(kept.Count * 2);
        int lastX = int.MinValue, lastY = int.MinValue;

        for (var k = 0; k < kept.Count; k++)
        {
            var x = (int)Math.Round(kept[k].X);
            var y = (int)Math.Round(kept[k].Y);

            if (x == lastX && y == lastY)
            {
                continue;
            }

            result.Add(x);
            result.Add(y);
            lastX = x;
            lastY = y;
        }

        if (result.Count >= 4 && result[0] == result[^2] && result[1] == result[^1])
        {
            result.RemoveRange(result.Count - 2, 2);
        }

        return result.ToArray();
    }

    /// <summary>Douglas–Peucker on a closed ring: split at the first point and the point farthest from it, each half simplified.</summary>
    private static List<(double X, double Y)> Simplify(List<(double X, double Y)> ring, double eps)
    {
        var n = ring.Count;

        if (n <= 4)
        {
            return new List<(double X, double Y)>(ring);
        }

        var far = 0;
        var farthest = -1.0;

        for (var k = 1; k < n; k++)
        {
            var dx = ring[k].X - ring[0].X;
            var dy = ring[k].Y - ring[0].Y;
            var d = dx * dx + dy * dy;

            if (d > farthest)
            {
                farthest = d;
                far = k;
            }
        }

        var keep = new bool[n + 1];

        keep[0] = true;
        keep[far] = true;
        keep[n] = true;

        Mark(ring, 0, far, eps, keep);
        Mark(ring, far, n, eps, keep);

        List<(double X, double Y)> kept = [];

        for (var k = 0; k < n; k++)
        {
            if (keep[k])
            {
                kept.Add(ring[k]);
            }
        }

        return kept;
    }

    /// <summary>Keeps the points between two kept ones that stand off the chord by more than epsilon, without recursion.</summary>
    private static void Mark(List<(double X, double Y)> ring, int from, int to, double eps, bool[] keep)
    {
        var n = ring.Count;
        var stack = new Stack<(int A, int B)>();

        stack.Push((from, to));

        while (stack.Count > 0)
        {
            var (a, b) = stack.Pop();

            if (b - a < 2)
            {
                continue;
            }

            var pa = ring[a % n];
            var pb = ring[b % n];
            var best = -1.0;
            var at = -1;

            for (var k = a + 1; k < b; k++)
            {
                var d = Off(ring[k % n], pa, pb);

                if (d > best)
                {
                    best = d;
                    at = k;
                }
            }

            if (best > eps && at > 0)
            {
                keep[at] = true;
                stack.Push((a, at));
                stack.Push((at, b));
            }
        }
    }

    private static double Off((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 1e-9)
        {
            return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
        }

        return Math.Abs(dy * p.X - dx * p.Y + b.X * a.Y - b.Y * a.X) / length;
    }

    private static double Area(List<(double X, double Y)> ring)
    {
        var sum = 0.0;

        for (var k = 0; k < ring.Count; k++)
        {
            var a = ring[k];
            var b = ring[(k + 1) % ring.Count];

            sum += a.X * b.Y - b.X * a.Y;
        }

        return sum / 2.0;
    }
}
