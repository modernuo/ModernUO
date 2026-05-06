using System;
using CalcMoves = Server.Movement.Movement;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Computes static-only walkability for a single cell — the per-cell, per-direction
/// "can step" mask and destination Z, based purely on land + statics + multis. Mirrors
/// <see cref="MovementImpl"/>.Check minus the item and mobile collision phases.
/// </summary>
/// <remarks>
/// Default-walker scope: assumes CanSwim=false, CanFly=false, CanOpenDoors=false,
/// CantWalk=false. Single source-Z per cell. Diagonal corner-cut is NOT applied here;
/// callers must AND the partner-cell results at query time per the creature rule
/// (one cardinal partner walkable suffices).
/// </remarks>
public static class StepProbe
{
    private const int PersonHeight = 16;
    private const int StepHeight = 2;

    public static StepMask ComputeMaskAt(Map map, int x, int y, sbyte sourceZ)
    {
        if (map == null || map == Map.Internal)
        {
            return default;
        }

        GetStaticStartZ(map, x, y, sourceZ, out var startZ, out var startTop, out _);

        byte mask = 0;
        Span<sbyte> destZs = stackalloc sbyte[8];

        for (var d = 0; d < 8; d++)
        {
            var dx = x;
            var dy = y;
            CalcMoves.Offset((Direction)d, ref dx, ref dy);

            if (CheckStaticStep(map, dx, dy, startZ, startTop, out var newZ))
            {
                mask |= (byte)(1 << d);
                destZs[d] = (sbyte)newZ;
            }
        }

        return new StepMask(
            mask,
            destZs[0], destZs[1], destZs[2], destZs[3],
            destZs[4], destZs[5], destZs[6], destZs[7]
        );
    }

    /// <summary>
    /// Returns the slow path's standing-Z for a default walker at (x, y) with hint locZ.
    /// This is the Z the creature ends up STANDING AT — typically the topmost walkable
    /// surface that's reachable from locZ (paver Z+1 for paver-over-ground; landCenter
    /// for bare land). Mirrors MovementImpl.Check's surface-selection logic for the
    /// destination cell, distilled to "what Z value does the slow path return as newZ
    /// when stepping ONTO this cell". Used by StepCache to bake SourceZ
    /// correctly so A*'s tracked-per-cell Z matches the cache's bake-time assumption.
    /// </summary>
    public static int ComputeStandingZ(Map map, int x, int y, int locZ)
    {
        GetStaticStartZ(map, x, y, locZ, out _, out _, out var zCenter);
        return zCenter;
    }

    /// <summary>
    /// Mirrors GetStartZ from MovementImpl, but static-only (no item list).
    /// Assumes default walker: CanSwim=false, CantWalk=false.
    /// </summary>
    private static void GetStaticStartZ(Map map, int x, int y, int locZ, out int zLow, out int zTop, out int zCenter)
    {
        var landTile = map.Tiles.GetLandTile(x, y);
        var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        var impassable = (flags & TileFlag.Impassable) != 0;

        // CantWalk=false, CanSwim=false → landBlocks = impassable
        var landBlocks = impassable;

        map.GetAverageZ(x, y, out var landZ, out var landCenter, out var landTop);

        var considerLand = !landTile.Ignored;

        zCenter = zLow = zTop = 0;
        var isSet = false;

        if (considerLand && !landBlocks && locZ >= landCenter)
        {
            zLow = landZ;
            zCenter = landCenter;
            zTop = landTop;
            isSet = true;
        }

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            var calcTop = tile.Z + id.CalcHeight;

            // CanSwim=false → only check Surface; CantWalk=false
            if (isSet && calcTop < zCenter || locZ < calcTop || !id.Surface)
            {
                continue;
            }

            zLow = tile.Z;
            zCenter = calcTop;

            var top = tile.Z + id.Height;

            if (!isSet || top > zTop)
            {
                zTop = top;
            }

            isSet = true;
        }

        if (!isSet)
        {
            zLow = zTop = locZ;
        }
        else if (locZ > zTop)
        {
            zTop = locZ;
        }
    }

    /// <summary>
    /// Mirrors MovementImpl.Check for static tiles only.
    /// Assumes default walker: CanSwim=false, CanFly=false, CantWalk=false,
    /// AlwaysIgnoreDoors=false. Items and mobile collision phases are omitted.
    /// </summary>
    private static bool CheckStaticStep(Map map, int x, int y, int startZ, int startTop, out int newZ)
    {
        newZ = 0;

        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        var landTile = map.Tiles.GetLandTile(x, y);
        var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        var impassable = (flags & TileFlag.Impassable) != 0;

        // CantWalk=false, CanSwim=false → landBlocks = impassable
        var landBlocks = impassable;

        var considerLand = !landTile.Ignored;

        map.GetAverageZ(x, y, out var landZ, out var landCenter, out _);

        var moveIsOk = false;

        var stepTop = startTop + StepHeight;
        var checkTop = startZ + PersonHeight;

        int testTop;

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            // CanSwim=false, CantWalk=false:
            // Skip if not a passable surface (no swim path either)
            if (!itemData.Surface || itemData.Impassable)
            {
                continue;
            }

            var itemZ = tile.Z;
            var itemTop = itemZ;
            var ourZ = itemZ + itemData.CalcHeight;
            testTop = checkTop;

            // Pick the candidate closest to startZ; ties broken by higher ourZ
            if (moveIsOk)
            {
                var cmp = Math.Abs(ourZ - startZ) - Math.Abs(newZ - startZ);

                if (cmp > 0 || cmp == 0 && ourZ > newZ)
                {
                    continue;
                }
            }

            if (ourZ + PersonHeight > testTop)
            {
                testTop = ourZ + PersonHeight;
            }

            if (!itemData.Bridge)
            {
                itemTop += itemData.Height;
            }

            if (stepTop < itemTop)
            {
                continue;
            }

            var landCheck = itemZ + Math.Min(itemData.Height, StepHeight);

            if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
            {
                continue;
            }

            // IsOk equivalent: check static tiles don't block (ourZ, testTop) space
            if (StaticsBlockAt(map, x, y, ourZ, testTop))
            {
                continue;
            }

            newZ = ourZ;
            moveIsOk = true;
        }

        // Land surface fallback (mirrors Check's land block at the bottom)
        if (!considerLand || landBlocks || stepTop < landZ)
        {
            return moveIsOk;
        }

        testTop = checkTop;

        if (landCenter + PersonHeight > testTop)
        {
            testTop = landCenter + PersonHeight;
        }

        var shouldCheck = true;

        if (moveIsOk)
        {
            var cmp = Math.Abs(landCenter - startZ) - Math.Abs(newZ - startZ);

            if (cmp > 0 || cmp == 0 && landCenter > newZ)
            {
                shouldCheck = false;
            }
        }

        if (shouldCheck && !StaticsBlockAt(map, x, y, landCenter, testTop))
        {
            newZ = landCenter;
            moveIsOk = true;
        }

        return moveIsOk;
    }

    /// <summary>
    /// Mirrors the static-tile portion of IsOk: returns true if any static tile at (x,y)
    /// has ImpassableSurface and overlaps the vertical range (ourZ, testTop).
    /// </summary>
    private static bool StaticsBlockAt(Map map, int x, int y, int ourZ, int testTop)
    {
        foreach (var check in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var itemData = TileData.ItemTable[check.ID & TileData.MaxItemValue];

            if (itemData.ImpassableSurface)
            {
                var checkZ = check.Z;
                var checkTop = checkZ + itemData.CalcHeight;

                if (checkTop > ourZ && testTop > checkZ)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
