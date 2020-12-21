using System;
using System.Collections.Generic;
using System.IO;
using Server.Commands;
using Server.Gumps;
using Server.Items;

namespace Server
{
    public static class Statics
    {
        public delegate void FreezeCallback(Mobile from, bool okay, StateInfo si);

        private const string BaseFreezeWarning = "{0}  " +
                                                 "Those items <u>will be removed from the world</u> and placed into the server data files.  " +
                                                 "Other players <u>will not see the changes</u> unless you distribute your data files to them.<br><br>" +
                                                 "This operation may not complete unless the server and client are using different data files.  " +
                                                 "If you receive a message stating 'output data files could not be opened,' then you are probably sharing data files.  " +
                                                 "Create a new directory for the world data files (statics*.mul and staidx*.mul) and add that to Scritps/Misc/DataPath.cs.<br><br>" +
                                                 "The change will be in effect immediately on the server, however, you must restart your client and update it's data files for the changes to become visible.  " +
                                                 "It is strongly recommended that you make backup of the data files mentioned above.  " +
                                                 "Do you wish to proceed?";

        private const string BaseUnfreezeWarning = "{0}  " +
                                                   "Those items <u>will be removed from the static files</u> and exchanged with unmovable dynamic items.  " +
                                                   "Other players <u>will not see the changes</u> unless you distribute your data files to them.<br><br>" +
                                                   "This operation may not complete unless the server and client are using different data files.  " +
                                                   "If you receive a message stating 'output data files could not be opened,' then you are probably sharing data files.  " +
                                                   "Create a new directory for the world data files (statics*.mul and staidx*.mul) and add that to Scritps/Misc/DataPath.cs.<br><br>" +
                                                   "The change will be in effect immediately on the server, however, you must restart your client and update it's data files for the changes to become visible.  " +
                                                   "It is strongly recommended that you make backup of the data files mentioned above.  " +
                                                   "Do you wish to proceed?";

        private static readonly Point3D NullP3D = new(int.MinValue, int.MinValue, int.MinValue);

        private static byte[] m_Buffer;

        private static StaticTile[] m_TileBuffer = new StaticTile[128];

        public static void Initialize()
        {
            CommandSystem.Register("Freeze", AccessLevel.Administrator, Freeze_OnCommand);
            CommandSystem.Register("FreezeMap", AccessLevel.Administrator, FreezeMap_OnCommand);
            CommandSystem.Register("FreezeWorld", AccessLevel.Administrator, FreezeWorld_OnCommand);

            CommandSystem.Register("Unfreeze", AccessLevel.Administrator, Unfreeze_OnCommand);
            CommandSystem.Register("UnfreezeMap", AccessLevel.Administrator, UnfreezeMap_OnCommand);
            CommandSystem.Register("UnfreezeWorld", AccessLevel.Administrator, UnfreezeWorld_OnCommand);
        }

        [Usage("Freeze"), Description("Makes a targeted area of dynamic items static.")]
        public static void Freeze_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            BoundingBoxPicker.Begin(from, (map, start, end) => FreezeBox_Callback(from, map, start, end));
        }

        [Usage("FreezeMap"), Description("Makes every dynamic item in your map static.")]
        public static void FreezeMap_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            var map = from.Map;

