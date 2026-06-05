using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Engines.Pathing.Cache;
using Server.Logging;

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
        // Lifetime counters — populated by the per-Find record method (Task 2+).
#pragma warning disable CS0649
        public long LifeServed, LifeBuilt, LifeFell, LifeFinds;
        public long WinServed, WinBuilt, WinFell, WinFinds;
#pragma warning restore CS0649
    }

    private static readonly Dictionary<Mobile, TrackState> _tracked = new();
    private static StreamWriter _writer;
    private static string _outputPath = Path.Combine(Core.BaseDirectory, "Logs", "pathtrack.jsonl");

    public static bool IsTracking => _tracked.Count > 0;
    public static bool IsTracked(Mobile m) => m != null && _tracked.ContainsKey(m);
    public static string OutputPath => _outputPath;

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

        StartTracking(observer, target);
        return true;
    }

    private static void StartTracking(Mobile observer, Mobile target)
    {
        EnsureWriter();
        _tracked[target] = new TrackState { Observer = observer };
        observer?.SendMessage($"PathTrack: now tracking {target.Name} (0x{target.Serial.Value:X}). Writing to {_outputPath}");
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
}
