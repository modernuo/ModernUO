using System.IO;
using System.Text;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.Engines.Pathing;

/// <summary>
/// Admin-toggled telemetry: appends a JSONL line per pathfind request to a file.
/// One record per BitmapAStarAlgorithm.Find call, capturing the inputs (start, goal,
/// map, capability flags) needed to replay the scenario in benchmarks. Output format
/// matches the corpus the BDN harness consumes.
///
/// Hot-toggleable at runtime via the [PathRecord admin command — no restart needed.
/// <see cref="Configure"/> only seeds the initial state from server.cfg
/// (pathfinding.recorder.enable, default false).
///
/// Holds a single StreamWriter open while recording; its internal buffer absorbs
/// per-record writes without per-call File.Open / File.Append. Each record is built
/// in a stack-allocated ValueStringBuilder (zero per-int allocation for the field
/// formatting), then handed to the writer as a ReadOnlySpan&lt;char&gt;.
///
/// <b>Workload note:</b> intended for short bursts of capture (turn on, walk a region
/// or trigger a scenario, turn off). On a busy server with hundreds of pathfinds
/// per second, sustained recording can saturate the StreamWriter's 4 KB buffer and
/// block the game thread on disk writes. A backpressure-aware async sink is a
/// future enhancement if 24/7 capture becomes a use case.
/// </summary>
public static class PathfindRecorder
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PathfindRecorder));

    private static StreamWriter _writer;

    public static bool Enabled { get; private set; }
    public static string OutputPath { get; set; }
    public static long RecordsWritten { get; private set; }

    public static void Configure()
    {
        OutputPath = ServerConfiguration.GetOrUpdateSetting(
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
        if (enabled == Enabled)
        {
            return;
        }

        if (enabled)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? ".");
                var stream = new FileStream(OutputPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(stream, new UTF8Encoding(false));
                Enabled = true;
                logger.Information("PathfindRecorder enabled, writing to {Path}", OutputPath);
            }
            catch (IOException ex)
            {
                logger.Warning(ex, "PathfindRecorder: failed to open {Path} for write", OutputPath);
                _writer = null;
                Enabled = false;
            }
        }
        else
        {
            Enabled = false;
            try
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
            catch (IOException ex)
            {
                logger.Warning(ex, "PathfindRecorder: error closing {Path}", OutputPath);
            }
            _writer = null;
            logger.Information("PathfindRecorder disabled ({Count} records this session)", RecordsWritten);
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
            logger.Warning(ex, "PathfindRecorder: flush failed for {Path}", OutputPath);
        }
    }

    /// <summary>
    /// Capture one Find call. Hot path: cheap when disabled (single bool check).
    /// When enabled, formats one JSONL line and writes it through the StreamWriter's
    /// internal buffer — flush is amortized across many calls.
    /// </summary>
    public static void RecordIfEnabled(Mobile m, Map map, Point3D start, Point3D goal)
    {
        if (!Enabled || _writer == null || m == null || map == null)
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
            // One interpolation handles every numeric field with no per-int ToString
            // allocation; bool fields use explicit literal spans because JSON wants
            // lowercase "true"/"false" and bool.ToString() yields "True"/"False".
            using var vsb = ValueStringBuilder.Create(192);
            vsb.Append(
                $"{{\"Name\":\"recorded\",\"MapId\":{map.MapID},\"StartX\":{start.X},\"StartY\":{start.Y},\"StartZ\":{start.Z},\"GoalX\":{goal.X},\"GoalY\":{goal.Y},\"GoalZ\":{goal.Z},\"CanSwim\":"
            );
            vsb.Append(canSwim ? "true" : "false");
            vsb.Append(",\"CanFly\":");
            vsb.Append(canFly ? "true" : "false");
            vsb.Append(",\"CanOpenDoors\":");
            vsb.Append(canOpenDoors ? "true" : "false");
            vsb.Append(",\"CanMoveOverObstacles\":");
            vsb.Append(canMoveOverObstacles ? "true" : "false");
            vsb.Append("}\n");
            _writer.Write(vsb.AsSpan());
            RecordsWritten++;
        }
        catch (IOException ex)
        {
            logger.Warning(ex, "PathfindRecorder: write failed, disabling");
            SetEnabled(false);
        }
    }
}
