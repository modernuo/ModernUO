using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Regions;
using Server.Spells;
using Server.Systems.FeatureFlags;

namespace Server.Multis
{
    public enum HousePlacementResult
    {
        Valid,
        BadRegion,
        BadLand,
        BadStatic,
        BadItem,
        NoSurface,
        BadRegionHidden,
        BadRegionTemp,
        InvalidCastleKeep,
        BadRegionRaffle
    }

    public static class HousePlacement
    {
        private const int YardSize = 5;

        // Any land tile which matches one of these ID numbers is considered a road and cannot be placed over.
        private static readonly int[] m_RoadIDs =
        {
            0x0071, 0x0078,
            0x00E8, 0x00EB,
            0x07AE, 0x07B1,
            0x3FF4, 0x3FF4,
            0x3FF8, 0x3FFB,
            0x0442, 0x0479, // Sand stones
            0x0501, 0x0510, // Sand stones
            0x0009, 0x0015, // Furrows
            0x0150, 0x015C  // Furrows
        };

        public static HousePlacementResult Check(
            Mobile from, int multiID, Point3D center, out List<IEntity> toMove, Direction houseFacing = Direction.South
        )
        {
            // If this spot is considered valid, every item and mobile in this list will be moved under the house sign
            toMove = new List<IEntity>();

            var map = from.Map;

            if (map == null || map == Map.Internal)
            {
                return HousePlacementResult.BadLand; // A house cannot go here
            }

            if (!ContentFeatureFlags.HousePlacement && from.AccessLevel < FeatureFlagSettings.RequiredAccessLevel)
            {
                return HousePlacementResult.BadRegionTemp;
            }

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                return HousePlacementResult.Valid; // Staff can place anywhere
            }

            if (map == Map.Ilshenar || SpellHelper.IsFeluccaT2A(map, center))
            {
                return HousePlacementResult.BadRegion; // No houses in Ilshenar/T2A
            }

            if (map == Map.Malas && multiID is 0x007C or 0x007E)
            {
                return HousePlacementResult.InvalidCastleKeep;
            }

            if (Region.Find(center, map).IsPartOf<NoHousingRegion, NoHousingGuardedRegion>())
            {
                return HousePlacementResult.BadRegion;
            }

            // This holds data describing the internal structure of the house
            var mcl = MultiData.GetComponents(multiID);

            if (multiID >= 0x13EC && multiID < 0x1D00)
            {
                HouseFoundation.AddStairsTo(ref mcl); // this is a AOS house, add the stairs
            }

            // Location of the northwest-most corner of the house
            var start = new Point3D(center.X + mcl.Min.X, center.Y + mcl.Min.Y, center.Z);

            // These are storage lists. They hold items and mobiles found in the map for further processing
            using var items = PooledRefList<Item>.Create();
            var mobiles = new List<Mobile>();

            // These are also storage lists. They hold location values indicating the yard and border locations.
            List<Point2D> borders = [];

            /* RULES:
             *
             * 1) All tiles which are around the -outside- of the foundation must not have anything impassable.
             * 2) No impassable object or land tile may come in direct contact with any part of the house.
             * 3) Five tiles from the front and back of the house must be completely clear of all house tiles.
             * 4) The foundation must rest flatly on a surface. Any bumps around the foundation are not allowed.
             * 5) No foundation tile may reside over terrain which is viewed as a road.
             */

