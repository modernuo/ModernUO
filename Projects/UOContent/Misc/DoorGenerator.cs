using System;
using Server.Items;
using Server.Logging;
using Server.Network;

namespace Server;

public static class DoorGenerator
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(DoorGenerator));

    private static readonly Rectangle2D[] _britRegions =
    {
        new(new Point2D(250, 750), new Point2D(775, 1330)),
        new(new Point2D(525, 2095), new Point2D(925, 2430)),
        new(new Point2D(1025, 2155), new Point2D(1265, 2310)),
        new(new Point2D(1635, 2430), new Point2D(1705, 2508)),
        new(new Point2D(1775, 2605), new Point2D(2165, 2975)),
        new(new Point2D(1055, 3520), new Point2D(1570, 4075)),
        new(new Point2D(2860, 3310), new Point2D(3120, 3630)),
        new(new Point2D(2470, 1855), new Point2D(3950, 3045)),
        new(new Point2D(3425, 990), new Point2D(3900, 1455)),
        new(new Point2D(4175, 735), new Point2D(4840, 1600)),
        new(new Point2D(2375, 330), new Point2D(3100, 1045)),
        new(new Point2D(2100, 1090), new Point2D(2310, 1450)),
        new(new Point2D(1495, 1400), new Point2D(1550, 1475)),
        new(new Point2D(1085, 1520), new Point2D(1415, 1910)),
        new(new Point2D(1410, 1500), new Point2D(1745, 1795)),
        new(new Point2D(5120, 2300), new Point2D(6143, 4095))
    };

    private static readonly Rectangle2D[] _ilshRegions =
    {
        new(new Point2D(0, 0), new Point2D(288 * 8, 200 * 8))
    };

    private static readonly Rectangle2D[] _malasRegions =
    {
        new(new Point2D(0, 0), new Point2D(320 * 8, 256 * 8))
    };

    private static readonly int[] _southFrames =
    {
        0x0006,
        0x0008,
        0x000B,
        0x001A,
        0x001B,
        0x001F,
        0x0038,
        0x0057,
        0x0059,
        0x005B,
        0x005D,
        0x0080,
        0x0081,
        0x0082,
        0x0084,
        0x0090,
        0x0091,
        0x0094,
        0x0096,
        0x0099,
        0x00A6,
        0x00A7,
        0x00AA,
        0x00AE,
        0x00B0,
        0x00B3,
        0x00C7,
        0x00C9,
        0x00F8,
        0x00FA,
        0x00FD,
        0x00FE,
        0x0100,
        0x0103,
        0x0104,
        0x0106,
        0x0109,
        0x0127,
        0x0129,
        0x012B,
        0x012D,
        0x012F,
        0x0131,
        0x0132,
        0x0134,
        0x0135,
        0x0137,
        0x0139,
        0x013B,
        0x014C,
        0x014E,
        0x014F,
        0x0151,
        0x0153,
        0x0155,
        0x0157,
        0x0158,
        0x015A,
        0x015D,
        0x015E,
        0x015F,
        0x0162,
        0x01CF,
        0x01D1,
        0x01D4,
        0x01FF,
        0x0204,
        0x0206,
        0x0208,
        0x020A
    };

    private static readonly int[] _northFrames =
    {
        0x0006,
        0x0008,
        0x000D,
        0x001A,
        0x001B,
        0x0020,
        0x003A,
        0x0057,
        0x0059,
        0x005B,
        0x005D,
        0x0080,
        0x0081,
        0x0082,
        0x0084,
        0x0090,
        0x0091,
        0x0094,
        0x0096,
        0x0099,
        0x00A6,
        0x00A7,
        0x00AC,
        0x00AE,
        0x00B0,
        0x00C7,
        0x00C9,
        0x00F8,
        0x00FA,
        0x00FD,
        0x00FE,
        0x0100,
        0x0103,
        0x0104,
        0x0106,
        0x0109,
        0x0127,
        0x0129,
        0x012B,
        0x012D,
        0x012F,
        0x0131,
        0x0132,
        0x0134,
        0x0135,
        0x0137,
        0x0139,
        0x013B,
        0x014C,
        0x014E,
        0x014F,
        0x0151,
        0x0153,
        0x0155,
        0x0157,
        0x0158,
        0x015A,
        0x015D,
        0x015E,
        0x015F,
        0x0162,
        0x01CF,
        0x01D1,
        0x01D4,
        0x01FF,
        0x0201,
        0x0204,
        0x0208,
        0x020A
    };

    private static readonly int[] _eastFrames =
    {
        0x0007,
        0x000A,
        0x001A,
        0x001C,
        0x001E,
        0x0037,
        0x0058,
        0x0059,
        0x005C,
        0x005E,
        0x0080,
        0x0081,
        0x0082,
        0x0084,
        0x0090,
        0x0092,
        0x0095,
        0x0097,
        0x0098,
        0x00A6,
        0x00A8,
        0x00AB,
        0x00AE,
        0x00AF,
        0x00B2,
        0x00C7,
        0x00C8,
        0x00EA,
        0x00F8,
        0x00F9,
        0x00FC,
        0x00FE,
        0x00FF,
        0x0102,
        0x0104,
        0x0105,
        0x0108,
        0x0127,
        0x0128,
        0x012B,
        0x012C,
        0x012E,
        0x0130,
        0x0132,
        0x0133,
        0x0135,
        0x0136,
        0x0138,
        0x013A,
        0x014C,
        0x014D,
        0x014F,
        0x0150,
        0x0152,
        0x0154,
        0x0156,
        0x0158,
        0x0159,
        0x015C,
        0x015E,
        0x0160,
        0x0163,
        0x01CF,
        0x01D0,
        0x01D3,
        0x01FF,
        0x0203,
        0x0205,
        0x0207,
        0x0209
    };

    private static readonly int[] _westFrames =
    {
        0x0007,
        0x000C,
        0x001A,
        0x001C,
        0x0021,
        0x0039,
        0x0058,
        0x0059,
        0x005C,
        0x005E,
        0x0080,
        0x0081,
        0x0082,
        0x0084,
        0x0090,
        0x0092,
        0x0095,
        0x0097,
        0x0098,
        0x00A6,
        0x00A8,
        0x00AD,
        0x00AE,
        0x00AF,
        0x00B5,
        0x00C7,
        0x00C8,
        0x00EA,
        0x00F8,
        0x00F9,
        0x00FC,
        0x00FE,
        0x00FF,
        0x0102,
        0x0104,
        0x0105,
        0x0108,
        0x0127,
        0x0128,
        0x012C,
        0x012E,
        0x0130,
        0x0132,
        0x0133,
        0x0135,
        0x0136,
        0x0138,
        0x013A,
        0x014C,
        0x014D,
        0x014F,
        0x0150,
        0x0152,
        0x0154,
        0x0156,
        0x0158,
        0x0159,
        0x015C,
        0x015E,
        0x0160,
        0x0163,
        0x01CF,
        0x01D0,
        0x01D3,
        0x01FF,
        0x0200,
        0x0203,
        0x0207,
        0x0209
    };

    private static Map m_Map;
    private static int m_Count;

    public static void Configure()
    {
        CommandSystem.Register("DoorGen", AccessLevel.Developer, DoorGen_OnCommand);
    }

    [Usage("DoorGen"), Description("Generates doors by analyzing the map. Slow.")]
    public static void DoorGen_OnCommand(CommandEventArgs e)
    {
        Generate();
    }

    public static void Generate()
    {
        World.Broadcast(0x35, true, "Generating doors, please wait.");

        NetState.FlushAll();

        m_Map = Map.Trammel;
        m_Count = 0;

        for (var i = 0; i < _britRegions.Length; ++i)
        {
            Generate(_britRegions[i]);
        }

        var trammelCount = m_Count;

        m_Map = Map.Felucca;
        m_Count = 0;

        for (var i = 0; i < _britRegions.Length; ++i)
        {
            Generate(_britRegions[i]);
        }

        var feluccaCount = m_Count;

        m_Map = Map.Ilshenar;
        m_Count = 0;

        for (var i = 0; i < _ilshRegions.Length; ++i)
        {
            Generate(_ilshRegions[i]);
        }

        var ilshenarCount = m_Count;

        m_Map = Map.Malas;
        m_Count = 0;

        for (var i = 0; i < _malasRegions.Length; ++i)
        {
            Generate(_malasRegions[i]);
        }

        var malasCount = m_Count;

        World.Broadcast(
            0x35,
            true,
            $"Door generation complete. Trammel: {trammelCount}; Felucca: {feluccaCount}; Ilshenar: {ilshenarCount}; Malas: {malasCount};"
        );
    }

    public static bool IsFrame(int id, int[] list)
    {
        if (id > list[^1])
        {
            return false;
        }

        for (var i = 0; i < list.Length; ++i)
        {
            var delta = id - list[i];

            if (delta < 0)
            {
                return false;
            }

            if (delta == 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsNorthFrame(int id) => IsFrame(id, _northFrames);

    public static bool IsSouthFrame(int id) => IsFrame(id, _southFrames);

    public static bool IsWestFrame(int id) => IsFrame(id, _westFrames);

    public static bool IsEastFrame(int id) => IsFrame(id, _eastFrames);

    public static bool IsEastFrame(int x, int y, int z, out int newZ)
    {
        foreach (var tile in m_Map.Tiles.GetStaticTiles(x, y))
        {
            if (!IsEastFrame(tile.ID))
            {
                continue;
            }

            var delta = tile.Z - z;

            if (delta is >= -1 and <= 1)
            {
                newZ = tile.Z;
                return true;
            }
        }

        newZ = -1;
        return false;
    }

    public static bool IsSouthFrame(int x, int y, int z, out int newZ)
    {
        foreach (var tile in m_Map.Tiles.GetStaticTiles(x, y))
        {
            if (!IsSouthFrame(tile.ID))
            {
                continue;
            }

            var delta = tile.Z - z;

            if (delta is >= -1 and <= 1)
            {
                newZ = tile.Z;
                return true;
            }
        }

        newZ = -1;
        return false;
    }

    public static BaseDoor AddDoor(int x, int y, int z, int newZ, DoorFacing facing)
    {
        var minZ = Math.Min(z, newZ);
        if (!m_Map.CanFit(x, y, minZ, 16, false, false))
        {
            return null;
        }

        if (y == 1743 && x is >= 1343 and <= 1344)
        {
            return null;
        }

        if (y == 1679 && x is >= 1392 and <= 1393)
        {
            return null;
        }

        if (x == 1320 && y is >= 1618 and <= 1640)
        {
            return null;
        }

        if (x == 1383 && y is >= 1642 and <= 1643)
        {
            return null;
        }

        // Ilshenar ruins
        if (m_Map == Map.Ilshenar && x is >= 644 and <= 670 && y is >= 925 and <= 941 || x == 985 && y == 994)
        {
            return null;
        }

        if (z != newZ)
        {
            logger.Warning($"Door at {x}, {y}, {z} ({m_Map}) has mismatched Z levels: {z} != {newZ}");
        }

        BaseDoor door = new DarkWoodDoor(facing);
        door.MoveToWorld(new Point3D(x, y, minZ), m_Map);

        ++m_Count;

        return door;
    }

    public static void Generate(Rectangle2D region)
    {
        for (var rx = 0; rx < region.Width; ++rx)
        {
            for (var ry = 0; ry < region.Height; ++ry)
            {
                var vx = rx + region.X;
                var vy = ry + region.Y;

                foreach (var tile in m_Map.Tiles.GetStaticTiles(vx, vy))
                {
                    var id = tile.ID;
                    var z = tile.Z;
                    int newZ;

                    if (IsWestFrame(id))
                    {
                        if (IsEastFrame(vx + 2, vy, z, out newZ))
                        {
                            AddDoor(vx + 1, vy, z, newZ, DoorFacing.WestCW);
                        }
                        else if (IsEastFrame(vx + 3, vy, z, out newZ))
                        {
                            var first = AddDoor(vx + 1, vy, z, newZ, DoorFacing.WestCW);
                            var second = AddDoor(vx + 2, vy, z, newZ, DoorFacing.EastCCW);

                            if (first != null && second != null)
                            {
                                first.Link = second;
                                second.Link = first;
                            }
                            else
                            {
                                first?.Delete();
                                second?.Delete();
                            }
                        }
                    }
                    else if (IsNorthFrame(id))
                    {
                        if (IsSouthFrame(vx, vy + 2, z, out newZ))
                        {
                            AddDoor(vx, vy + 1, z, newZ, DoorFacing.SouthCW);
                        }
                        else if (IsSouthFrame(vx, vy + 3, z, out newZ))
                        {
                            var first = AddDoor(vx, vy + 1, z, newZ, DoorFacing.NorthCCW);
                            var second = AddDoor(vx, vy + 2, z, newZ, DoorFacing.SouthCW);

                            if (first != null && second != null)
                            {
                                first.Link = second;
                                second.Link = first;
                            }
                            else
                            {
                                first?.Delete();
                                second?.Delete();
                            }
                        }
                    }
                }
            }
        }
    }
}
