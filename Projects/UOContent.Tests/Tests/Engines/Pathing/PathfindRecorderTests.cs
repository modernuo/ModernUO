using System.IO;
using Server.Engines.Pathing;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class PathfindRecorderTests
{
    private static string NewTempPath() =>
        Path.Combine(Path.GetTempPath(), $"pathfind-recorder-{System.Guid.NewGuid():N}.jsonl");

    /// <summary>
    /// Reflection-set the static _outputPath without going through Configure (which
    /// reads from server.cfg) so tests don't poison the project's server.cfg.
    /// </summary>
    private static void OverrideOutputPath(string path)
    {
        typeof(PathfindRecorder).GetField("_outputPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(null, path);
    }

    [Fact]
    public void Disabled_RecordIfEnabled_DoesNothing()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);
        PathfindRecorder.SetEnabled(false);

        try
        {
            var stub = new RecorderStub(World.NewMobile);
            stub.DefaultMobileInit();
            stub.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            PathfindRecorder.RecordIfEnabled(
                stub, Map.Maps[1],
                new Point3D(1500, 1600, 0), new Point3D(1498, 1598, 0)
            );

            stub.Delete();
            Assert.False(File.Exists(path), "disabled recorder must not create the output file");
        }
        finally
        {
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void Enabled_RecordIfEnabled_WritesValidJsonlLine()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);
        PathfindRecorder.SetEnabled(true);

        try
        {
            Assert.True(PathfindRecorder.Enabled);

            var stub = new RecorderStub(World.NewMobile);
            stub.DefaultMobileInit();
            stub.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            PathfindRecorder.RecordIfEnabled(
                stub, Map.Maps[1],
                new Point3D(1500, 1600, 5), new Point3D(1498, 1598, 5)
            );

            // Disable to flush + close before reading.
            PathfindRecorder.SetEnabled(false);
            stub.Delete();

            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path).TrimEnd();
            Assert.Single(content.Split('\n'));
            Assert.StartsWith("{\"Name\":\"recorded\"", content);
            Assert.Contains("\"MapId\":1", content);
            Assert.Contains("\"StartX\":1500", content);
            Assert.Contains("\"StartY\":1600", content);
            Assert.Contains("\"GoalX\":1498", content);
            Assert.Contains("\"GoalY\":1598", content);
            Assert.Contains("\"CanSwim\":false", content);
            Assert.EndsWith("}", content);
        }
        finally
        {
            PathfindRecorder.SetEnabled(false);
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void Enabled_RecordsCapabilityFlagsFromBaseCreature()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);
        PathfindRecorder.SetEnabled(true);

        try
        {
            var stub = new RecorderStub(World.NewMobile);
            stub.DefaultMobileInit();
            stub.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);
            stub.CanSwim = true;

            PathfindRecorder.RecordIfEnabled(
                stub, Map.Maps[1],
                new Point3D(1500, 1600, 0), new Point3D(1498, 1598, 0)
            );

            PathfindRecorder.SetEnabled(false);
            stub.Delete();

            var content = File.ReadAllText(path);
            Assert.Contains("\"CanSwim\":true", content);
            Assert.Contains("\"CanFly\":false", content);
            Assert.Contains("\"CanOpenDoors\":", content);            // value depends on BaseCreature defaults
            Assert.Contains("\"CanMoveOverObstacles\":", content);    // — same.
        }
        finally
        {
            PathfindRecorder.SetEnabled(false);
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void SetEnabled_TogglingTwice_IsIdempotent()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            PathfindRecorder.SetEnabled(true);
            Assert.True(PathfindRecorder.Enabled);

            PathfindRecorder.SetEnabled(true); // second on: no-op, no error
            Assert.True(PathfindRecorder.Enabled);

            PathfindRecorder.SetEnabled(false);
            Assert.False(PathfindRecorder.Enabled);

            PathfindRecorder.SetEnabled(false); // second off: no-op
            Assert.False(PathfindRecorder.Enabled);
        }
        finally
        {
            PathfindRecorder.SetEnabled(false);
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    private sealed class RecorderStub : Server.Mobiles.BaseCreature
    {
        public RecorderStub(Serial serial) : base(serial)
        {
            Body = 0xC9;
        }
    }
}