            for (var x = 0; x < mcl.Width; ++x)
            {
                for (var y = 0; y < mcl.Height; ++y)
                {
                    var tileX = start.X + x;
                    var tileY = start.Y + y;

                    var addTiles = mcl.Tiles[x][y];

                    if (addTiles.Length == 0)
                    {
                        continue; // There are no tiles here, continue checking somewhere else
                    }

                    var testPoint = new Point3D(tileX, tileY, center.Z);

                    var reg = Region.Find(testPoint, map);

                    if (!reg.AllowHousing(from, testPoint)) // Cannot place houses in dungeons, towns, treasure map areas etc
                    {
                        if (reg.IsPartOf<TempNoHousingRegion>())
                        {
                            return HousePlacementResult.BadRegionTemp;
                        }

                        if (reg.IsPartOf<TreasureRegion, HouseRegion>())
                        {
                            return HousePlacementResult.BadRegionHidden;
                        }

                        if (reg.IsPartOf<HouseRaffleRegion>())
                        {
                            return HousePlacementResult.BadRegionRaffle;
                        }

                        return HousePlacementResult.BadRegion;
                    }

                    var landTile = map.Tiles.GetLandTile(tileX, tileY);
                    var landID = landTile.ID & TileData.MaxLandValue;

                    items.Clear();

                    foreach (var item in map.GetItemsAt(tileX, tileY))
                    {
                        if (item.Visible && item.X == tileX && item.Y == tileY)
                        {
                            items.Add(item);
                        }
                    }

                    mobiles.Clear();

                    foreach (var m in map.GetMobilesAt(tileX, tileY))
                    {
                        if (m.X == tileX && m.Y == tileY)
                        {
                            mobiles.Add(m);
                        }
                    }

                    map.GetAverageZ(tileX, tileY, out var landStartZ, out var landAvgZ, out _);

                    var hasFoundation = false;

                    for (var i = 0; i < addTiles.Length; ++i)
                    {
                        var addTile = addTiles[i];

                        if (addTile.ID == 0x1) // Nodraw
                        {
                            continue;
                        }

                        var addTileData = TileData.ItemTable[addTile.ID & TileData.MaxItemValue];

                        var isFoundation = addTile.Z == 0 && addTileData.Wall;
                        var hasSurface = false;

                        if (isFoundation)
                        {
                            hasFoundation = true;
                        }

                        var addTileZ = center.Z + addTile.Z;
                        var addTileTop = addTileZ + addTile.Height;

                        if (addTileData.Surface)
                        {
                            addTileTop += 16;
                        }

                        if (addTileTop > landStartZ && landAvgZ > addTileZ)
                        {
                            return HousePlacementResult.BadLand; // Broke rule #2
                        }

                        if (isFoundation &&
                            (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Impassable) == 0 &&
                            landAvgZ == center.Z)
                        {
                            hasSurface = true;
                        }

                        foreach (var tile in map.Tiles.GetStaticAndMultiTiles(tileX, tileY))
                        {
                            var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                            if ((id.Impassable || id.Surface && !id.Background) &&
                                addTileTop > tile.Z && tile.Z + id.CalcHeight > addTileZ)
                            {
                                return HousePlacementResult.BadStatic; // Broke rule #2
                            }

                            /*else if (isFoundation && !hasSurface && (id.Flags & TileFlag.Surface) != 0 && (oldTile.Z + id.CalcHeight) == center.Z)
                                hasSurface = true;*/
                        }

                        foreach (var item in items)
                        {
                            var id = item.ItemData;

                            if (addTileTop <= item.Z || item.Z + id.CalcHeight <= addTileZ)
                            {
                                continue;
                            }

                            if (item.Movable)
                            {
                                toMove.Add(item);
                            }
                            else if (id.Impassable || id.Surface && !id.Background)
                            {
                                return HousePlacementResult.BadItem; // Broke rule #2
                            }
                        }

                        if (isFoundation && !hasSurface)
                        {
                            return HousePlacementResult.NoSurface; // Broke rule #4
                        }

                        for (var j = 0; j < mobiles.Count; ++j)
                        {
                            var m = mobiles[j];

                            if (addTileTop > m.Z && m.Z + 16 > addTileZ)
                            {
                                toMove.Add(m);
                            }
                        }
                    }

                    for (var i = 0; i < m_RoadIDs.Length; i += 2)
                    {
                        if (landID >= m_RoadIDs[i] && landID <= m_RoadIDs[i + 1])
                        {
                            return HousePlacementResult.BadLand; // Broke rule #5
                        }
                    }

                    if (hasFoundation)
                    {
                        if (!CheckYard(map, tileX, tileY, YardSize, houseFacing))
                        {
                            return HousePlacementResult.BadStatic; // Broke rule #3
                        }

                        for (var xOffset = -1; xOffset <= 1; ++xOffset)
                        {
                            for (var yOffset = -1; yOffset <= 1; ++yOffset)
                            {
                                if (xOffset == 0 && yOffset == 0)
                                {
                                    continue;
                                }

                                // To ease this rule, we will not add to the border list if the tile here is under a base floor (z<=8)

                                var vx = x + xOffset;
                                var vy = y + yOffset;

                                if (vx >= 0 && vx < mcl.Width && vy >= 0 && vy < mcl.Height)
                                {
                                    var breakTiles = mcl.Tiles[vx][vy];
                                    var shouldBreak = false;

                                    for (var i = 0; !shouldBreak && i < breakTiles.Length; ++i)
                                    {
                                        var breakTile = breakTiles[i];

                                        if (breakTile.Height == 0 && breakTile.Z <= 8 &&
                                            TileData.ItemTable[breakTile.ID & TileData.MaxItemValue].Surface)
                                        {
                                            shouldBreak = true;
                                        }
                                    }

                                    if (shouldBreak)
                                    {
                                        continue;
                                    }
                                }

                                var borderPoint = new Point2D(tileX + xOffset, tileY + yOffset);

                                if (!borders.Contains(borderPoint))
                                {
                                    borders.Add(borderPoint);
                                }
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < borders.Count; ++i)
            {
                var borderPoint = borders[i];

                var landTile = map.Tiles.GetLandTile(borderPoint.X, borderPoint.Y);
                var landID = landTile.ID & TileData.MaxLandValue;

                if ((TileData.LandTable[landID].Flags & TileFlag.Impassable) != 0)
                {
                    return HousePlacementResult.BadLand;
                }

                for (var j = 0; j < m_RoadIDs.Length; j += 2)
                {
                    if (landID >= m_RoadIDs[j] && landID <= m_RoadIDs[j + 1])
                    {
                        return HousePlacementResult.BadLand; // Broke rule #5
                    }
                }

                foreach (var tile in map.Tiles.GetStaticAndMultiTiles(borderPoint.X, borderPoint.Y))
                {
                    var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                    if (id.Impassable || id.Surface && !id.Background && tile.Z + id.CalcHeight > center.Z + 2)
                    {
                        return HousePlacementResult.BadStatic; // Broke rule #1
                    }
                }

                foreach (var item in map.GetItemsAt(borderPoint))
                {
                    if (item.Movable)
                    {
                        continue;
                    }

                    var id = item.ItemData;

                    if (id.Impassable || id.Surface && !id.Background && item.Z + id.CalcHeight > center.Z + 2)
                    {
                        return HousePlacementResult.BadItem; // Broke rule #1
                    }
                }
            }

            return HousePlacementResult.Valid;
        }

        private static bool CheckYard(Map map, int tileX, int tileY, int yardSize, Direction houseFacing)
        {
            var isSouthFacing = (houseFacing & Direction.South) != 0;
            var isEastFacing = (houseFacing & Direction.East) != 0;

            for (var xOffset = -yardSize; xOffset <= yardSize; ++xOffset)
            {
                var absXOffset = Math.Abs(xOffset);
                for (var yOffset = -yardSize; yOffset <= yardSize; ++yOffset)
                {
                    var absYOffset = Math.Abs(yOffset);
                    var yardPoint = new Point2D(tileX + xOffset, tileY + yOffset);

                    var inSouthYard = yOffset > 0 && yOffset <= yardSize && absXOffset <= 1;
                    var inEastYard = xOffset > 0 && xOffset <= yardSize && absYOffset <= 1;
                    var inNorthYard = yOffset < 0 && yOffset >= -yardSize && absXOffset <= 1;
                    var inWestYard = xOffset < 0 && xOffset >= -yardSize && absYOffset <= 1;

                    // Check each house at this point
                    foreach (var house in map.GetMultisInSector<BaseHouse>(yardPoint))
                    {
                        if (!house.Contains(yardPoint))
                        {
                            continue;
                        }

                        var existingHouseFacing = house.HouseDirection;
                        var existingHouseIsSouthFacing = (existingHouseFacing & Direction.South) != 0;
                        var existingHouseIsEastFacing = (existingHouseFacing & Direction.East) != 0;

                        // Sub-Rule 1: No houses within immediate proximity (1 tile radius)
                        if (absXOffset <= 1 && absYOffset <= 1)
                        {
                            return false;
                        }

                        // Sub-Rule 2: If we're south facing, protect our south yard
                        if (isSouthFacing && inSouthYard)
                        {
                            return false;
                        }

                        // Sub-Rule 3: If we're east facing, protect our east yard
                        if (isEastFacing && inEastYard)
                        {
                            return false;
                        }

                        // Sub-Rule 4: If there's a south-facing house to our north, respect its yard
                        if (inNorthYard && existingHouseIsSouthFacing)
                        {
                            return false;
                        }

                        // Sub-Rule 5: If there's an east-facing house to our west, respect its yard
                        if (inWestYard && existingHouseIsEastFacing)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
