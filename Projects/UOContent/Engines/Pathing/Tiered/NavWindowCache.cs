using System.Collections.Generic;

namespace Server.Engines.Pathing.Tiered;

/// <summary>
/// The clusters the planners look into most, kept probed: a route's two ends are joined to the graph by a flood inside
/// each end's cluster, and probing a cluster's cells costs far more than flooding them. Bots set out from and go to the
/// same few places — the bank, the forge, their hall — so a small cache holds most ends. Emptied round a cluster whose
/// ground changed (<see cref="Invalidate"/>).
/// </summary>
public sealed class NavWindowCache
{
    /// <summary>Windows kept.</summary>
    public int Capacity { get; set; } = 64;

    private readonly Dictionary<long, NavWindow> _windows = new();

    private long _clock;

    public long Hits { get; private set; }

    public long Misses { get; private set; }

    private static long Key(NavGraph graph, int cluster) => ((long)graph.Id << 32) | (uint)cluster;

    /// <summary>A window over <paramref name="cluster"/> of <paramref name="graph"/>, probed as far as it has been before.</summary>
    public NavWindow Get(NavGraph graph, int cluster)
    {
        var key = Key(graph, cluster);

        if (_windows.TryGetValue(key, out var window))
        {
            Hits++;
            window.Used = ++_clock;

            return window;
        }

        Misses++;

        if (_windows.Count >= Capacity)
        {
            var oldest = 0L;
            var oldestUse = long.MaxValue;

            foreach (var (k, w) in _windows)
            {
                if (w.Used < oldestUse)
                {
                    oldestUse = w.Used;
                    oldest = k;
                }
            }

            window = _windows[oldest];
            _windows.Remove(oldest);
        }

        window ??= new NavWindow();
        window.Reset(graph.Terrain, cluster, cluster % graph.ClustersX * NavGraph.Side, cluster / graph.ClustersX * NavGraph.Side);
        window.Used = ++_clock;
        _windows[key] = window;

        return window;
    }

    /// <summary>Drops the windows over the clusters a rectangle of <paramref name="graph"/> touches, and the ring round them.</summary>
    public void Invalidate(NavGraph graph, int x1, int y1, int x2, int y2)
    {
        var cx1 = x1 / NavGraph.Side - 1;
        var cy1 = y1 / NavGraph.Side - 1;
        var cx2 = x2 / NavGraph.Side + 1;
        var cy2 = y2 / NavGraph.Side + 1;

        for (var cy = cy1; cy <= cy2; cy++)
        {
            for (var cx = cx1; cx <= cx2; cx++)
            {
                if (cx >= 0 && cy >= 0 && cx < graph.ClustersX && cy < graph.ClustersY)
                {
                    _windows.Remove(Key(graph, cy * graph.ClustersX + cx));
                }
            }
        }
    }

    public void Clear() => _windows.Clear();
}
