using System;
using CalcMoves = Server.Movement.Movement;

namespace Server.Engines.Pathing.Cache;

/// <summary>
/// Computes static-only walkability for a single cell — the per-cell, per-direction
/// "can step" mask and destination Z, based purely on land + statics + multis. Mirrors
/// <see cref="MovementImpl"/>.Check minus the item and mobile collision phases.
/// </summary>
/// <remarks>
/// Bakes two rule sets per cell: walker (canSwim=false, cantWalk=false) and swim-only
/// (canSwim=true, cantWalk=true). Item / mobile collision phases are omitted (they're
/// the dynamic-obstacle pass's job). Diagonal corner-cut is NOT applied here; callers
/// must AND the partner-cell results at query time.
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

        GetStaticStartZ(map, x, y, sourceZ, canSwim: false, cantWalk: false,
            out var walkStartZ, out var walkStartTop, out _);
        GetStaticStartZ(map, x, y, sourceZ, canSwim: true, cantWalk: true,
            out var swimStartZ, out var swimStartTop, out _);

        byte walkMask = 0;
        byte wetMask = 0;
        Span<sbyte> walkZs = stackalloc sbyte[8];
        Span<sbyte> swimZs = stackalloc sbyte[8];
        // stackalloc is NOT zero-initialized — unwritten slots hold whatever was on the
        // stack. Clear before use; the loop only writes slots where the step succeeds.
        walkZs.Clear();
        swimZs.Clear();

        for (var d = 0; d < 8; d++)
        {
            var dx = x;
            var dy = y;
            CalcMoves.Offset((Direction)d, ref dx, ref dy);

            if (CheckStaticStep(map, dx, dy, walkStartZ, walkStartTop,
                    canSwim: false, cantWalk: false, out var walkZ))
            {
                walkMask |= (byte)(1 << d);
                walkZs[d] = (sbyte)walkZ;
            }

            if (CheckStaticStep(map, dx, dy, swimStartZ, swimStartTop,
                    canSwim: true, cantWalk: true, out var swimZ))
            {
                wetMask |= (byte)(1 << d);
                swimZs[d] = (sbyte)swimZ;
            }
        }

        return new StepMask(
            walkMask, wetMask,
            walkZs[0], walkZs[1], walkZs[2], walkZs[3],
            walkZs[4], walkZs[5], walkZs[6], walkZs[7],
            swimZs[0], swimZs[1], swimZs[2], swimZs[3],
            swimZs[4], swimZs[5], swimZs[6], swimZs[7]
        );
    }

    /// <summary>
    /// Returns the slow path's standing-Z for a default walker at (x, y). Mirrors
    /// MovementImpl.Check's surface-selection — paver Z+1 for paver-over-ground,
    /// landCenter for bare land. Used by <see cref="StepCache"/> to bake SourceZ so
    /// A*'s tracked-per-cell Z matches the cache's bake-time assumption.
    /// </summary>
    public static int ComputeStandingZ(Map map, int x, int y, int locZ)
    {
        GetStaticStartZ(map, x, y, locZ, canSwim: false, cantWalk: false,
            out _, out _, out var zCenter);
        return zCenter;
    }

    /// <summary>
    /// Mirrors GetStartZ from MovementImpl, parameterized by canSwim / cantWalk.
    /// </summary>
    private static void GetStaticStartZ(
        Map map, int x, int y, int locZ, bool canSwim, bool cantWalk,
        out int zLow, out int zTop, out int zCenter
    )
    {
        var landTile = map.Tiles.GetLandTile(x, y);
        var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        var impassable = (flags & TileFlag.Impassable) != 0;

        // Mirrors MovementImpl: impassable + swim on water is OK; otherwise block on
        // cantWalk or impassable.
        var landBlocks = (cantWalk || impassable)
            && !(impassable && canSwim && (flags & TileFlag.Wet) != 0);

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

            if (isSet && calcTop < zCenter || locZ < calcTop || !id.Surface && !(canSwim && id.Wet))
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
    /// Mirrors MovementImpl.Check for static tiles only, parameterized by canSwim / cantWalk.
    /// Items and mobile collision phases are omitted.
    /// </summary>
    private static bool CheckStaticStep(
        Map map, int x, int y, int startZ, int startTop, bool canSwim, bool cantWalk,
        out int newZ
    )
    {
        newZ = 0;

        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        var landTile = map.Tiles.GetLandTile(x, y);
        var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
        var impassable = (flags & TileFlag.Impassable) != 0;

        var landBlocks = (cantWalk || impassable)
            && !(impassable && canSwim && (flags & TileFlag.Wet) != 0);

        var considerLand = !landTile.Ignored;

        map.GetAverageZ(x, y, out var landZ, out var landCenter, out _);

        var moveIsOk = false;

        var stepTop = startTop + StepHeight;
        var checkTop = startZ + PersonHeight;

        int testTop;

        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(x, y))
        {
            var itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
            var notWater = !itemData.Wet;

            // Mirrors MovementImpl: skip if not a passable surface AND not swimmable water,
            // OR if the mobile can't walk and this isn't water.
            if ((!itemData.Surface || itemData.Impassable) && (!canSwim || notWater)
                || cantWalk && notWater)
            {
                continue;
            }

            var itemZ = tile.Z;
            var itemTop = itemZ;
            var ourZ = itemZ + itemData.CalcHeight;
            testTop = checkTop;

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

            if (StaticsBlockAt(map, x, y, ourZ, testTop))
            {
                continue;
            }

            newZ = ourZ;
            moveIsOk = true;
        }

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
