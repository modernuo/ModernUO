using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

[Collection("Sequential Pathfinding Tests")]
public class MultiMaskSynthesisTests
{
    private const int MapId = 1;            // Trammel
    private const int GuildHouseId = 0x74;  // static house: walls, door aperture, floor
    private const int PlaceX = 1480;
    private const int PlaceY = 1620;

    [Fact]
    public void ComputeMultiMaskAt_MatchesCheckMovement_OverFootprintAndHalo()
    {
        StepCache.Instance.Clear();
        var map = Map.Maps[MapId];
        map.GetAverageZ(PlaceX, PlaceY, out _, out var z, out _);
        var loc = new Point3D(PlaceX, PlaceY, (sbyte)z);

        var multi = new TestMulti(GuildHouseId);
        try
        {
            multi.MoveToWorld(loc, map);
            MultiTestSupport.AssertSynthesizerMatchesCheckMovement(multi, map);
        }
        finally
        {
            multi.Delete();
        }
    }
}
