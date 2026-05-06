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

        Assert.Equal(0, result.Mask);
        for (var d = 0; d < 8; d++)
        {
            Assert.Equal(0, result.GetDestZ((Direction)d));
        }
    }

    [Fact]
    public void ComputeMaskAt_InternalMap_ReturnsDefault()
    {
        var result = StepProbe.ComputeMaskAt(Map.Internal, 100, 100, 0);

        Assert.Equal(0, result.Mask);
        for (var d = 0; d < 8; d++)
        {
            Assert.Equal(0, result.GetDestZ((Direction)d));
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

    [Fact]
    public void ComputeMaskAt_TrammelOpenPlain_HasFlatCellWithAllDirectionsWalkable()
    {
        // Invariant: somewhere in the open-plain region, there must be a fully-flat cell
        // whose mask is 0xFF and all 8 destZ values equal sourceZ (no slope).
        // This protects against a regression where the baker stops detecting flat cells.
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var found = false;

        for (var x = 1500; x < 1532 && !found; x++)
        {
            for (var y = 1600; y < 1632 && !found; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);
                var sourceZ = (sbyte)avgZ;
                var result = StepProbe.ComputeMaskAt(map, x, y, sourceZ);

                if (result.Mask != 0xFF)
                {
                    continue;
                }

                var allSameZ = true;
                for (var d = 0; d < 8; d++)
                {
                    if (result.GetDestZ((Direction)d) != sourceZ)
                    {
                        allSameZ = false;
                        break;
                    }
                }

                if (allSameZ)
                {
                    found = true;
                    _output.WriteLine($"Flat-open cell found at ({x}, {y}, {sourceZ})");
                }
            }
        }

        Assert.True(found, "Expected at least one fully-flat open cell in (1500..1532, 1600..1632)");
    }

    [Fact]
    public void ComputeMaskAt_BritainInnDense_HasCellWithBlockedDirections()
    {
        // Invariant: inside the dense Britain inn region, at least one cell must have
        // at least one direction blocked by a static. Protects against a regression
        // where the baker reports everything as walkable (the original false-pass bug).
        var map = Map.Maps[1];
        Assert.NotNull(map);

        var blockedCellCount = 0;

        for (var x = 1480; x < 1512; x++)
        {
            for (var y = 1610; y < 1642; y++)
            {
                map.GetAverageZ(x, y, out _, out var avgZ, out _);
                var sourceZ = (sbyte)avgZ;
                var result = StepProbe.ComputeMaskAt(map, x, y, sourceZ);

                if (result.Mask != 0xFF)
                {
                    blockedCellCount++;
                }
            }
        }

        _output.WriteLine($"Cells with at least one blocked direction: {blockedCellCount} / 1024");
        Assert.True(blockedCellCount > 0, "Expected at least one cell with a blocked direction in the dense region");
    }

    [Theory]
    [InlineData(1500, 1600)]
    [InlineData(1480, 1610)]
    [InlineData(0, 0)]
    public void ComputeMaskAt_IsDeterministic(int x, int y)
    {
        // Same inputs must always produce identical output. Catches accidental
        // statefulness in the baker (e.g., a static cache that gets corrupted).
        var map = Map.Maps[1];
        Assert.NotNull(map);

        map.GetAverageZ(x, y, out _, out var avgZ, out _);
        var sourceZ = (sbyte)avgZ;

        var first = StepProbe.ComputeMaskAt(map, x, y, sourceZ);
        var second = StepProbe.ComputeMaskAt(map, x, y, sourceZ);
        var third = StepProbe.ComputeMaskAt(map, x, y, sourceZ);

        Assert.Equal(first.Mask, second.Mask);
        Assert.Equal(first.Mask, third.Mask);
        for (var d = 0; d < 8; d++)
        {
            Assert.Equal(first.GetDestZ((Direction)d), second.GetDestZ((Direction)d));
            Assert.Equal(first.GetDestZ((Direction)d), third.GetDestZ((Direction)d));
        }
    }

    [Fact]
    public void ComputeMaskAt_PinnedCell_TrammelOpenPlainOrigin()
    {
        // PINNING test: locks specific output for Trammel (1500, 1600).
        // Cell at z=10 with mask 0xC1 (N + W + NW only walkable). The other five
        // directions are blocked by water (E/SE/S/SW are wet tiles, NE is shore).
        // If this changes, either the baker logic changed or the underlying tile
        // data changed; re-run the parity test to confirm before updating expected values.
        var map = Map.Maps[1];
        Assert.NotNull(map);

        map.GetAverageZ(1500, 1600, out _, out var avgZ, out _);
        var sourceZ = (sbyte)avgZ;

        var result = StepProbe.ComputeMaskAt(map, 1500, 1600, sourceZ);

        Assert.Equal((sbyte)10, sourceZ);
        Assert.Equal((byte)0xC1, result.Mask);

        Assert.True(result.IsWalkable(Direction.North));
        Assert.False(result.IsWalkable(Direction.Right));
        Assert.False(result.IsWalkable(Direction.East));
        Assert.False(result.IsWalkable(Direction.Down));
        Assert.False(result.IsWalkable(Direction.South));
        Assert.False(result.IsWalkable(Direction.Left));
        Assert.True(result.IsWalkable(Direction.West));
        Assert.True(result.IsWalkable(Direction.Up));

        Assert.Equal((sbyte)10, result.GetDestZ(Direction.North));
        Assert.Equal((sbyte)10, result.GetDestZ(Direction.West));
        Assert.Equal((sbyte)10, result.GetDestZ(Direction.Up));
    }

    [Fact]
    public void ComputeMaskAt_PinnedCell_BritainInnDenseOrigin()
    {
        // PINNING test: locks specific output for Trammel (1480, 1610).
        // Cell at z=20 with mask 0x3F (N/NE/E/SE/S/SW walkable, W/NW blocked by
        // a wall to the west). All walkable directions stay flat at z=20.
        var map = Map.Maps[1];
        Assert.NotNull(map);

        map.GetAverageZ(1480, 1610, out _, out var avgZ, out _);
        var sourceZ = (sbyte)avgZ;

        var result = StepProbe.ComputeMaskAt(map, 1480, 1610, sourceZ);

        Assert.Equal((sbyte)20, sourceZ);
        Assert.Equal((byte)0x3F, result.Mask);

        Assert.True(result.IsWalkable(Direction.North));
        Assert.True(result.IsWalkable(Direction.Right));
        Assert.True(result.IsWalkable(Direction.East));
        Assert.True(result.IsWalkable(Direction.Down));
        Assert.True(result.IsWalkable(Direction.South));
        Assert.True(result.IsWalkable(Direction.Left));
        Assert.False(result.IsWalkable(Direction.West));
        Assert.False(result.IsWalkable(Direction.Up));

        for (var d = 0; d < 6; d++)
        {
            Assert.Equal((sbyte)20, result.GetDestZ((Direction)d));
        }
    }
}
