using Server.Engines.Pathing.Cache;
using Xunit;
using CalcMoves = Server.Movement.Movement;

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
        var mover = MultiTestSupport.GetWalkerOracle(map, loc);
        try
        {
            multi.MoveToWorld(loc, map);

            var cells = MultiArt.FootprintWithHalo(multi);
            Assert.NotEmpty(cells);

            var touchedMulti = 0;
            var sawWalkable = 0;
            var sawBlocked = 0;

            foreach (var c in cells)
            {
                var sourceZ = (sbyte)loc.Z;
                var p = new Point3D(c.X, c.Y, sourceZ);

                if (MultiTestSupport.HasMultiTileAt(multi, c.X, c.Y))
                {
                    touchedMulti++;
                }

                var mask = StepProbe.ComputeMultiMaskAt(map, c.X, c.Y, sourceZ);

                for (var d = 0; d < 8; d++)
                {
                    var dir = (Direction)d;
                    var expectWalk = CalcMoves.CheckMovement(mover, map, p, dir, out var expectZ);

                    // The synthesizer reports the raw forward-cell step per direction and does NOT
                    // apply diagonal corner-cutting — by design, the caller ANDs the partner cells.
                    // CheckMovement (the oracle) DOES corner-cut. Replicate the caller's corner-cut
                    // on the mask so we compare like-for-like. The walker is not a player, so the
                    // diagonal is blocked only when BOTH orthogonal partner cells are blocked.
                    var forwardWalk = (mask.WalkMask & (1 << d)) != 0;
                    var gotWalk = forwardWalk;
                    var isDiagonal = (d & 0x1) == 0x1;
                    if (forwardWalk && isDiagonal)
                    {
                        var leftBit = (d - 1) & 0x7;
                        var rightBit = (d + 1) & 0x7;
                        var leftWalk = (mask.WalkMask & (1 << leftBit)) != 0;
                        var rightWalk = (mask.WalkMask & (1 << rightBit)) != 0;
                        if (!leftWalk && !rightWalk)
                        {
                            gotWalk = false;
                        }
                    }

                    Assert.Equal(expectWalk, gotWalk);

                    if (expectWalk)
                    {
                        // Z is taken from the forward cell only; corner-cut never alters newZ when
                        // the move is allowed.
                        Assert.Equal((sbyte)expectZ, mask.GetWalkZ(dir));
                        sawWalkable++;
                    }
                    else
                    {
                        sawBlocked++;
                    }
                }
            }

            Assert.True(touchedMulti > 0, "sweep touched no multi-covered cells");
            Assert.True(sawWalkable > 0, "sweep observed no walkable transitions");
            Assert.True(sawBlocked > 0, "sweep observed no blocked transitions");
        }
        finally
        {
            mover.Delete();
            multi.Delete();
        }
    }
}
