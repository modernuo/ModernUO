using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Engines.Pathing.Cache;
using Server.Logging;
using Server.Text;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin-toggled, purely passive pathfinding telemetry. Attach to one or more mobs with
/// the <c>[PathTrack</c> command; for every real <see cref="BitmapAStarAlgorithm.Find"/>
/// that mob performs, this snapshots the global <see cref="StepCache"/> counters before and
/// after the search and appends the per-Find delta to <c>Logs/pathtrack.jsonl</c>. Every
/// <see cref="WindowSize"/> Finds it also sends the observing admin a one-line rolling
/// verdict (served / built / fell-through percentages).
///
/// Unlike <see cref="PathDiag"/>, this changes NOTHING about cache behavior — it does not
/// touch <c>MissPromotionThreshold</c> or pre-warm. That is the point: it measures whether
/// mobs warm up naturally at the production promotion threshold during real gameplay
/// (a pet following the player, a wandering NPC, a chasing monster).
///
/// Single-threaded game loop means the before/after snapshot pair brackets exactly one
/// Find, so the delta is correctly attributed to that mob. Multiple mobs may be tracked at
/// once; lines are disambiguated by serial. Same buffered-write workload caveat as
/// <see cref="PathfindRecorder"/>: intended for short capture bursts, not 24/7.
/// </summary>
public static class PathTracker
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PathTracker));

    // Live-verdict cadence: emit a rolling summary every this many Finds per mob. Fixed
    // bucket (reset on emit), not a sliding window — enough to read the warming trend.
    private const int WindowSize = 25;

    private sealed class TrackState
    {
        public Mobile Observer;
        // Lifetime counters — populated by RecordIfTracked.
        public long LifeServed, LifeBuilt, LifeFell, LifeFinds;
        public long WinServed, WinBuilt, WinFell, WinFinds;
    }

    private static readonly Dictionary<Mobile, TrackState> _tracked = new();
    private static StreamWriter _writer;
    private static string _outputPath = Path.Combine(Core.BaseDirectory, "Logs", "pathtrack.jsonl");

    public static bool IsTracking => _tracked.Count > 0;
    public static bool IsTracked(Mobile m) => m != null && _tracked.ContainsKey(m);
    public static string OutputPath => _outputPath;

    /// <summary>
    /// Per-Find counter delta, grouped into the three signals we care about:
    ///   served = cache hits (chunk was already warm),
    ///   built  = chunk built or dirty-rebuilt on this Find (warming in progress),
    ///   fell   = fallthroughs (cache couldn't help: multi-Z / off-map / src-Z / not-built).
    /// </summary>
    internal static (long served, long built, long fell) ComputeDelta(in CacheStats before, in CacheStats after) => (
        after.Hits - before.Hits,
        (after.MissesNotBuilt - before.MissesNotBuilt) + (after.MissesDirtyRebuild - before.MissesDirtyRebuild),
        (after.FallthroughMultiZ - before.FallthroughMultiZ)
        + (after.FallthroughSourceZMismatch - before.FallthroughSourceZMismatch)
        + (after.FallthroughOffMap - before.FallthroughOffMap)
        + (after.FallthroughNotBuilt - before.FallthroughNotBuilt)
    );

    /// <summary>
    /// Start tracking <paramref name="target"/> if untracked (returns true), or stop and
    /// print its lifetime tally if already tracked (returns false). <paramref name="observer"/>
    /// is the admin who receives the live verdict lines.
    /// </summary>
    public static bool Toggle(Mobile observer, Mobile target)
    {
        if (target == null)
        {
            return false;
        }

        if (_tracked.ContainsKey(target))
        {
            StopTracking(observer, target);
            return false;
        }

        return StartTracking(observer, target);
    }

    private static bool StartTracking(Mobile observer, Mobile target)
    {
        EnsureWriter();
        if (_writer == null)
        {
            observer?.SendMessage($"PathTrack: could not open {_outputPath} for write; not tracking {target.Name}.");
            return false;
        }

        _tracked[target] = new TrackState { Observer = observer };
        observer?.SendMessage($"PathTrack: now tracking {target.Name} (0x{target.Serial.Value:X}). Writing to {_outputPath}");
        return true;
    }

    private static void StopTracking(Mobile observer, Mobile target)
    {
        if (_tracked.Remove(target, out var st))
        {
            var total = st.LifeServed + st.LifeBuilt + st.LifeFell;
            var servedPct = total == 0 ? 0 : 100 * st.LifeServed / total;
            observer?.SendMessage(
                $"PathTrack: stopped {target.Name} after {st.LifeFinds} Finds — served {servedPct}% (served={st.LifeServed} built={st.LifeBuilt} fell={st.LifeFell})"
            );
        }

        if (_tracked.Count == 0)
        {
            CloseWriter();
        }
    }

    /// <summary>Stop tracking every mob and flush + close the log.</summary>
    public static void Clear()
    {
        _tracked.Clear();
        CloseWriter();
    }

    private static void EnsureWriter()
    {
        if (_writer != null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_outputPath) ?? ".");
            var stream = new FileStream(_outputPath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, new UTF8Encoding(false));
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "PathTracker: failed to open {Path} for write", _outputPath);
            _writer = null;
        }
    }

    private static void CloseWriter()
    {
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "PathTracker: error closing {Path}", _outputPath);
        }

        _writer = null;
    }

    /// <summary>
    /// Record one Find by a tracked mob: compute the cache delta vs <paramref name="before"/>,
    /// append a JSONL line, accumulate lifetime + rolling totals, and emit the live verdict
    /// every <see cref="WindowSize"/> Finds. No-op when <paramref name="m"/> is not tracked.
    /// Prunes the mob if it was deleted since tracking began.
    /// </summary>
    public static void RecordIfTracked(
        Mobile m, Map map, Point3D start, Point3D goal, Direction[] path, in CacheStats before
    )
    {
        if (m == null || !_tracked.TryGetValue(m, out var st))
        {
            return;
        }

        if (m.Deleted)
        {
            _tracked.Remove(m);
            if (_tracked.Count == 0)
            {
                CloseWriter();
            }
            return;
        }

        var after = StepCache.Instance.GetStats();
        var (served, built, fell) = ComputeDelta(before, after);
        // len is the path length in steps, or -1 when no path was found (null path).
        var len = path?.Length ?? -1;

        if (!WriteLine(m, map, start, goal, len, served, built, fell))
        {
            return;
        }

        st.LifeServed += served;
        st.LifeBuilt += built;
        st.LifeFell += fell;
        st.LifeFinds++;

        st.WinServed += served;
        st.WinBuilt += built;
        st.WinFell += fell;
        st.WinFinds++;

        if (st.WinFinds >= WindowSize)
        {
            // Emit first: the window counters still hold the full WindowSize-Find window here; reset after.
            EmitLive(m, st);
            st.WinServed = st.WinBuilt = st.WinFell = st.WinFinds = 0;
        }
    }

    private static bool WriteLine(
        Mobile m, Map map, Point3D start, Point3D goal, int len, long served, long built, long fell
    )
    {
        if (_writer == null)
        {
            return false;
        }

        try
        {
            // ValueStringBuilder is a ref struct; a `using var` ref local can't be combined with a
            // try/catch here, so dispose it explicitly in a finally. (PathfindRecorder uses `using var`
            // because it has no inner catch around the write.)
            // One stack-allocated builder, no per-field ToString allocation. The mob name is
            // sanitized (quotes/backslashes/control characters dropped) so it can't break the JSON line.
            var vsb = ValueStringBuilder.Create(192);
            try
            {
                vsb.Append($"{{\"t\":\"{Core.Now:yyyy-MM-dd HH:mm:ss}\",\"serial\":\"0x{m.Serial.Value:X}\",\"name\":\"");
                AppendSanitized(ref vsb, m.Name);
                vsb.Append(
                    $"\",\"mapId\":{map?.MapID ?? -1},\"sx\":{start.X},\"sy\":{start.Y},\"sz\":{start.Z},\"gx\":{goal.X},\"gy\":{goal.Y},\"gz\":{goal.Z},\"len\":{len},\"served\":{served},\"built\":{built},\"fell\":{fell}}}\n"
                );
                _writer.Write(vsb.AsSpan());
            }
            finally
            {
                vsb.Dispose();
            }

            return true;
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "PathTracker: write failed, clearing tracking");
            Clear();
            return false;
        }
    }

    // Strips quotes, backslashes, and all control characters (< 0x20) so the name can't break
    // or invalidate the JSON line. Names are short and rarely contain these, so the per-char
    // loop is negligible.
    private static void AppendSanitized(ref ValueStringBuilder vsb, string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return;
        }

        foreach (var c in s)
        {
            if (c is '"' or '\\' || c < ' ')
            {
                continue;
            }
            vsb.Append(c);
        }
    }

    private static void EmitLive(Mobile m, TrackState st)
    {
        var total = st.WinServed + st.WinBuilt + st.WinFell;
        var servedPct = total == 0 ? 0 : 100 * st.WinServed / total;
        var builtPct = total == 0 ? 0 : 100 * st.WinBuilt / total;
        var fellPct = total == 0 ? 0 : 100 * st.WinFell / total;
        st.Observer?.SendMessage(
            $"{m.Name}: served {servedPct}% | built {builtPct}% | fell {fellPct}% (last {WindowSize})"
        );
    }
}
