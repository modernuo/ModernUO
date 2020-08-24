using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Movement
{
    public class FastMovementImpl : IMovementImpl
    {
        private const int PersonHeight = 16;
        private const int StepHeight = 2;

        private const TileFlag ImpassableSurface = TileFlag.Impassable | TileFlag.Surface;
        public static bool Enabled = false;

        private static IMovementImpl _Successor;

        private FastMovementImpl()
        {
        }

        public bool CheckMovement(Mobile m, Map map, Point3D loc, Direction d, out int newZ)
        {
            if (!Enabled && _Successor != null) return _Successor.CheckMovement(m, map, loc, d, out newZ);

            if (map == null || map == Map.Internal)
            {
                newZ = 0;
                return false;
            }

            int xStart = loc.X;
            int yStart = loc.Y;

            int xForward = xStart, yForward = yStart;
            int xRight = xStart, yRight = yStart;
            int xLeft = xStart, yLeft = yStart;

            bool checkDiagonals = ((int)d & 0x1) == 0x1;

            Offset(d, ref xForward, ref yForward);
            Offset((Direction)((int)d - 1 & 0x7), ref xLeft, ref yLeft);
            Offset((Direction)((int)d + 1 & 0x7), ref xRight, ref yRight);

            if (xForward < 0 || yForward < 0 || xForward >= map.Width || yForward >= map.Height)
            {
                newZ = 0;
                return false;
            }

            IEnumerable<Item> itemsStart, itemsForward, itemsLeft, itemsRight;

            bool ignoreMovableImpassables = MovementImpl.IgnoreMovableImpassables;
            TileFlag reqFlags = ImpassableSurface;

            if (m.CanSwim) reqFlags |= TileFlag.Wet;

            if (checkDiagonals)
            {
                Sector sStart = map.GetSector(xStart, yStart);
                Sector sForward = map.GetSector(xForward, yForward);
                Sector sLeft = map.GetSector(xLeft, yLeft);
                Sector sRight = map.GetSector(xRight, yRight);

                itemsStart = sStart.Items.Where(i => Verify(i, reqFlags, ignoreMovableImpassables, xStart, yStart));
                itemsForward = sForward.Items.Where(i => Verify(i, reqFlags, ignoreMovableImpassables, xForward, yForward));
                itemsLeft = sLeft.Items.Where(i => Verify(i, reqFlags, ignoreMovableImpassables, xLeft, yLeft));
                itemsRight = sRight.Items.Where(i => Verify(i, reqFlags, ignoreMovableImpassables, xRight, yRight));
            }
            else
            {
                Sector sStart = map.GetSector(xStart, yStart);
                Sector sForward = map.GetSector(xForward, yForward);

                itemsStart = sStart.Items.Where(i => Verify(i, reqFlags, ignoreMovableImpassables, xStart, yStart));
                itemsForward = sForward.Items.Where(i => Verify(i, reqFlags, ignoreMovableImpassables, xForward, yForward));
                itemsLeft = Enumerable.Empty<Item>();
                itemsRight = Enumerable.Empty<Item>();
            }

            GetStartZ(m, map, loc, itemsStart, out int startZ, out int startTop);

            List<Item> list = null;

            MovementPool.AcquireMoveCache(ref list, itemsForward);

            bool moveIsOk = Check(map, m, list, xForward, yForward, startTop, startZ, m.CanSwim, m.CantWalk, out newZ);

            if (moveIsOk && checkDiagonals)
            {
                if (m.Player && m.AccessLevel < AccessLevel.GameMaster)
                {
                    MovementPool.AcquireMoveCache(ref list, itemsLeft);

                    if (!Check(map, m, list, xLeft, yLeft, startTop, startZ, m.CanSwim, m.CantWalk, out _))
                    {
                        moveIsOk = false;
                    }
                    else
                    {
                        MovementPool.AcquireMoveCache(ref list, itemsRight);

                        if (!Check(map, m, list, xRight, yRight, startTop, startZ, m.CanSwim, m.CantWalk, out _))
                            moveIsOk = false;
                    }
                }
                else
                {
                    MovementPool.AcquireMoveCache(ref list, itemsLeft);

                    if (!Check(map, m, list, xLeft, yLeft, startTop, startZ, m.CanSwim, m.CantWalk, out _))
                    {
                        MovementPool.AcquireMoveCache(ref list, itemsRight);

                        if (!Check(map, m, list, xRight, yRight, startTop, startZ, m.CanSwim, m.CantWalk, out _))
                            moveIsOk = false;
                    }
                }
            }

            MovementPool.ClearMoveCache(ref list, true);

            if (!moveIsOk) newZ = startZ;

            return moveIsOk;
        }

        public bool CheckMovement(Mobile m, Direction d, out int newZ) =>
            !Enabled && _Successor != null
                ? _Successor.CheckMovement(m, d, out newZ)
                : CheckMovement(m, m.Map, m.Location, d, out newZ);

        public static void Initialize()
        {
            _Successor = Movement.Impl;
            Movement.Impl = new FastMovementImpl();
        }

        private static bool IsOk(StaticTile tile, int ourZ, int ourTop)
        {
            ItemData itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

            return tile.Z + itemData.CalcHeight <= ourZ || ourTop <= tile.Z || (itemData.Flags & ImpassableSurface) == 0;
        }

        private static bool IsOk(Item item, int ourZ, int ourTop, bool ignoreDoors, bool ignoreSpellFields)
        {
            int itemID = item.ItemID & TileData.MaxItemValue;
            ItemData itemData = TileData.ItemTable[itemID];

            if ((itemData.Flags & ImpassableSurface) == 0) return true;

            if (((itemData.Flags & TileFlag.Door) != 0 || itemID == 0x692 || itemID == 0x846 || itemID == 0x873 ||
                 (itemID >= 0x6F5 && itemID <= 0x6F6)) && ignoreDoors)
                return true;

            if ((itemID == 0x82 || itemID == 0x3946 || itemID == 0x3956) && ignoreSpellFields) return true;

            return item.Z + itemData.CalcHeight <= ourZ || ourTop <= item.Z;
        }

        private static bool IsOk(
            bool ignoreDoors,
            bool ignoreSpellFields,
            int ourZ,
            int ourTop,
            IEnumerable<StaticTile> tiles,
            IEnumerable<Item> items)
        {
            return tiles.All(t => IsOk(t, ourZ, ourTop)) &&
                   items.All(i => IsOk(i, ourZ, ourTop, ignoreDoors, ignoreSpellFields));
        }

        private static bool Check(
            Map map,
            Mobile m,
            List<Item> items,
            int x,
            int y,
            int startTop,
            int startZ,
            bool canSwim,
            bool cantWalk,
            out int newZ)
        {
            newZ = 0;

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);
            LandTile landTile = map.Tiles.GetLandTile(x, y);
            LandData landData = TileData.LandTable[landTile.ID & TileData.MaxLandValue];
            bool landBlocks = (landData.Flags & TileFlag.Impassable) != 0;
            bool considerLand = !landTile.Ignored;

            if (landBlocks && canSwim && (landData.Flags & TileFlag.Wet) != 0)
                landBlocks = false;
            else if (cantWalk && (landData.Flags & TileFlag.Wet) == 0) landBlocks = true;

            int landZ = 0, landCenter = 0, landTop = 0;

            map.GetAverageZ(x, y, ref landZ, ref landCenter, ref landTop);

            bool moveIsOk = false;

            int stepTop = startTop + StepHeight;
            int checkTop = startZ + PersonHeight;

            bool ignoreDoors = MovementImpl.AlwaysIgnoreDoors || !m.Alive || m.IsDeadBondedPet || m.Body.IsGhost ||
                               m.Body.BodyID == 987;
            bool ignoreSpellFields = m is PlayerMobile && map.MapID != 0;

            int itemZ, itemTop, ourZ, ourTop, testTop;
            ItemData itemData;
            TileFlag flags;

            foreach (StaticTile tile in tiles)
            {
                itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                if (m.Flying && Insensitive.Equals(itemData.Name, "hover over"))
                {
                    newZ = tile.Z;
                    return true;
                }

                // Stygian Dragon
                if (m.Body == 826 && map?.MapID == 5)
                {
                    if (x >= 307 && x <= 354 && y >= 126 && y <= 192)
                    {
                        if (tile.Z > newZ) newZ = tile.Z;

                        moveIsOk = true;
                    }
                    else if (x >= 42 && x <= 89)
                    {
                        if ((y >= 333 && y <= 399) || (y >= 531 && y <= 597) || (y >= 739 && y <= 805))
                        {
                            if (tile.Z > newZ) newZ = tile.Z;

                            moveIsOk = true;
                        }
                    }
                }

                flags = itemData.Flags;

                if ((flags & ImpassableSurface) != TileFlag.Surface && (!canSwim || (flags & TileFlag.Wet) == 0)) continue;

                if (cantWalk && (flags & TileFlag.Wet) == 0) continue;

                itemZ = tile.Z;
                itemTop = itemZ;
                ourZ = itemZ + itemData.CalcHeight;
                ourTop = ourZ + PersonHeight;
                testTop = checkTop;

                if (moveIsOk)
                {
                    int cmp = Math.Abs(ourZ - m.Z) - Math.Abs(newZ - m.Z);

                    if (cmp > 0 || (cmp == 0 && ourZ > newZ)) continue;
                }

                if (ourTop > testTop) testTop = ourTop;

                if (!itemData.Bridge) itemTop += itemData.Height;

                if (stepTop < itemTop) continue;

                int landCheck = itemZ;

                if (itemData.Height >= StepHeight)
                    landCheck += StepHeight;
                else
                    landCheck += itemData.Height;

                if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ) continue;

                if (!IsOk(ignoreDoors, ignoreSpellFields, ourZ, testTop, tiles, items)) continue;

                newZ = ourZ;
                moveIsOk = true;
            }

            foreach (Item item in items)
            {
                itemData = item.ItemData;
                flags = itemData.Flags;

                if (m.Flying && Insensitive.Equals(itemData.Name, "hover over"))
                {
                    newZ = item.Z;
                    return true;
                }

                if (item.Movable) continue;

                if ((flags & ImpassableSurface) != TileFlag.Surface && (!m.CanSwim || (flags & TileFlag.Wet) == 0)) continue;

                if (cantWalk && (flags & TileFlag.Wet) == 0) continue;

                itemZ = item.Z;
                itemTop = itemZ;
                ourZ = itemZ + itemData.CalcHeight;
                ourTop = ourZ + PersonHeight;
                testTop = checkTop;

                if (moveIsOk)
                {
                    int cmp = Math.Abs(ourZ - m.Z) - Math.Abs(newZ - m.Z);

                    if (cmp > 0 || (cmp == 0 && ourZ > newZ)) continue;
                }

                if (ourTop > testTop) testTop = ourTop;

                if (!itemData.Bridge) itemTop += itemData.Height;

                if (stepTop < itemTop) continue;

                int landCheck = itemZ;

                if (itemData.Height >= StepHeight)
                    landCheck += StepHeight;
                else
                    landCheck += itemData.Height;

                if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ) continue;

                if (!IsOk(ignoreDoors, ignoreSpellFields, ourZ, testTop, tiles, items)) continue;

                newZ = ourZ;
                moveIsOk = true;
            }

            if (!considerLand || landBlocks || stepTop < landZ) return moveIsOk;

            ourZ = landCenter;
            ourTop = ourZ + PersonHeight;
            testTop = checkTop;

            if (ourTop > testTop) testTop = ourTop;

            bool shouldCheck = true;

            if (moveIsOk)
            {
                int cmp = Math.Abs(ourZ - m.Z) - Math.Abs(newZ - m.Z);

                if (cmp > 0 || (cmp == 0 && ourZ > newZ)) shouldCheck = false;
            }

            if (!shouldCheck || !IsOk(ignoreDoors, ignoreSpellFields, ourZ, testTop, tiles, items)) return moveIsOk;

            newZ = ourZ;
            moveIsOk = true;

            return moveIsOk;
        }

        private static bool Verify(Item item, int x, int y) => item.AtWorldPoint(x, y);

        private static bool Verify(Item item, TileFlag reqFlags, bool ignoreMovableImpassables) =>
            item != null && (!ignoreMovableImpassables || !item.Movable || !item.ItemData.Impassable) &&
            (item.ItemData.Flags & reqFlags) != 0 && !(item is BaseMulti) && item.ItemID <= TileData.MaxItemValue;

        private static bool Verify(Item item, TileFlag reqFlags, bool ignoreMovableImpassables, int x, int y) =>
            Verify(item, reqFlags, ignoreMovableImpassables) && Verify(item, x, y);

        private static void GetStartZ(Mobile m, Map map, Point3D loc, IEnumerable<Item> itemList, out int zLow, out int zTop)
        {
            int xCheck = loc.X, yCheck = loc.Y;

            LandTile landTile = map.Tiles.GetLandTile(xCheck, yCheck);
            LandData landData = TileData.LandTable[landTile.ID & TileData.MaxLandValue];
            bool landBlocks = (landData.Flags & TileFlag.Impassable) != 0;

            if (landBlocks && m.CanSwim && (landData.Flags & TileFlag.Wet) != 0)
                landBlocks = false;
            else if (m.CantWalk && (landData.Flags & TileFlag.Wet) == 0) landBlocks = true;

            int landZ = 0, landCenter = 0, landTop = 0;

            map.GetAverageZ(xCheck, yCheck, ref landZ, ref landCenter, ref landTop);

            bool considerLand = !landTile.Ignored;

            int zCenter = zLow = zTop = 0;
            bool isSet = false;

            if (considerLand && !landBlocks && loc.Z >= landCenter)
            {
                zLow = landZ;
                zCenter = landCenter;
                zTop = landTop;
                isSet = true;
            }

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(xCheck, yCheck, true);

            foreach (StaticTile tile in staticTiles)
            {
                ItemData tileData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                int calcTop = tile.Z + tileData.CalcHeight;

                if (isSet && calcTop < zCenter) continue;

                if ((tileData.Flags & TileFlag.Surface) == 0 &&
                    (!m.CanSwim || (tileData.Flags & TileFlag.Wet) == 0)) continue;

                if (loc.Z < calcTop) continue;

                if (m.CantWalk && (tileData.Flags & TileFlag.Wet) == 0) continue;

                zLow = tile.Z;
                zCenter = calcTop;

                int top = tile.Z + tileData.Height;

                if (!isSet || top > zTop) zTop = top;

                isSet = true;
            }

            foreach (Item item in itemList)
            {
                ItemData itemData = item.ItemData;

                int calcTop = item.Z + itemData.CalcHeight;

                if (isSet && calcTop < zCenter) continue;

                if ((itemData.Flags & TileFlag.Surface) == 0 &&
                    (!m.CanSwim || (itemData.Flags & TileFlag.Wet) == 0)) continue;

                if (loc.Z < calcTop) continue;

                if (m.CantWalk && (itemData.Flags & TileFlag.Wet) == 0) continue;

                zLow = item.Z;
                zCenter = calcTop;

                int top = item.Z + itemData.Height;

                if (!isSet || top > zTop) zTop = top;

                isSet = true;
            }

            if (!isSet)
                zLow = zTop = loc.Z;
            else if (loc.Z > zTop)
                zTop = loc.Z;
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

        private static class MovementPool
        {
            private static readonly object _MovePoolLock = new object();
            private static readonly Queue<List<Item>> _MoveCachePool = new Queue<List<Item>>(0x400);

            public static void AcquireMoveCache(ref List<Item> cache, IEnumerable<Item> items)
            {
                if (cache == null)
                    lock (_MovePoolLock)
                    {
                        cache = _MoveCachePool.Count > 0 ? _MoveCachePool.Dequeue() : new List<Item>(0x10);
                    }
                else
                    cache.Clear();

                cache.AddRange(items);
            }

            public static void ClearMoveCache(ref List<Item> cache, bool free)
            {
                cache?.Clear();

                if (!free) return;

                lock (_MovePoolLock)
                {
                    if (_MoveCachePool.Count < 0x400) _MoveCachePool.Enqueue(cache);
                }

                cache = null;
            }
        }
    }
}
