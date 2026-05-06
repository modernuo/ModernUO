using System.Globalization;
using System.IO;
using System.Text;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin-toggled telemetry: appends a JSONL line per pathfind request to a file.
/// One record per BitmapAStarAlgorithm.Find call, capturing the inputs (start, goal,
/// map, capability flags) needed to replay the scenario in benchmarks. Output format
/// matches the corpus the BDN harness consumes.
///
/// Off by default; flipped on via [PathRecord on / off or
/// pathfinding.recorder.enable in server.cfg. Holds a single StreamWriter open
/// while recording — its internal buffer absorbs per-record writes without per-call
/// File.Open / File.Append overhead.
/// </summary>
public static class PathfindRecorder
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PathfindRecorder));

    private static bool _enabled;
    private static string _outputPath;
    private static StreamWriter _writer;
    private static long _recordsWritten;

    public static bool Enabled => _enabled;
    public static string OutputPath => _outputPath;
    public static long RecordsWritten => _recordsWritten;

    public static void Configure()
    {
        _outputPath = ServerConfiguration.GetOrUpdateSetting(
            "pathfinding.recorder.path",
            Path.Combine(Core.BaseDirectory, "Data", "Pathfinding", "recordings", "pathfinds.jsonl")
        );

        var startEnabled = ServerConfiguration.GetOrUpdateSetting("pathfinding.recorder.enable", false);
        if (startEnabled)
        {
            SetEnabled(true);
        }
    }

    /// <summary>
    /// Toggle recording. When enabling, opens an append-mode StreamWriter; when
    /// disabling, flushes + disposes it. Idempotent — calling twice with the same
    /// state is a no-op.
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
        {
            return;
        }

        if (enabled)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_outputPath) ?? ".");
                var stream = new FileStream(_outputPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, new UTF8Encoding(false));
                _enabled = true;
                logger.Information("PathfindRecorder enabled, writing to {Path}", _outputPath);
            }
            catch (IOException ex)
            {
                logger.Warning(ex, "PathfindRecorder: failed to open {Path} for write", _outputPath);
                _writer = null;
                _enabled = false;
            }
        }
        else
        {
            _enabled = false;
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch (IOException ex)
            {
                logger.Warning(ex, "PathfindRecorder: error closing {Path}", _outputPath);
            }
            _writer = null;
            logger.Information("PathfindRecorder disabled ({Count} records this session)", _recordsWritten);
        }
    }

    /// <summary>
    /// Force a flush of the writer's internal buffer to disk. Safe to call when
    /// disabled (no-op). Useful after a burst of recording when an admin wants to
    /// inspect the file without waiting for buffer fill or disable.
    /// </summary>
    public static void Flush()
    {
        try
        {
            _writer?.Flush();
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "PathfindRecorder: flush failed for {Path}", _outputPath);
        }
    }

    /// <summary>
    /// Capture one Find call. Hot path: cheap when disabled (single bool check).
    /// When enabled, formats one JSONL line and writes it through the StreamWriter's
    /// internal buffer — flush is amortized across many calls.
    /// </summary>
    public static void RecordIfEnabled(Mobile m, Map map, Point3D start, Point3D goal)
    {
        if (!_enabled || _writer == null || m == null || map == null)
        {
            return;
        }

        var canSwim = false;
        var canFly = false;
        var canOpenDoors = false;
        var canMoveOverObstacles = false;
        if (m is BaseCreature bc)
        {
            canSwim = bc.CanSwim;
            canFly = bc.CanFly;
            canOpenDoors = bc.CanOpenDoors;
            canMoveOverObstacles = bc.CanMoveOverObstacles;
        }

        try
        {
            var w = _writer;
            w.Write("{\"Name\":\"recorded\",\"MapId\":");
            w.Write(map.MapID.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"StartX\":");
            w.Write(start.X.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"StartY\":");
            w.Write(start.Y.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"StartZ\":");
            w.Write(start.Z.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"GoalX\":");
            w.Write(goal.X.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"GoalY\":");
            w.Write(goal.Y.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"GoalZ\":");
            w.Write(goal.Z.ToString(CultureInfo.InvariantCulture));
            w.Write(",\"CanSwim\":");
            w.Write(canSwim ? "true" : "false");
            w.Write(",\"CanFly\":");
            w.Write(canFly ? "true" : "false");
            w.Write(",\"CanOpenDoors\":");
            w.Write(canOpenDoors ? "true" : "false");
            w.Write(",\"CanMoveOverObstacles\":");
            w.Write(canMoveOverObstacles ? "true" : "false");
            w.Write("}\n");
            _recordsWritten++;
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "PathfindRecorder: write failed, disabling");
            SetEnabled(false);
        }
    }
}
