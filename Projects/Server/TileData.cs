/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TileData.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server;

public struct LandData
{
    public string Name { get; set; }

    public TileFlag Flags { get; set; }

    public LandData(string name, TileFlag flags)
    {
        Name = name;
        Flags = flags;
    }
}

[PropertyObject]
public struct ItemData
{
    private byte _weight;
    private byte _quality;
    private ushort _animation;
    private byte _quantity;
    private byte _value;
    private byte _height;

    public ItemData(string name, TileFlag flags, int weight, int quality, int animation, int quantity, int value, int height)
    {
        Name = name;
        Flags = flags;
        _weight = (byte)weight;
        _quality = (byte)quality;
        _animation = (ushort)animation;
        _quantity = (byte)quantity;
        _value = (byte)value;
        _height = (byte)height;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public string Name { get; set; }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public TileFlag Flags { get; set; }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Weight
    {
        get => _weight;
        set => _weight = (byte)value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Quality
    {
        get => _quality;
        set => _quality = (byte)value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Animation
    {
        get => _animation;
        set => _animation = (ushort)value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Quantity
    {
        get => _quantity;
        set => _quantity = (byte)value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Value
    {
        get => _value;
        set => _value = (byte)value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int Height
    {
        get => _height;
        set => _height = (byte)value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public int CalcHeight => Bridge ? _height / 2 : _height;

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Door
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Door];
        set => this[TileFlag.Door] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Background
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Background];
        set => this[TileFlag.Background] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Bridge
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Bridge];
        set => this[TileFlag.Bridge] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Wall
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Wall];
        set => this[TileFlag.Wall] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Window
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Window];
        set => this[TileFlag.Window] = value;
    }

    public bool ImpassableSurface
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Impassable | TileFlag.Surface];
        set => this[TileFlag.Impassable | TileFlag.Surface] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Impassable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Impassable];
        set => this[TileFlag.Impassable] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Surface
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Surface];
        set => this[TileFlag.Surface] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Roof
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Roof];
        set => this[TileFlag.Roof] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool LightSource
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.LightSource];
        set => this[TileFlag.LightSource] = value;
    }

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Wet
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[TileFlag.Wet];
        set => this[TileFlag.Wet] = value;
    }

    public bool this[TileFlag flag]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Flags & flag) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }
    }
}

[Flags]
public enum TileFlag : ulong
{
    None = 0x00000000,
    Background = 0x00000001,
    Weapon = 0x00000002,
    Transparent = 0x00000004,
    Translucent = 0x00000008,
    Wall = 0x00000010,
    Damaging = 0x00000020,
    Impassable = 0x00000040,
    Wet = 0x00000080,
    Unknown1 = 0x00000100,
    Surface = 0x00000200,
    Bridge = 0x00000400,
    Generic = 0x00000800,
    Window = 0x00001000,
    NoShoot = 0x00002000,
    ArticleA = 0x00004000,
    ArticleAn = 0x00008000,
    Internal = 0x00010000,
    Foliage = 0x00020000,
    PartialHue = 0x00040000,
    Unknown2 = 0x00080000,
    Map = 0x00100000,
    Container = 0x00200000,
    Wearable = 0x00400000,
    LightSource = 0x00800000,
    Animation = 0x01000000,
    NoDiagonal = 0x02000000,
    Unknown3 = 0x04000000,
    Armor = 0x08000000,
    Roof = 0x10000000,
    Door = 0x20000000,
    StairBack = 0x40000000,
    StairRight = 0x80000000,

    HS33 = 0x0000000100000000,
    HS34 = 0x0000000200000000,
    HS35 = 0x0000000400000000,
    HS36 = 0x0000000800000000,
    HS37 = 0x0000001000000000,
    HS38 = 0x0000002000000000,
    HS39 = 0x0000004000000000,
    HS40 = 0x0000008000000000,
    HS41 = 0x0000010000000000,
    HS42 = 0x0000020000000000,
    HS43 = 0x0000040000000000,
    HS44 = 0x0000080000000000,
    HS45 = 0x0000100000000000,
    HS46 = 0x0000200000000000,
    HS47 = 0x0000400000000000,
    HS48 = 0x0000800000000000,
    HS49 = 0x0001000000000000,
    HS50 = 0x0002000000000000,
    HS51 = 0x0004000000000000,
    HS52 = 0x0008000000000000,
    HS53 = 0x0010000000000000,
    HS54 = 0x0020000000000000,
    HS55 = 0x0040000000000000,
    HS56 = 0x0080000000000000,
    HS57 = 0x0100000000000000,
    HS58 = 0x0200000000000000,
    HS59 = 0x0400000000000000,
    HS60 = 0x0800000000000000,
    HS61 = 0x1000000000000000,
    HS62 = 0x2000000000000000,
    HS63 = 0x4000000000000000,
    HS64 = 0x8000000000000000
}

public static class TileData
{
    public static LandData[] LandTable { get; } = new LandData[0x4000];
    public static ItemData[] ItemTable { get; } = new ItemData[0x10000];
    public static int MaxLandValue { get; private set; }
    public static int MaxItemValue { get; private set; }

    static TileData()
    {
        if (Core.IsRunningFromXUnit)
        {
            return;
        }

        Load();
    }

    private static unsafe void Load()
    {
        var filePath = Core.FindDataFile("tiledata.mul");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var bin = new BinaryReader(fs);

        bool is64BitFlags;
        const int landLength = 0x4000;
        int itemLength;

        if (fs.Length >= 3188736) // 7.0.9.0
        {
            is64BitFlags = true;
            itemLength = 0x10000;
        }
        else if (fs.Length >= 1644544) // 7.0.0.0
        {
            is64BitFlags = false;
            itemLength = 0x8000;
        }
        else
        {
            is64BitFlags = false;
            itemLength = 0x4000;
        }

        Span<byte> buffer = stackalloc byte[20];

        for (var i = 0; i < landLength; i++)
        {
            if (is64BitFlags ? i == 1 || i > 0 && (i & 0x1F) == 0 : (i & 0x1F) == 0)
            {
                bin.ReadInt32(); // header
            }

            var flags = (TileFlag)(is64BitFlags ? bin.ReadUInt64() : bin.ReadUInt32());
            bin.ReadInt16(); // skip 2 bytes -- textureID

            bin.Read(buffer);
            var terminator = buffer.IndexOfTerminator(1);
            var name = Encoding.ASCII.GetString(buffer[..(terminator < 0 ? buffer.Length : terminator)]);
            LandTable[i] = new LandData(name.Intern(), flags);
        }

        for (var i = 0; i < itemLength; i++)
        {
            if ((i & 0x1F) == 0)
            {
                bin.ReadInt32(); // header
            }

            var flags = (TileFlag)(is64BitFlags ? bin.ReadUInt64() : bin.ReadUInt32());
            int weight = bin.ReadByte();
            int quality = bin.ReadByte();
            int animation = bin.ReadUInt16();
            bin.ReadByte();
            int quantity = bin.ReadByte();
            bin.ReadInt32();
            bin.ReadByte();
            int value = bin.ReadByte();
            int height = bin.ReadByte();

            bin.Read(buffer);
            var terminator = buffer.IndexOfTerminator(1);
            var name = Encoding.ASCII.GetString(buffer[..(terminator < 0 ? buffer.Length : terminator)]);
            ItemTable[i] = new ItemData(
                name.Intern(),
                flags,
                weight,
                quality,
                animation,
                quantity,
                value,
                height
            );
        }

        MaxLandValue = LandTable.Length - 1;
        MaxItemValue = ItemTable.Length - 1;
    }
}