            if (map != null && map != Map.Internal)
            {
                SendWarning(
                    from,
                    "You are about to freeze <u>all items in {0}</u>.",
                    BaseFreezeWarning,
                    map,
                    NullP3D,
                    NullP3D,
                    FreezeWarning_Callback
                );
            }
        }

        [Usage("FreezeWorld"), Description("Makes every dynamic item on all maps static.")]
        public static void FreezeWorld_OnCommand(CommandEventArgs e)
        {
            SendWarning(
                e.Mobile,
                "You are about to freeze <u>every item on every map</u>.",
                BaseFreezeWarning,
                null,
                NullP3D,
                NullP3D,
                FreezeWarning_Callback
            );
        }

        public static void SendWarning(
            Mobile m, string header, string baseWarning, Map map, Point3D start, Point3D end,
            FreezeCallback callback
        )
        {
            m.SendGump(
                new WarningGump(
                    1060635,
                    30720,
                    string.Format(baseWarning, string.Format(header, map)),
                    0xFFC000,
                    420,
                    400,
                    okay => callback(m, okay, new StateInfo(map, start, end))
                )
            );
        }

        private static void FreezeBox_Callback(Mobile from, Map map, Point3D start, Point3D end)
        {
            SendWarning(
                from,
                "You are about to freeze a section of items.",
                BaseFreezeWarning,
                map,
                start,
                end,
                FreezeWarning_Callback
            );
        }

        private static void FreezeWarning_Callback(Mobile from, bool okay, StateInfo si)
        {
            if (!okay)
            {
                return;
            }

            Freeze(from, si.m_Map, si.m_Start, si.m_End);
        }

        public static void Freeze(Mobile from, Map targetMap, Point3D start3d, Point3D end3d)
        {
            var mapTable = new Dictionary<Map, Dictionary<Point2D, DeltaState>>();

            if (start3d == NullP3D && end3d == NullP3D)
            {
                if (targetMap == null)
                {
                    CommandLogging.WriteLine(
                        from,
                        "{0} {1} invoking freeze for every item in every map",
                        from.AccessLevel,
                        CommandLogging.Format(from)
                    );
                }
                else
                {
                    CommandLogging.WriteLine(
                        from,
                        "{0} {1} invoking freeze for every item in {0}",
                        from.AccessLevel,
                        CommandLogging.Format(from),
                        targetMap
                    );
                }

                foreach (var item in World.Items.Values)
                {
                    if (targetMap != null && item.Map != targetMap)
                    {
                        continue;
                    }

                    if (item.Parent != null)
                    {
                        continue;
                    }

                    if (item is Static || item is BaseFloor || item is BaseWall)
                    {
                        var itemMap = item.Map;

                        if (itemMap == null || itemMap == Map.Internal)
                        {
                            continue;
                        }

                        if (!mapTable.TryGetValue(itemMap, out var table))
                        {
                            mapTable[itemMap] = table = new Dictionary<Point2D, DeltaState>();
                        }

                        var p = new Point2D(item.X >> 3, item.Y >> 3);

                        if (!table.TryGetValue(p, out var state))
                        {
                            table[p] = state = new DeltaState(p);
                        }

                        state.m_List.Add(item);
                    }
                }
            }
            else if (targetMap != null)
            {
                Point2D start = targetMap.Bound(new Point2D(start3d)), end = targetMap.Bound(new Point2D(end3d));

                CommandLogging.WriteLine(
                    from,
                    "{0} {1} invoking freeze from {2} to {3} in {4}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    start,
                    end,
                    targetMap
                );

                var eable =
                    targetMap.GetItemsInBounds(new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1));

                foreach (var item in eable)
                {
                    if (item is Static || item is BaseFloor || item is BaseWall)
                    {
                        var itemMap = item.Map;

                        if (itemMap == null || itemMap == Map.Internal)
                        {
                            continue;
                        }

                        if (!mapTable.TryGetValue(itemMap, out var table))
                        {
                            mapTable[itemMap] = table = new Dictionary<Point2D, DeltaState>();
                        }

                        var p = new Point2D(item.X >> 3, item.Y >> 3);

                        if (!table.TryGetValue(p, out var state))
                        {
                            table[p] = state = new DeltaState(p);
                        }

                        state.m_List.Add(item);
                    }
                }

                eable.Free();
            }

            if (mapTable.Count == 0)
            {
                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        "No freezable items were found.  Only the following item types are frozen:<br> - Static<br> - BaseFloor<br> - BaseWall",
                        0xFFC000,
                        320,
                        240
                    )
                );
                return;
            }

            var badDataFile = false;

            var totalFrozen = 0;

            foreach (var de in mapTable)
            {
                var map = de.Key;
                var table = de.Value;

                var matrix = map.Tiles;

                using var idxStream = OpenWrite(matrix.IndexStream);
                using var mulStream = OpenWrite(matrix.DataStream);
                if (idxStream == null || mulStream == null)
                {
                    badDataFile = true;
                    continue;
                }

                var idxReader = new BinaryReader(idxStream);

                var idxWriter = new BinaryWriter(idxStream);
                var mulWriter = new BinaryWriter(mulStream);

                foreach (var state in table.Values)
                {
                    var oldTiles = ReadStaticBlock(
                        idxReader,
                        mulStream,
                        state.m_X,
                        state.m_Y,
                        matrix.BlockWidth,
                        matrix.BlockHeight,
                        out var oldTileCount
                    );

                    if (oldTileCount < 0)
                    {
                        continue;
                    }

                    var newTileCount = 0;
                    var newTiles = new StaticTile[state.m_List.Count];

                    for (var i = 0; i < state.m_List.Count; ++i)
                    {
                        var item = state.m_List[i];

                        var xOffset = item.X - state.m_X * 8;
                        var yOffset = item.Y - state.m_Y * 8;

                        if (xOffset < 0 || xOffset >= 8 || yOffset < 0 || yOffset >= 8)
                        {
                            continue;
                        }

                        var newTile = new StaticTile(
                            (ushort)item.ItemID,
                            (byte)xOffset,
                            (byte)yOffset,
                            (sbyte)item.Z,
                            (short)item.Hue
                        );

                        newTiles[newTileCount++] = newTile;

                        item.Delete();

                        ++totalFrozen;
                    }

                    var mulPos = -1;
                    var length = -1;
                    var extra = 0;

                    if (oldTileCount + newTileCount > 0)
                    {
                        mulWriter.Seek(0, SeekOrigin.End);

                        mulPos = (int)mulWriter.BaseStream.Position;
                        length = (oldTileCount + newTileCount) * 7;
                        extra = 1;

                        for (var i = 0; i < oldTileCount; ++i)
                        {
                            var toWrite = oldTiles[i];

                            mulWriter.Write((ushort)toWrite.ID);
                            mulWriter.Write((byte)toWrite.X);
                            mulWriter.Write((byte)toWrite.Y);
                            mulWriter.Write((sbyte)toWrite.Z);
                            mulWriter.Write((short)toWrite.Hue);
                        }

                        for (var i = 0; i < newTileCount; ++i)
                        {
                            var toWrite = newTiles[i];

                            mulWriter.Write((ushort)toWrite.ID);
                            mulWriter.Write((byte)toWrite.X);
                            mulWriter.Write((byte)toWrite.Y);
                            mulWriter.Write((sbyte)toWrite.Z);
                            mulWriter.Write((short)toWrite.Hue);
                        }

                        mulWriter.Flush();
                    }

                    var idxPos = (state.m_X * matrix.BlockHeight + state.m_Y) * 12;

                    idxWriter.Seek(idxPos, SeekOrigin.Begin);
                    idxWriter.Write(mulPos);
                    idxWriter.Write(length);
                    idxWriter.Write(extra);

                    idxWriter.Flush();

                    matrix.SetStaticBlock(state.m_X, state.m_Y, null);
                }
            }

            if (totalFrozen == 0 && badDataFile)
            {
                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        "Output data files could not be opened and the freeze operation has been aborted.<br><br>This probably means your server and client are using the same data files.  Instructions on how to resolve this can be found in the first warning window.",
                        0xFFC000,
                        320,
                        240
                    )
                );
            }
            else
            {
                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        $"Freeze operation completed successfully.<br><br>{totalFrozen} item{(totalFrozen != 1 ? "s were" : " was")} frozen.<br><br>You must restart your client and update it's data files to see the changes.",
                        0xFFC000,
                        320,
                        240
                    )
                );
            }
        }

        [Usage("Unfreeze"), Description("Makes a targeted area of static items dynamic.")]
        public static void Unfreeze_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            BoundingBoxPicker.Begin(from, (map, start, end) => UnfreezeBox_Callback(from, map, start, end));
        }

        [Usage("UnfreezeMap"), Description("Makes every static item in your map dynamic.")]
        public static void UnfreezeMap_OnCommand(CommandEventArgs e)
        {
            var map = e.Mobile.Map;

            if (map != null && map != Map.Internal)
            {
                SendWarning(
                    e.Mobile,
                    "You are about to unfreeze <u>all items in {0}</u>.",
                    BaseUnfreezeWarning,
                    map,
                    NullP3D,
                    NullP3D,
                    UnfreezeWarning_Callback
                );
            }
        }

        [Usage("UnfreezeWorld"), Description("Makes every static item on all maps dynamic.")]
        public static void UnfreezeWorld_OnCommand(CommandEventArgs e)
        {
            SendWarning(
                e.Mobile,
                "You are about to unfreeze <u>every item on every map</u>.",
                BaseUnfreezeWarning,
                null,
                NullP3D,
                NullP3D,
                UnfreezeWarning_Callback
            );
        }

        private static void UnfreezeBox_Callback(Mobile from, Map map, Point3D start, Point3D end)
        {
            SendWarning(
                from,
                "You are about to unfreeze a section of items.",
                BaseUnfreezeWarning,
                map,
                start,
                end,
                UnfreezeWarning_Callback
            );
        }

        private static void UnfreezeWarning_Callback(Mobile from, bool okay, StateInfo si)
        {
            if (!okay)
            {
                return;
            }

            Unfreeze(from, si.m_Map, si.m_Start, si.m_End);
        }

        private static void DoUnfreeze(Map map, Point2D start, Point2D end, ref bool badDataFile, ref int totalUnfrozen)
        {
            start = map.Bound(start);
            end = map.Bound(end);

            var xStartBlock = start.X >> 3;
            var yStartBlock = start.Y >> 3;
            var xEndBlock = end.X >> 3;
            var yEndBlock = end.Y >> 3;

            int xTileStart = start.X, yTileStart = start.Y;
            int xTileWidth = end.X - start.X + 1, yTileHeight = end.Y - start.Y + 1;

            var matrix = map.Tiles;

            using var idxStream = OpenWrite(matrix.IndexStream);
            using var mulStream = OpenWrite(matrix.DataStream);
            if (idxStream == null || mulStream == null)
            {
                badDataFile = true;
                return;
            }

            var idxReader = new BinaryReader(idxStream);

            var idxWriter = new BinaryWriter(idxStream);
            var mulWriter = new BinaryWriter(mulStream);

            for (var x = xStartBlock; x <= xEndBlock; ++x)
            {
                for (var y = yStartBlock; y <= yEndBlock; ++y)
                {
                    var oldTiles = ReadStaticBlock(
                        idxReader,
                        mulStream,
                        x,
                        y,
                        matrix.BlockWidth,
                        matrix.BlockHeight,
                        out var oldTileCount
                    );

                    if (oldTileCount < 0)
                    {
                        continue;
                    }

                    var newTileCount = 0;
                    var newTiles = new StaticTile[oldTileCount];

                    int baseX = (x << 3) - xTileStart, baseY = (y << 3) - yTileStart;

                    for (var i = 0; i < oldTileCount; ++i)
                    {
                        var oldTile = oldTiles[i];

                        var px = baseX + oldTile.X;
                        var py = baseY + oldTile.Y;

                        if (px < 0 || px >= xTileWidth || py < 0 || py >= yTileHeight)
                        {
                            newTiles[newTileCount++] = oldTile;
                        }
                        else
                        {
                            ++totalUnfrozen;

                            Item item = new Static(oldTile.ID);

                            item.Hue = oldTile.Hue;

                            item.MoveToWorld(new Point3D(px + xTileStart, py + yTileStart, oldTile.Z), map);
                        }
                    }

                    var mulPos = -1;
                    var length = -1;
                    var extra = 0;

                    if (newTileCount > 0)
                    {
                        mulWriter.Seek(0, SeekOrigin.End);

                        mulPos = (int)mulWriter.BaseStream.Position;
                        length = newTileCount * 7;
                        extra = 1;

                        for (var i = 0; i < newTileCount; ++i)
                        {
                            var toWrite = newTiles[i];

                            mulWriter.Write((ushort)toWrite.ID);
                            mulWriter.Write((byte)toWrite.X);
                            mulWriter.Write((byte)toWrite.Y);
                            mulWriter.Write((sbyte)toWrite.Z);
                            mulWriter.Write((short)toWrite.Hue);
                        }

                        mulWriter.Flush();
                    }

                    var idxPos = (x * matrix.BlockHeight + y) * 12;

                    idxWriter.Seek(idxPos, SeekOrigin.Begin);
                    idxWriter.Write(mulPos);
                    idxWriter.Write(length);
                    idxWriter.Write(extra);

                    idxWriter.Flush();

                    matrix.SetStaticBlock(x, y, null);
                }
            }
        }

        public static void DoUnfreeze(Map map, ref bool badDataFile, ref int totalUnfrozen)
        {
            DoUnfreeze(map, Point2D.Zero, new Point2D(map.Width - 1, map.Height - 1), ref badDataFile, ref totalUnfrozen);
        }

        public static void Unfreeze(Mobile from, Map map, Point3D start, Point3D end)
        {
            var totalUnfrozen = 0;
            var badDataFile = false;

            if (map == null)
            {
                CommandLogging.WriteLine(
                    from,
                    "{0} {1} invoking unfreeze for every item in every map",
                    from.AccessLevel,
                    CommandLogging.Format(from)
                );

                DoUnfreeze(Map.Felucca, ref badDataFile, ref totalUnfrozen);
                DoUnfreeze(Map.Trammel, ref badDataFile, ref totalUnfrozen);
                DoUnfreeze(Map.Ilshenar, ref badDataFile, ref totalUnfrozen);
                DoUnfreeze(Map.Malas, ref badDataFile, ref totalUnfrozen);
                DoUnfreeze(Map.Tokuno, ref badDataFile, ref totalUnfrozen);
            }
            else if (start == NullP3D && end == NullP3D)
            {
                CommandLogging.WriteLine(
                    from,
                    "{0} {1} invoking unfreeze for every item in {2}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    map
                );

                DoUnfreeze(map, ref badDataFile, ref totalUnfrozen);
            }
            else
            {
                CommandLogging.WriteLine(
                    from,
                    "{0} {1} invoking unfreeze from {2} to {3} in {4}",
                    from.AccessLevel,
                    CommandLogging.Format(from),
                    new Point2D(start),
                    new Point2D(end),
                    map
                );

                DoUnfreeze(map, new Point2D(start), new Point2D(end), ref badDataFile, ref totalUnfrozen);
            }

            if (totalUnfrozen == 0 && badDataFile)
            {
                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        "Output data files could not be opened and the unfreeze operation has been aborted.<br><br>This probably means your server and client are using the same data files.  Instructions on how to resolve this can be found in the first warning window.",
                        0xFFC000,
                        320,
                        240
                    )
                );
            }
            else
            {
                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        $"Unfreeze operation completed successfully.<br><br>{totalUnfrozen} item{(totalUnfrozen != 1 ? "s were" : " was")} unfrozen.<br><br>You must restart your client and update it's data files to see the changes.",
                        0xFFC000,
                        320,
                        240
                    )
                );
            }
        }

        private static FileStream OpenWrite(FileStream orig)
        {
            if (orig == null)
            {
                return null;
            }

            try
            {
                return new FileStream(orig.Name, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch
            {
                return null;
            }
        }

        private static StaticTile[] ReadStaticBlock(
            BinaryReader idxReader, FileStream mulStream, int x, int y, int width,
            int height, out int count
        )
        {
            try
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    count = -1;
                    return m_TileBuffer;
                }

                idxReader.BaseStream.Seek((x * height + y) * 12, SeekOrigin.Begin);

                var lookup = idxReader.ReadInt32();
                var length = idxReader.ReadInt32();

                if (lookup < 0 || length <= 0)
                {
                    count = 0;
                }
                else
                {
                    count = length / 7;

                    mulStream.Seek(lookup, SeekOrigin.Begin);

                    if (m_TileBuffer.Length < count)
                    {
                        m_TileBuffer = new StaticTile[count];
                    }

                    var staTiles = m_TileBuffer;

                    if (m_Buffer == null || length > m_Buffer.Length)
                    {
                        m_Buffer = GC.AllocateUninitializedArray<byte>(length);
                    }

                    mulStream.Read(m_Buffer, 0, length);

                    var index = 0;

                    for (var i = 0; i < count; ++i)
                    {
                        staTiles[i]
                            .Set(
                                (ushort)(m_Buffer[index++] | (m_Buffer[index++] << 8)),
                                m_Buffer[index++],
                                m_Buffer[index++],
                                (sbyte)m_Buffer[index++],
                                (short)(m_Buffer[index++] | (m_Buffer[index++] << 8))
                            );
                    }
                }
            }
            catch
            {
                count = -1;
            }

            return m_TileBuffer;
        }

        private class DeltaState
        {
            public readonly List<Item> m_List;
            public readonly int m_X;
            public readonly int m_Y;

            public DeltaState(Point2D p)
            {
                m_X = p.X;
                m_Y = p.Y;
                m_List = new List<Item>();
            }
        }

        public class StateInfo
        {
            public Map m_Map;
            public Point3D m_Start, m_End;

            public StateInfo(Map map, Point3D start, Point3D end)
            {
                m_Map = map;
                m_Start = start;
                m_End = end;
            }
        }
    }
}
