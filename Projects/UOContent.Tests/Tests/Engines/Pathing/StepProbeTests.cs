using Server.Engines.Pathing.Cache;
using Xunit;
using Xunit.Abstractions;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class StepProbeTests
{
    private readonly ITestOutputHelper _output;

    public StepProbeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ComputeMaskAt_NullMap_ReturnsDefault()
    {
        var result = StepProbe.ComputeMaskAt(null, 100, 100, 0);

        Assert.Equal(0, result.WalkMask);
        for (var d = 0; d < 8; d++)
        {
            Assert.Equal(0, result.GetWalkZ((Direction)d));
        }
    }

    [Fact]
    public void ComputeMaskAt_InternalMap_ReturnsDefault()
    {
        var result = StepProbe.ComputeMaskAt(Map.Internal, 100, 100, 0);

        Assert.Equal(0, result.WalkMask);
        for (var d = 0; d < 8; d++)
        {
            Assert.Equal(0, result.GetWalkZ((Direction)d));
        }
    }

    [Fact]
    public void ComputeMaskAt_TopLeftCorner_OffMapDirectionsBlocked()
    {
        // From source (0, 0), these neighbor cells are off-map and MUST be blocked,
        // regardless of map content:
        //   N  = (0, -1)   bit 0
        //   NE = (1, -1)   bit 1
        //   SW = (-1, 1)   bit 5
        //   W  = (-1, 0)   bit 6
        //   NW = (-1, -1)  bit 7
        // Bits 2 (E), 3 (SE), 4 (S) depend on map content and aren't asserted.
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var result = StepProbe.ComputeMaskAt(map, 0, 0, 0);

        Assert.False(result.IsWalkable(Direction.North), "N should be off-map");
        Assert.False(result.IsWalkable(Direction.Right), "NE should be off-map");
        Assert.False(result.IsWalkable(Direction.Left), "SW should be off-map");
        Assert.False(result.IsWalkable(Direction.West), "W should be off-map");
        Assert.False(result.IsWalkable(Direction.Up), "NW should be off-map");
    }

    [Fact]
    public void ComputeMaskAt_BottomRightCorner_OffMapDirectionsBlocked()
    {
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var x = map.Width - 1;
        var y = map.Height - 1;
        var result = StepProbe.ComputeMaskAt(map, x, y, 0);

        // From the SE corner, S/SE/SW/E/NE all go off-map (only N/NW/W in-map).
        Assert.False(result.IsWalkable(Direction.Right), "NE should be off-map");
        Assert.False(result.IsWalkable(Direction.East), "E should be off-map");
        Assert.False(result.IsWalkable(Direction.Down), "SE should be off-map");
        Assert.False(result.IsWalkable(Direction.South), "S should be off-map");
        Assert.False(result.IsWalkable(Direction.Left), "SW should be off-map");
    }
}
