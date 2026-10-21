using Server.Tests.Terrain;
using Server;
using Server.Engines.Pathing.Cache;
using Server.Items;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class FoundationRedesignTests
{
    private readonly ITestOutputHelper _output;

    public FoundationRedesignTests(ITestOutputHelper output) => _output = output;

    private const int MapId = 1;
    private const int PlaceX = 1500;
    private const int PlaceY = 1600;

    [Fact]
    public void RedesignReRegistered_RoutesNewFootprintToLivePath()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        Assert.NotNull(map);

        var foundation = new SwappableFoundation(TestMultis.GuildHouse);
        var shared = MultiData.GetComponents(TestMultis.GuildHouse);
        var seededShape = (shared.Width, shared.Height, shared.List.Length);
        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);
        var loc = new Point3D(PlaceX, PlaceY, (sbyte)z);
        foundation.MoveToWorld(loc, map);

        try
        {
            // Current design's covered cells route to the live (multi-aware) path.
            var before = MultiArt.FindFloorCell(foundation);
            Assert.True(before.HasValue, "non-vacuity: initial design must have a floor cell");
            var maskBefore = StepCache.Instance.TryGetMask(map, before.Value.X, before.Value.Y, (sbyte)z);
            Assert.Equal(CacheHitKind.Fallthrough_Multi, maskBefore.HitKind);

            // Redesign, RE-REGISTERED so sectors track the new footprint (model a real commit).
            // Internalize() moves the multi to Map.Internal, which fires Map.OnLeave and removes
            // the OLD footprint's sector registration. We then swap the MCL and MoveToWorld back,
            // which fires Map.OnEnter -> AddMulti against the fresh Components (new footprint).
            foundation.Internalize();
            foundation.Redesign(MultiData.GetComponents(TestMultis.Tower));
            foundation.MoveToWorld(loc, map);

            var after = MultiArt.FindFloorCell(foundation);
            Assert.True(after.HasValue, "non-vacuity: redesigned design must have a floor cell");
            // The new footprint cell must be registered (HasMultis) and route to the live path.
            var sector = map.GetRealSector(after.Value.X >> 4, after.Value.Y >> 4);
            Assert.True(sector.HasMultis, "redesigned footprint must re-register its sector");
            var maskAfter = StepCache.Instance.TryGetMask(map, after.Value.X, after.Value.Y, (sbyte)z);
            Assert.Equal(CacheHitKind.Fallthrough_Multi, maskAfter.HitKind);

            // The redesign swapped this foundation's list; the shared footprint every other GuildHouse reads is untouched.
            var seeded = MultiData.GetComponents(TestMultis.GuildHouse);
            Assert.Equal(seededShape, (seeded.Width, seeded.Height, seeded.List.Length));

            _output.WriteLine($"redesign re-registered: before {before.Value.X},{before.Value.Y} after {after.Value.X},{after.Value.Y}");
        }
        finally
        {
            if (!foundation.Deleted)
            {
                foundation.Delete();
            }
        }
    }
}
