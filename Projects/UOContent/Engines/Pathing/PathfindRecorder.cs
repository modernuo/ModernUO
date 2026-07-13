using System.IO;
using System.Text;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.Engines.Pathing;

/// <summary>
/// Appends one JSONL record per pathfind, capturing the inputs — start, goal, map, capability
/// flags — needed to replay it later in a benchmark. Toggled at runtime with [PathRecord;
/// <see cref="Configure"/> only seeds the initial state from server.cfg.
///
/// Meant for short bursts: turn it on, walk the region or trigger the scenario, turn it off. The
/// writes go through a StreamWriter's buffer on the game thread, so a busy shard doing hundreds of
/// pathfinds a second can saturate that buffer and stall the loop on disk I/O. Sustained capture
/// would need an async sink with backpressure.
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
    /// Toggles recording, opening the file on enable and flushing and closing it on disable.
    /// Idempotent.
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
    /// Pushes the writer's buffer to disk, so a capture can be inspected without disabling first.
    /// No-op when disabled.
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
    /// Records one Find. Sits on the pathfinding hot path, so it costs a single bool check when
    /// disabled.
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
            // One interpolation covers every numeric field without a per-field ToString. The bools
            // are appended as literals because JSON wants lowercase and bool.ToString() capitalizes.
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
