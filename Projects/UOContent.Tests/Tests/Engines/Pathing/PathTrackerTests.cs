using System.IO;
using Server.Engines.Pathing;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class PathTrackerTests
{
    private static string NewTempPath() =>
        Path.Combine(Path.GetTempPath(), $"pathtrack-{System.Guid.NewGuid():N}.jsonl");

    // Reflection-set the static _outputPath so tests don't write to the real Logs folder.
    private static void OverrideOutputPath(string path)
    {
        typeof(PathTracker).GetField("_outputPath",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(null, path);
    }

    [Fact]
    public void Toggle_AddsThenRemoves_Mob()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            Assert.False(PathTracker.IsTracking);

            var observer = new TrackStub(World.NewMobile);
            observer.DefaultMobileInit();
            observer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var target = new TrackStub(World.NewMobile);
            target.DefaultMobileInit();
            target.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            Assert.True(PathTracker.Toggle(observer, target));   // start
            Assert.True(PathTracker.IsTracking);
            Assert.True(PathTracker.IsTracked(target));

            Assert.False(PathTracker.Toggle(observer, target));  // stop
            Assert.False(PathTracker.IsTracking);
            Assert.False(PathTracker.IsTracked(target));

            observer.Delete();
            target.Delete();
        }
        finally
        {
            PathTracker.Clear();
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void Clear_StopsAllTracking()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            var observer = new TrackStub(World.NewMobile);
            observer.DefaultMobileInit();
            observer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var a = new TrackStub(World.NewMobile);
            a.DefaultMobileInit();
            a.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);
            var b = new TrackStub(World.NewMobile);
            b.DefaultMobileInit();
            b.MoveToWorld(new Point3D(1501, 1600, 0), Map.Maps[1]);

            PathTracker.Toggle(observer, a);
            PathTracker.Toggle(observer, b);
            Assert.True(PathTracker.IsTracking);

            PathTracker.Clear();
            Assert.False(PathTracker.IsTracking);
            Assert.False(PathTracker.IsTracked(a));
            Assert.False(PathTracker.IsTracked(b));

            observer.Delete();
            a.Delete();
            b.Delete();
        }
        finally
        {
            PathTracker.Clear();
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    private sealed class TrackStub : Server.Mobiles.BaseCreature
    {
        public TrackStub(Serial serial) : base(serial)
        {
            Body = 0xC9;
        }
    }
}
