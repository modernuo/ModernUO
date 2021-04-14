using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Movement
{
    public class MovementImpl : IMovementImpl
    {
        private const int PersonHeight = 16;
        private const int StepHeight = 2;

        private const TileFlag ImpassableSurface = TileFlag.Impassable | TileFlag.Surface;

        private static Point3D m_Goal;

        private readonly List<Mobile>[] m_MobPools = { new(), new(), new() };

        private readonly List<Item>[] m_Pools = { new(), new(), new(), new() };

        private readonly List<Sector> m_Sectors = new();

        private MovementImpl()
        {
        }

        public static bool AlwaysIgnoreDoors { get; set; }
        public static bool IgnoreMovableImpassables { get; set; }
        public static bool IgnoreSpellFields { get; set; }

        public static Point3D Goal
        {
            get => m_Goal;
            set => m_Goal = value;
        }

        public bool CheckMovement(Mobile m, Map map, Point3D loc, Direction d, out int newZ)
        {
            if (map == null || map == Map.Internal)
            {
                newZ = 0;
                return false;
            }

            var xStart = loc.X;
            var yStart = loc.Y;
            int xForward = xStart, yForward = yStart;
            int xRight = xStart, yRight = yStart;
            int xLeft = xStart, yLeft = yStart;

            var checkDiagonals = ((int)d & 0x1) == 0x1;

            Offset(d, ref xForward, ref yForward);
            Offset((Direction)(((int)d - 1) & 0x7), ref xLeft, ref yLeft);
            Offset((Direction)(((int)d + 1) & 0x7), ref xRight, ref yRight);

            if (xForward < 0 || yForward < 0 || xForward >= map.Width || yForward >= map.Height)
            {
                newZ = 0;
                return false;
            }

            var itemsStart = m_Pools[0];
            var itemsForward = m_Pools[1];
            var itemsLeft = m_Pools[2];
            var itemsRight = m_Pools[3];

            var ignoreMovableImpassables = IgnoreMovableImpassables;
            var reqFlags = ImpassableSurface;

            if (m.CanSwim)
            {
                reqFlags |= TileFlag.Wet;
            }

            var mobsForward = m_MobPools[0];
            var mobsLeft = m_MobPools[1];
            var mobsRight = m_MobPools[2];

            var checkMobs = (m as BaseCreature)?.Controlled == false && (xForward != m_Goal.X || yForward != m_Goal.Y);

            if (checkDiagonals)
            {
                var sectorStart = map.GetSector(xStart, yStart);
                var sectorForward = map.GetSector(xForward, yForward);
                var sectorLeft = map.GetSector(xLeft, yLeft);
                var sectorRight = map.GetSector(xRight, yRight);

                var sectors = m_Sectors;

                sectors.Add(sectorStart);

                if (!sectors.Contains(sectorForward))
                {
                    sectors.Add(sectorForward);
                }

                if (!sectors.Contains(sectorLeft))
                {
                    sectors.Add(sectorLeft);
                }

                if (!sectors.Contains(sectorRight))
                {
                    sectors.Add(sectorRight);
                }

                for (var i = 0; i < sectors.Count; ++i)
                {
                    var sector = sectors[i];

                    for (var j = 0; j < sector.Items.Count; ++j)
                    {
                        var item = sector.Items[j];

                        if (ignoreMovableImpassables && item.Movable &&
                            (item.ItemData.Flags & ImpassableSurface) != 0)
                        {
                            continue;
                        }

                        if ((item.ItemData.Flags & reqFlags) == 0)
                        {
                            continue;
                        }

                        if (item is BaseMulti || item.ItemID > TileData.MaxItemValue)
                        {
                            continue;
                        }

                        if (sector == sectorStart && item.AtWorldPoint(xStart, yStart))
                        {
                            itemsStart.Add(item);
                        }
                        else if (sector == sectorForward && item.AtWorldPoint(xForward, yForward))
                        {
                            itemsForward.Add(item);
                        }
                        else if (sector == sectorLeft && item.AtWorldPoint(xLeft, yLeft))
                        {
                            itemsLeft.Add(item);
                        }
                        else if (sector == sectorRight && item.AtWorldPoint(xRight, yRight))
                        {
                            itemsRight.Add(item);
                        }
                    }

                    if (checkMobs)
                    {
                        for (var j = 0; j < sector.Mobiles.Count; ++j)
                        {
                            var mob = sector.Mobiles[j];

                            if (sector == sectorForward && mob.X == xForward && mob.Y == yForward)
                            {
                                mobsForward.Add(mob);
                            }
                            else if (sector == sectorLeft && mob.X == xLeft && mob.Y == yLeft)
                            {
                                mobsLeft.Add(mob);
                            }
                            else if (sector == sectorRight && mob.X == xRight && mob.Y == yRight)
                            {
                                mobsRight.Add(mob);
                            }
                        }
                    }
                }

                if (m_Sectors.Count > 0)
                {
                    m_Sectors.Clear();
                }
            }
            else
            {
                var sectorStart = map.GetSector(xStart, yStart);
                var sectorForward = map.GetSector(xForward, yForward);

                if (sectorStart == sectorForward)
                {
                    for (var i = 0; i < sectorStart.Items.Count; ++i)
                    {
                        var item = sectorStart.Items[i];

                        if (ignoreMovableImpassables && item.Movable &&
                            (item.ItemData.Flags & ImpassableSurface) != 0)
                        {
                            continue;
                        }

                        if ((item.ItemData.Flags & reqFlags) == 0)
                        {
                            continue;
                        }

                        if (item is BaseMulti || item.ItemID > TileData.MaxItemValue)
                        {
                            continue;
                        }

                        if (item.AtWorldPoint(xStart, yStart))
                        {
                            itemsStart.Add(item);
                        }
                        else if (item.AtWorldPoint(xForward, yForward))
                        {
                            itemsForward.Add(item);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < sectorForward.Items.Count; ++i)
                    {
                        var item = sectorForward.Items[i];

                        if (ignoreMovableImpassables && item.Movable &&
                            (item.ItemData.Flags & ImpassableSurface) != 0)
                        {
                            continue;
                        }

                        if ((item.ItemData.Flags & reqFlags) == 0)
                        {
                            continue;
                        }

                        if (item.AtWorldPoint(xForward, yForward) && !(item is BaseMulti) &&
                            item.ItemID <= TileData.MaxItemValue)
                        {
                            itemsForward.Add(item);
                        }
                    }

                    for (var i = 0; i < sectorStart.Items.Count; ++i)
                    {
                        var item = sectorStart.Items[i];

                        if (ignoreMovableImpassables && item.Movable &&
                            (item.ItemData.Flags & ImpassableSurface) != 0)
                        {
                            continue;
                        }

                        if ((item.ItemData.Flags & reqFlags) == 0)
                        {
                            continue;
                        }

                        if (item.AtWorldPoint(xStart, yStart) && !(item is BaseMulti) &&
                            item.ItemID <= TileData.MaxItemValue)
                        {
                            itemsStart.Add(item);
                        }
                    }
                }

                if (checkMobs)
                {
                    for (var i = 0; i < sectorForward.Mobiles.Count; ++i)
                    {
                        var mob = sectorForward.Mobiles[i];

                        if (mob.X == xForward && mob.Y == yForward)
                        {
                            mobsForward.Add(mob);
                        }
                    }
                }
            }

            GetStartZ(m, map, loc, itemsStart, out var startZ, out var startTop);

            var moveIsOk = Check(
                map,
                m,
                itemsForward,
                mobsForward,
                xForward,
                yForward,
                startTop,
                startZ,
                m.CanSwim,
                m.CantWalk,
                out newZ
            );

            if (moveIsOk && checkDiagonals)
            {
                if (m.Player && m.AccessLevel < AccessLevel.GameMaster)
                {
                    if (!Check(map, m, itemsLeft, mobsLeft, xLeft, yLeft, startTop, startZ, m.CanSwim, m.CantWalk, out _) ||
                        !Check(
                            map,
                            m,
                            itemsRight,
                            mobsRight,
                            xRight,
                            yRight,
                            startTop,
                            startZ,
                            m.CanSwim,
                            m.CantWalk,
                            out _
                        ))
                    {
                        moveIsOk = false;
                    }
                }
                else
                {
                    if (!Check(map, m, itemsLeft, mobsLeft, xLeft, yLeft, startTop, startZ, m.CanSwim, m.CantWalk, out _) &&
                        !Check(
                            map,
                            m,
                            itemsRight,
                            mobsRight,
                            xRight,
                            yRight,
                            startTop,
                            startZ,
                            m.CanSwim,
                            m.CantWalk,
                            out _
                        ))
                    {
                        moveIsOk = false;
                    }
                }
            }

            for (int i = 0, c = checkDiagonals ? 4 : 2; i < c; ++i)
            {
                m_Pools[i].Clear();
            }

            for (int i = 0, c = checkDiagonals ? 3 : 1; i < c; ++i)
            {
                m_MobPools[i].Clear();
            }

            if (!moveIsOk)
            {
                newZ = startZ;
            }

            return moveIsOk;
        }

        public bool CheckMovement(Mobile m, Direction d, out int newZ) => CheckMovement(m, m.Map, m.Location, d, out newZ);

        public static void Configure()
        {
            Movement.Impl = new MovementImpl();
        }

        private bool IsOk(
            bool ignoreDoors, bool ignoreSpellFields, int ourZ, int ourTop, StaticTile[] tiles, List<Item> items
        )
        {
            for (var i = 0; i < tiles.Length; ++i)
            {
                var check = tiles[i];
                var itemData = TileData.ItemTable[check.ID & TileData.MaxItemValue];

                if ((itemData.Flags & ImpassableSurface) != 0) // Impassable || Surface
                {
                    var checkZ = check.Z;
                    var checkTop = checkZ + itemData.CalcHeight;

                    if (checkTop > ourZ && ourTop > checkZ)
                    {
                        return false;
                    }
                }
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var itemID = item.ItemID & TileData.MaxItemValue;
                var itemData = TileData.ItemTable[itemID];
                var flags = itemData.Flags;

                if ((flags & ImpassableSurface) != 0) // Impassable || Surface
                {
                    if (ignoreDoors && ((flags & TileFlag.Door) != 0 || itemID == 0x692 || itemID == 0x846 ||
                                        itemID == 0x873 ||
                                        itemID >= 0x6F5 && itemID <= 0x6F6))
                    {
                        continue;
                    }

                    if (ignoreSpellFields && (itemID == 0x82 || itemID == 0x3946 || itemID == 0x3956))
                    {
                        continue;
                    }

                    var checkZ = item.Z;
                    var checkTop = checkZ + itemData.CalcHeight;

                    if (checkTop > ourZ && ourTop > checkZ)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool Check(
            Map map, Mobile m, List<Item> items, List<Mobile> mobiles, int x, int y, int startTop, int startZ,
            bool canSwim, bool cantWalk, out int newZ
        )
        {
            newZ = 0;

            var tiles = map.Tiles.GetStaticTiles(x, y, true);
            var landTile = map.Tiles.GetLandTile(x, y);
            var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
            var impassable = (flags & TileFlag.Impassable) != 0;

            // Impassable + swim on water is ok, otherwise block if cannot walk or impassable
            var landBlocks = (cantWalk || impassable) && !(impassable && canSwim && (flags & TileFlag.Wet) != 0);

            var considerLand = !landTile.Ignored;

            int landZ = 0, landCenter = 0, landTop = 0;

            map.GetAverageZ(x, y, ref landZ, ref landCenter, ref landTop);

            var moveIsOk = false;

            var stepTop = startTop + StepHeight;
            var checkTop = startZ + PersonHeight;

            var ignoreDoors = AlwaysIgnoreDoors || !m.Alive || m.Body.BodyID == 0x3DB || m.IsDeadBondedPet;
            var ignoreSpellFields = m is PlayerMobile && map != Map.Felucca;

            for (var i = 0; i < tiles.Length; ++i)
            {
                var tile = tiles[i];
                var itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                flags = itemData.Flags;

                var notWater = (flags & TileFlag.Wet) == 0;

                // Surface && !Impassable
                if ((flags & ImpassableSurface) != TileFlag.Surface && (!canSwim || notWater) ||
                    cantWalk && notWater)
                {
                    continue;
                }

                var itemZ = tile.Z;
                var itemTop = itemZ;
                var ourZ = itemZ + itemData.CalcHeight;
                // int ourTop = ourZ + PersonHeight;
                var testTop = checkTop;

                if (moveIsOk)
                {
                    var cmp = (ourZ - m.Z).Abs() - (newZ - m.Z).Abs();

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

                if (stepTop >= itemTop)
                {
                    var landCheck = itemZ;

                    if (itemData.Height >= StepHeight)
                    {
                        landCheck += StepHeight;
                    }
                    else
                    {
                        landCheck += itemData.Height;
                    }

                    if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
                    {
                        continue;
                    }

                    if (IsOk(ignoreDoors, ignoreSpellFields, ourZ, testTop, tiles, items))
                    {
                        newZ = ourZ;
                        moveIsOk = true;
                    }
                }
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                var itemData = item.ItemData;
                flags = itemData.Flags;

                var notWater = (flags & TileFlag.Wet) == 0;

                // Surface && !Impassable && !Movable
                if (item.Movable ||
                    (flags & ImpassableSurface) != TileFlag.Surface && (!m.CanSwim || notWater) ||
                    cantWalk && notWater)
                {
                    continue;
                }

                var itemZ = item.Z;
                var itemTop = itemZ;
                var ourZ = itemZ + itemData.CalcHeight;
                // int ourTop = ourZ + PersonHeight;
                var testTop = checkTop;

                if (moveIsOk)
                {
                    var cmp = (ourZ - m.Z).Abs() - (newZ - m.Z).Abs();

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

                if (stepTop >= itemTop)
                {
                    var landCheck = itemZ;

                    if (itemData.Height >= StepHeight)
                    {
                        landCheck += StepHeight;
                    }
                    else
                    {
                        landCheck += itemData.Height;
                    }

                    if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
                    {
                        continue;
                    }

                    if (IsOk(ignoreDoors, ignoreSpellFields, ourZ, testTop, tiles, items))
                    {
                        newZ = ourZ;
                        moveIsOk = true;
                    }
                }
            }

            if (considerLand && !landBlocks && stepTop >= landZ)
            {
                var ourZ = landCenter;
                // int ourTop = ourZ + PersonHeight;
                var testTop = checkTop;

                if (ourZ + PersonHeight > testTop)
                {
                    testTop = ourZ + PersonHeight;
                }

                var shouldCheck = true;

                if (moveIsOk)
                {
                    var cmp = (ourZ - m.Z).Abs() - (newZ - m.Z).Abs();

                    if (cmp > 0 || cmp == 0 && ourZ > newZ)
                    {
                        shouldCheck = false;
                    }
                }

                if (shouldCheck && IsOk(ignoreDoors, ignoreSpellFields, ourZ, testTop, tiles, items))
                {
                    newZ = ourZ;
                    moveIsOk = true;
                }
            }

            if (moveIsOk)
            {
                for (var i = 0; moveIsOk && i < mobiles.Count; ++i)
                {
                    var mob = mobiles[i];

                    if (mob != m && mob.Z + 15 > newZ && newZ + 15 > mob.Z && !CanMoveOver(m, mob))
                    {
                        moveIsOk = false;
                    }
                }
            }

            return moveIsOk;
        }

        private bool CanMoveOver(Mobile m, Mobile t) =>
            !t.Alive || !m.Alive || t.IsDeadBondedPet || m.IsDeadBondedPet || t.Hidden && t.AccessLevel > AccessLevel.Player;

        private void GetStartZ(Mobile m, Map map, Point3D loc, List<Item> itemList, out int zLow, out int zTop)
        {
            int xCheck = loc.X, yCheck = loc.Y;

            var landTile = map.Tiles.GetLandTile(xCheck, yCheck);
            int landZ = 0, landCenter = 0, landTop = 0;
            var flags = TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags;
            var impassable = (flags & TileFlag.Impassable) != 0;

            // Impassable + swim on water is ok, otherwise block if cannot walk or impassable
            var landBlocks = (m.CantWalk || impassable) && !(impassable && m.CanSwim && (flags & TileFlag.Wet) != 0);

            map.GetAverageZ(xCheck, yCheck, ref landZ, ref landCenter, ref landTop);

            var considerLand = !landTile.Ignored;

            var zCenter = zLow = zTop = 0;
            var isSet = false;

            if (considerLand && !landBlocks && loc.Z >= landCenter)
            {
                zLow = landZ;
                zCenter = landCenter;

                zTop = landTop;

                isSet = true;
            }

            var staticTiles = map.Tiles.GetStaticTiles(xCheck, yCheck, true);

            for (var i = 0; i < staticTiles.Length; ++i)
            {
                var tile = staticTiles[i];
                var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                var calcTop = tile.Z + id.CalcHeight;

                if ((!isSet || calcTop >= zCenter) &&
                    ((id.Flags & TileFlag.Surface) != 0 || m.CanSwim && (id.Flags & TileFlag.Wet) != 0) && loc.Z >= calcTop)
                {
                    if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
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
            }

            for (var i = 0; i < itemList.Count; ++i)
            {
                var item = itemList[i];

                var id = item.ItemData;

                var calcTop = item.Z + id.CalcHeight;

                if ((!isSet || calcTop >= zCenter) &&
                    ((id.Flags & TileFlag.Surface) != 0 || m.CanSwim && (id.Flags & TileFlag.Wet) != 0) && loc.Z >= calcTop)
                {
                    if (m.CantWalk && (id.Flags & TileFlag.Wet) == 0)
                    {
                        continue;
                    }

                    zLow = item.Z;
                    zCenter = calcTop;

                    var top = item.Z + id.Height;

                    if (!isSet || top > zTop)
                    {
                        zTop = top;
                    }

                    isSet = true;
                }
            }

            if (!isSet)
            {
                zLow = zTop = loc.Z;
            }
            else if (loc.Z > zTop)
            {
                zTop = loc.Z;
            }
        }

        public void Offset(Direction d, ref int x, ref int y)
        {
            switch (d & Direction.Mask)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }
        }
    }
}
