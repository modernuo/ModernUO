using System.IO;
using Server.Engines.Pathing;
using Server.Engines.Pathing.Cache;
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

    [Fact]
    public void Toggle_Start_OpensLogFile()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            var observer = new TrackStub(World.NewMobile);
            observer.DefaultMobileInit();
            observer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var target = new TrackStub(World.NewMobile);
            target.DefaultMobileInit();
            target.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var started = PathTracker.Toggle(observer, target);
            Assert.True(started);
            Assert.True(File.Exists(path));

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
    public void ComputeDelta_SubtractsBeforeFromAfter_GroupingCounters()
    {
        var before = new CacheStats(
            residentChunks: 0, hits: 10, missesNotBuilt: 2, missesDirtyRebuild: 1,
            fallthroughMultiZ: 3, fallthroughOffMap: 1, fallthroughSourceZMismatch: 2,
            fallthroughNotBuilt: 4, fallthroughMulti: 0, multiLocalHits: 0, multiMaskCacheHits: 0,
            evictionsByLruCap: 0, buildsTotal: 0
        );
        var after = new CacheStats(
            residentChunks: 0, hits: 18, missesNotBuilt: 5, missesDirtyRebuild: 3,
            fallthroughMultiZ: 4, fallthroughOffMap: 1, fallthroughSourceZMismatch: 2,
            fallthroughNotBuilt: 6, fallthroughMulti: 0, multiLocalHits: 0, multiMaskCacheHits: 0,
            evictionsByLruCap: 0, buildsTotal: 0
        );

        var (served, built, fell) = PathTracker.ComputeDelta(before, after);

        Assert.Equal(8, served);                 // 18 - 10
        Assert.Equal(5, built);                   // (5-2) + (3-1)
        Assert.Equal(3, fell);                    // (4-3) + (1-1) + (2-2) + (6-4)
    }

    [Fact]
    public void RecordIfTracked_Untracked_WritesNothing()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            var m = new TrackStub(World.NewMobile);
            m.DefaultMobileInit();
            m.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            PathTracker.RecordIfTracked(
                m, Map.Maps[1], new Point3D(1500, 1600, 0), new Point3D(1502, 1600, 0),
                null, default
            );

            m.Delete();
            Assert.False(File.Exists(path));
        }
        finally
        {
            PathTracker.Clear();
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void RecordIfTracked_Tracked_WritesOneJsonlLine()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            var observer = new TrackStub(World.NewMobile);
            observer.DefaultMobileInit();
            observer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var m = new TrackStub(World.NewMobile);
            m.DefaultMobileInit();
            m.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            PathTracker.Toggle(observer, m);
            PathTracker.RecordIfTracked(
                m, Map.Maps[1], new Point3D(1500, 1600, 5), new Point3D(1504, 1600, 5),
                new[] { Direction.East, Direction.East }, default
            );

            PathTracker.Clear(); // flush + close
            observer.Delete();
            m.Delete();

            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path).TrimEnd();
            Assert.Single(content.Split('\n'));
            Assert.Contains("\"mapId\":1", content);
            Assert.Contains("\"sx\":1500", content);
            Assert.Contains("\"gx\":1504", content);
            Assert.Contains("\"len\":2", content);
            Assert.Contains("\"served\":", content);
            Assert.Contains("\"built\":", content);
            Assert.Contains("\"fell\":", content);
        }
        finally
        {
            PathTracker.Clear();
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void RecordIfTracked_DeletedMob_IsPruned()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            var observer = new TrackStub(World.NewMobile);
            observer.DefaultMobileInit();
            observer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var m = new TrackStub(World.NewMobile);
            m.DefaultMobileInit();
            m.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            PathTracker.Toggle(observer, m);
            Assert.True(PathTracker.IsTracked(m));

            m.Delete();
            PathTracker.RecordIfTracked(
                m, Map.Maps[1], new Point3D(1500, 1600, 0), new Point3D(1502, 1600, 0),
                null, default
            );

            Assert.False(PathTracker.IsTracked(m));
            observer.Delete();
        }
        finally
        {
            PathTracker.Clear();
            if (File.Exists(path)) { File.Delete(path); }
        }
    }

    [Fact]
    public void Find_OnTrackedMob_AppendsLine()
    {
        var path = NewTempPath();
        OverrideOutputPath(path);

        try
        {
            var observer = new TrackStub(World.NewMobile);
            observer.DefaultMobileInit();
            observer.MoveToWorld(new Point3D(1500, 1600, 0), Map.Maps[1]);

            var m = new TrackStub(World.NewMobile);
            m.DefaultMobileInit();
            var start = new Point3D(1496, 1628, 10);
            m.MoveToWorld(start, Map.Maps[1]);

            PathTracker.Toggle(observer, m);

            // A short, real route on Trammel (same family as the open_plain corpus scenario).
            Server.PathAlgorithms.BitmapAStarAlgorithm.Instance.Find(
                m, Map.Maps[1], start, new Point3D(1500, 1628, 10)
            );

            PathTracker.Clear(); // flush + close
            observer.Delete();
            m.Delete();

            Assert.True(File.Exists(path), "a tracked mob's Find must append a line");
            var content = File.ReadAllText(path).TrimEnd();
            Assert.Contains("\"sx\":1496", content);
            Assert.Contains("\"gx\":1500", content);
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
