using System.Collections.Generic;
using Server;
using Server.Engines.Pathing.Cache;
using Server.Items;
using Xunit;
using CalcMoves = Server.Movement.Movement;

namespace Server.Tests.Pathfinding;

/// <summary>
/// Minimal concrete <see cref="BaseMulti"/> for tests. Walkability depends only on
/// Components (the shared MCL for the multiID) + Location, so this is a faithful stand-in
/// for any fixed-design multi (classic house, camp, boat heading) without the owning
/// house/boat machinery. Never serialized in tests.
/// </summary>
public sealed class TestMulti : BaseMulti
{
    public TestMulti(int itemID) : base(itemID)
    {
    }
}

/// <summary>Default walker body, shared across pathfinding test fixtures.</summary>
public sealed class WalkerStub : Mobile
{
    public WalkerStub() => Body = 0xC9;
}

/// <summary>
/// Stand-in for a customizable foundation: Components is a swappable MCL, exactly the
/// runtime-mutation shape HouseFoundation uses (it replaces its MCL wholesale on redesign
/// commit). Lets the test change the footprint and assert the engine reflects it without
/// driving full house placement/customization.
/// </summary>
public sealed class SwappableFoundation : BaseMulti
{
    private MultiComponentList _mcl;

    public SwappableFoundation(int baseMultiID) : base(baseMultiID) =>
        _mcl = MultiData.GetComponents(baseMultiID);

    public override MultiComponentList Components => _mcl;

    public void Redesign(MultiComponentList replacement) => _mcl = replacement;
}

/// <summary>
/// Shared helpers for placing/probing multis in pathfinding tests.
/// </summary>
public static class MultiTestSupport
{
    // A default-walker oracle mobile (CanSwim=false, CantWalk=false) placed in-world so MovementImpl
    // state reads are valid. Caller MUST Delete() it (do it in a finally).
    public static Mobile GetWalkerOracle(Map map, Point3D loc)
    {
        var w = new WalkerStub();
        w.MoveToWorld(loc, map);
        return w;
    }

    public static bool HasMultiTileAt(BaseMulti multi, int wx, int wy)
    {
        var mcl = multi.Components;
        var lx = wx - multi.X + mcl.Center.X;
        var ly = wy - multi.Y + mcl.Center.Y;
        if (lx < 0 || ly < 0 || lx >= mcl.Width || ly >= mcl.Height)
        {
            return false;
        }
        return mcl.Tiles[lx][ly].Length > 0;
    }

    /// <summary>
    /// Sweeps the multi's footprint + 1-cell halo and asserts the multi mask synthesizer
    /// (<see cref="StepProbe.ComputeMultiMaskAt"/>) agrees with the <c>CheckMovement</c> oracle
    /// for all 8 directions at every cell, including the exact forward walk-Z on allowed moves.
    /// Creates its own walker oracle internally and Delete()s it; the caller owns the multi.
    /// </summary>
    public static void AssertSynthesizerMatchesCheckMovement(BaseMulti multi, Map map)
    {
        var loc = new Point3D(multi.X, multi.Y, multi.Z);
        var mover = GetWalkerOracle(map, loc);
        try
        {
            var cells = MultiArt.FootprintWithHalo(multi);
            Assert.NotEmpty(cells);

            var touchedMulti = 0;
            var sawWalkable = 0;
            var sawBlocked = 0;

            foreach (var c in cells)
            {
                var sourceZ = (sbyte)loc.Z;
                var p = new Point3D(c.X, c.Y, sourceZ);

                if (HasMultiTileAt(multi, c.X, c.Y))
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
        }
    }
}

/// <summary>
/// Helpers that derive expected geometry from a multi's MCL art at runtime, so tests
/// encode no hardcoded cell coordinates and survive art-data changes.
/// </summary>
public static class MultiArt
{
    public readonly record struct Cell(int X, int Y);

    /// <summary>Every world cell the multi's footprint covers (Tiles stack non-empty).</summary>
    public static List<Cell> FootprintCells(BaseMulti multi)
    {
        var mcl = multi.Components;
        var result = new List<Cell>();
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                if (mcl.Tiles[lx][ly].Length == 0)
                {
                    continue;
                }
                result.Add(new Cell(multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly));
            }
        }
        return result;
    }

    /// <summary>Footprint cells plus a 1-cell halo ring (the cells the split also routes to slow path).</summary>
    public static HashSet<Cell> FootprintWithHalo(BaseMulti multi)
    {
        var foot = FootprintCells(multi);
        var set = new HashSet<Cell>();
        foreach (var c in foot)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    set.Add(new Cell(c.X + dx, c.Y + dy));
                }
            }
        }
        return set;
    }

    /// <summary>First world cell whose MCL stack contains an impassable, non-surface (wall) tile, or null.</summary>
    public static Cell? FindWallCell(BaseMulti multi)
    {
        var mcl = multi.Components;
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                foreach (var t in mcl.Tiles[lx][ly])
                {
                    var data = TileData.ItemTable[t.ID & TileData.MaxItemValue];
                    if (data.Impassable && !data.Surface)
                    {
                        return new Cell(multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly);
                    }
                }
            }
        }
        return null;
    }

    /// <summary>First world cell whose MCL stack contains a walkable surface (floor) tile, or null.</summary>
    public static Cell? FindFloorCell(BaseMulti multi)
    {
        var mcl = multi.Components;
        for (var lx = 0; lx < mcl.Width; lx++)
        {
            for (var ly = 0; ly < mcl.Height; ly++)
            {
                foreach (var t in mcl.Tiles[lx][ly])
                {
                    var data = TileData.ItemTable[t.ID & TileData.MaxItemValue];
                    if (data.Surface && !data.Impassable)
                    {
                        return new Cell(multi.X + mcl.Min.X + lx, multi.Y + mcl.Min.Y + ly);
                    }
                }
            }
        }
        return null;
    }
}
