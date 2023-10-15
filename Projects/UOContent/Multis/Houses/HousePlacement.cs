using System.Collections.Generic;
using Server.Regions;
using Server.Spells;

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

        public static HousePlacementResult Check(Mobile from, int multiID, Point3D center, out List<IEntity> toMove)
        {
            // If this spot is considered valid, every item and mobile in this list will be moved under the house sign
            toMove = new List<IEntity>();

            var map = from.Map;

            if (map == null || map == Map.Internal)
            {
                return HousePlacementResult.BadLand; // A house cannot go here
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

            // Location of the nortwest-most corner of the house
            var start = new Point3D(center.X + mcl.Min.X, center.Y + mcl.Min.Y, center.Z);

            // These are storage lists. They hold items and mobiles found in the map for further processing
            var items = new List<Item>();
            var mobiles = new List<Mobile>();

            // These are also storage lists. They hold location values indicating the yard and border locations.
            List<Point2D> yard = new(), borders = new();

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

                        if (reg.IsPartOf<TreasureRegion>() || reg.IsPartOf<HouseRegion>())
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

                    var oldTiles = map.Tiles.GetStaticTiles(tileX, tileY, true);

                    var sector = map.GetSector(tileX, tileY);

                    items.Clear();

                    for (var i = 0; i < sector.Items.Count; ++i)
                    {
                        var item = sector.Items[i];

                        if (item.Visible && item.X == tileX && item.Y == tileY)
                        {
                            items.Add(item);
                        }
                    }

                    mobiles.Clear();

                    for (var i = 0; i < sector.Mobiles.Count; ++i)
                    {
                        var m = sector.Mobiles[i];

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

                        for (var j = 0; j < oldTiles.Length; ++j)
                        {
                            var oldTile = oldTiles[j];
                            var id = TileData.ItemTable[oldTile.ID & TileData.MaxItemValue];

                            if ((id.Impassable || id.Surface && !id.Background) &&
                                addTileTop > oldTile.Z && oldTile.Z + id.CalcHeight > addTileZ)
                            {
                                return HousePlacementResult.BadStatic; // Broke rule #2
                            }

                            /*else if (isFoundation && !hasSurface && (id.Flags & TileFlag.Surface) != 0 && (oldTile.Z + id.CalcHeight) == center.Z)
                                hasSurface = true;*/
                        }

                        for (var j = 0; j < items.Count; ++j)
                        {
                            var item = items[j];
                            var id = item.ItemData;

                            if (addTileTop > item.Z && item.Z + id.CalcHeight > addTileZ)
                            {
                                if (item.Movable)
                                {
                                    toMove.Add(item);
                                }
                                else if (id.Impassable || id.Surface && !id.Background)
                                {
                                    return HousePlacementResult.BadItem; // Broke rule #2
                                }
                            }

                            /*else if (isFoundation && !hasSurface && (id.Flags & TileFlag.Surface) != 0 && (item.Z + id.CalcHeight) == center.Z)
                              {
                                hasSurface = true;
                              }*/
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
                        for (var xOffset = -1; xOffset <= 1; ++xOffset)
                        {
                            for (var yOffset = -YardSize; yOffset <= YardSize; ++yOffset)
                            {
                                var yardPoint = new Point2D(tileX + xOffset, tileY + yOffset);

                                if (!yard.Contains(yardPoint))
                                {
                                    yard.Add(yardPoint);
                                }
                            }
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

                var tiles = map.Tiles.GetStaticTiles(borderPoint.X, borderPoint.Y, true);

                for (var j = 0; j < tiles.Length; ++j)
                {
                    var tile = tiles[j];
                    var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                    if (id.Impassable || id.Surface && !id.Background && tile.Z + id.CalcHeight > center.Z + 2)
                    {
                        return HousePlacementResult.BadStatic; // Broke rule #1
                    }
                }

                var sector = map.GetSector(borderPoint.X, borderPoint.Y);
                var sectorItems = sector.Items;

                for (var j = 0; j < sectorItems.Count; ++j)
                {
                    var item = sectorItems[j];

                    if (item.X != borderPoint.X || item.Y != borderPoint.Y || item.Movable)
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

            var _sectors = new List<Map.Sector>();
            var _houses = new List<BaseHouse>();

            for (var i = 0; i < yard.Count; i++)
            {
                var sector = map.GetSector(yard[i]);

                if (!_sectors.Contains(sector))
                {
                    _sectors.Add(sector);

                    for (var j = 0; j < sector.Multis?.Count; j++)
                    {
                        if (sector.Multis[j] is BaseHouse)
                        {
                            var _house = (BaseHouse)sector.Multis[j];
                            if (!_houses.Contains(_house))
                            {
                                _houses.Add(_house);
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < yard.Count; ++i)
            {
                foreach (var b in _houses)
                {
                    if (b.Contains(yard[i]))
                    {
                        return HousePlacementResult.BadStatic; // Broke rule #3
                    }
                }
            }
            /*Point2D yardPoint = yard[i];

              IPooledEnumerable eable = map.GetMultiTilesAt( yardPoint.X, yardPoint.Y );

              foreach ( StaticTile[] tile in eable )
              {
                for ( int j = 0; j < tile.Length; ++j )
                {
                  if ((TileData.ItemTable[tile[j].ID & TileData.MaxItemValue].Flags & (TileFlag.Impassable | TileFlag.Surface)) != 0)
                  {
                    eable.Free();
                    return HousePlacementResult.BadStatic; // Broke rule #3
                  }
                }
              }

              eable.Free();*/

            return HousePlacementResult.Valid;
        }
    }
}
