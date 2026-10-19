using Server.Engines.Pathing.Cache;
using Server.Items;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class BoatPathTests
{
    private readonly ITestOutputHelper _output;

    public BoatPathTests(ITestOutputHelper output) => _output = output;

    // Open water in the south-Britain bay (Trammel), used by the existing swim-bake test.
    private const int MapId = 1;
    private const int WaterX = 1450;
    private const int WaterY = 1770;

    [SkippableFact]
    public void BoatDeck_HasWalkableSurfaceCells()
    {
        TileDataRequirement.SkipIfMissing();
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];

        // SmallBoat North heading = multiID 0x0. A deck is made of surface tiles.
        var boat = new TestMulti(0x0);
        map.GetAverageZ(WaterX, WaterY, out _, out var z, out _);
        boat.MoveToWorld(new Point3D(WaterX, WaterY, (sbyte)z), map);

        try
        {
            var floor = MultiArt.FindFloorCell(boat);
            Assert.True(floor.HasValue, "non-vacuity: boat deck must have surface (floor) tiles");

            // The deck cell falls through to the live multi-aware path.
            var mask = StepCache.Instance.TryGetMask(map, floor.Value.X, floor.Value.Y, (sbyte)z);
            Assert.Equal(CacheHitKind.Fallthrough_Multi, mask.HitKind);
            _output.WriteLine($"boat deck floor cell at ({floor.Value.X},{floor.Value.Y})");
        }
        finally
        {
            boat.Delete();
        }
    }

    private static System.Collections.Generic.HashSet<(int lx, int ly)> LocalShape(BaseMulti multi)
    {
        var mcl = multi.Components;
        var set = new System.Collections.Generic.HashSet<(int, int)>();
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                if (mcl.Tiles[lx][ly].Length > 0)
                {
                    set.Add((lx, ly));
                }
            }
        }
        return set;
    }
}
