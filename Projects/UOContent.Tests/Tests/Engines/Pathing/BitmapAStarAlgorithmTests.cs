using Server.Engines.Pathing.Cache;
using Server.Mobiles;
using Server.PathAlgorithms.BitmapAStar;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

/// <summary>
/// Smoke tests for <see cref="BitmapAStarAlgorithm"/>'s two branches: cache-direct fast
/// path (default walkers) and per-cell slow path (capability creatures and non-GM players).
/// Exercised end-to-end against the real Trammel TileMatrix to ensure neither regresses
/// to "no path found" on reachable goals.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class BitmapAStarAlgorithmTests
{
    private readonly ITestOutputHelper _output;

    public BitmapAStarAlgorithmTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    // Pinned cell (1500, 1600, z=10): mask=0xC1 → N, W, NW walkable.
    // Use start.Z for goal.Z so destNode lands in the same Z plane the algorithm reaches
    // during expansion (GetAverageZ at the goal cell may differ from the engine's
    // runtime-computed standing Z, which would break the destNode equality check).
    [InlineData(1500, 1600, 1498, 1598)] // NW, 2 cells diagonal
    [InlineData(1500, 1600, 1497, 1599)] // NW-ish, 3 W + 1 N
    public void DefaultWalker_FindsPath_ViaCacheFastPath(int sx, int sy, int gx, int gy)
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new DefaultWalkerStub();
        map.GetAverageZ(sx, sy, out _, out var startZ, out _);
        var start = new Point3D(sx, sy, (sbyte)startZ);
        var goal = new Point3D(gx, gy, (sbyte)startZ);

        stub.MoveToWorld(start, map);

        var result = BitmapAStarAlgorithm.Instance.Find(stub, map, start, goal);

        stub.Delete();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        _output.WriteLine($"default-walker ({sx},{sy})->({gx},{gy}): {result.Length} steps");
    }

    [Theory]
    [InlineData(1500, 1600, 1498, 1598)] // NW, 2 cells diagonal
    [InlineData(1500, 1600, 1497, 1599)] // NW-ish, 3 W + 1 N
    public void CapabilityCreature_FindsPath_ViaInlineSlowPath(int sx, int sy, int gx, int gy)
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new SwimmingStub(World.NewMobile);
        stub.DefaultMobileInit();
        stub.CanSwim = true; // forces non-default-walker → inline GetSuccessorsSlowPath
        map.GetAverageZ(sx, sy, out _, out var startZ, out _);
        var start = new Point3D(sx, sy, (sbyte)startZ);
        var goal = new Point3D(gx, gy, (sbyte)startZ);

        stub.MoveToWorld(start, map);

        var result = BitmapAStarAlgorithm.Instance.Find(stub, map, start, goal);

        stub.Delete();

        // Capability creature: assert reachability — exact length depends on terrain and
        // tie-breaking, but a swimmer should always reach a goal a default walker reaches
        // on dry land (CanSwim is permissive, never restrictive).
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        _output.WriteLine($"swimmer ({sx},{sy})->({gx},{gy}): {result.Length} steps");
    }

    [Fact]
    public void NonGmPlayer_UsesCache_WithStrictDiagonalRule()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var stub = new PlayerStub();
        map.GetAverageZ(1500, 1600, out _, out var startZ, out _);
        var start = new Point3D(1500, 1600, (sbyte)startZ);
        var goal = new Point3D(1498, 1598, (sbyte)startZ);

        stub.MoveToWorld(start, map);

        var statsBefore = StepCache.Instance.GetStats();
        var result = BitmapAStarAlgorithm.Instance.Find(stub, map, start, goal);
        var statsAfter = StepCache.Instance.GetStats();

        stub.Delete();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // BuildsTotal increments on every chunk build, which happens only when the cache
        // is queried. Slow path never touches the cache.
        Assert.True(statsAfter.BuildsTotal > statsBefore.BuildsTotal,
            "Non-GM player should use the cache, not the slow path");
    }

    [Fact]
    public void DoorCreature_UsesCache_NotSlowPath()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[1];

        var stub = new DoorOpenerStub(World.NewMobile);
        stub.DefaultMobileInit();
        map.GetAverageZ(1500, 1600, out _, out var startZ, out _);
        var start = new Point3D(1500, 1600, (sbyte)startZ);
        var goal = new Point3D(1498, 1598, (sbyte)startZ);

        stub.MoveToWorld(start, map);

        var statsBefore = StepCache.Instance.GetStats();
        var result = BitmapAStarAlgorithm.Instance.Find(stub, map, start, goal);
        var statsAfter = StepCache.Instance.GetStats();

        stub.Delete();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(statsAfter.BuildsTotal > statsBefore.BuildsTotal,
            "CanOpenDoors creature should use the cache (doors are dynamic items)");
    }

    [Fact]
    public void ObstacleCreature_UsesCache_NotSlowPath()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[1];

        var stub = new ObstacleClimberStub(World.NewMobile);
        stub.DefaultMobileInit();
        map.GetAverageZ(1500, 1600, out _, out var startZ, out _);
        var start = new Point3D(1500, 1600, (sbyte)startZ);
        var goal = new Point3D(1498, 1598, (sbyte)startZ);

        stub.MoveToWorld(start, map);

        var statsBefore = StepCache.Instance.GetStats();
        var result = BitmapAStarAlgorithm.Instance.Find(stub, map, start, goal);
        var statsAfter = StepCache.Instance.GetStats();

        stub.Delete();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(statsAfter.BuildsTotal > statsBefore.BuildsTotal,
            "CanMoveOverObstacles creature should use the cache (movables are dynamic items)");
    }

    /// <summary>
    /// Plain Mobile — RequiresSlowPath returns false, the bitmap algorithm uses the cache
    /// fast path on every expansion.
    /// </summary>
    private sealed class DefaultWalkerStub : Mobile
    {
        public DefaultWalkerStub()
        {
            Body = 0xC9;
        }
    }

    /// <summary>
    /// Mobile with Player=true and default AccessLevel (Player). Triggers the strict
    /// AND-rule for diagonal corner-cut while still using the cache.
    /// </summary>
    private sealed class PlayerStub : Mobile
    {
        public PlayerStub()
        {
            Body = 0xC9;
            Player = true;
        }
    }

    /// <summary>
    /// BaseCreature with CanSwim=true — RequiresSlowPath returns true, the bitmap
    /// algorithm short-circuits GetSuccessors to GetSuccessorsSlowPath on every cell.
    /// Use the Serial constructor (deserialization path) to bypass NPCSpeeds init,
    /// which requires the npc-speeds.json table loaded — not available in tests.
    /// </summary>
    private sealed class SwimmingStub : BaseCreature
    {
        public SwimmingStub(Serial serial) : base(serial)
        {
            Body = 0xC9;
        }
    }

    private sealed class DoorOpenerStub : BaseCreature
    {
        public DoorOpenerStub(Serial serial) : base(serial)
        {
            Body = 0xC9;
        }

        public override bool CanOpenDoors => true;
    }

    private sealed class ObstacleClimberStub : BaseCreature
    {
        public ObstacleClimberStub(Serial serial) : base(serial)
        {
            Body = 0xC9;
        }

        public override bool CanMoveOverObstacles => true;
    }
}
