/***************************************************************************
 *                                TileData.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Text;

namespace Server
{
  public struct LandData
  {
    public LandData(string name, TileFlag flags)
    {
      Name = name;
      Flags = flags;
    }

    public string Name { get; set; }

    public TileFlag Flags { get; set; }
  }

  public struct ItemData
  {
    private byte m_Weight;
    private byte m_Quality;
    private byte m_Quantity;
    private byte m_Value;
    private byte m_Height;

    public ItemData(string name, TileFlag flags, int weight, int quality, int quantity, int value, int height)
    {
      Name = name;
      Flags = flags;
      m_Weight = (byte)weight;
      m_Quality = (byte)quality;
      m_Quantity = (byte)quantity;
      m_Value = (byte)value;
      m_Height = (byte)height;
    }

    public string Name { get; set; }

    public TileFlag Flags { get; set; }

    public bool Bridge
    {
      get => (Flags & TileFlag.Bridge) != 0;
      set
      {
        if (value)
          Flags |= TileFlag.Bridge;
        else
          Flags &= ~TileFlag.Bridge;
      }
    }

    public bool Impassable
    {
      get => (Flags & TileFlag.Impassable) != 0;
      set
      {
        if (value)
          Flags |= TileFlag.Impassable;
        else
          Flags &= ~TileFlag.Impassable;
      }
    }

    public bool Surface
    {
      get => (Flags & TileFlag.Surface) != 0;
      set
      {
        if (value)
          Flags |= TileFlag.Surface;
        else
          Flags &= ~TileFlag.Surface;
      }
    }

    public int Weight
    {
      get => m_Weight;
      set => m_Weight = (byte)value;
    }

    public int Quality
    {
      get => m_Quality;
      set => m_Quality = (byte)value;
    }

    public int Quantity
    {
      get => m_Quantity;
      set => m_Quantity = (byte)value;
    }

    public int Value
    {
      get => m_Value;
      set => m_Value = (byte)value;
    }

    public int Height
    {
      get => m_Height;
      set => m_Height = (byte)value;
    }

    public int CalcHeight
    {
      get
      {
        if ((Flags & TileFlag.Bridge) != 0)
          return m_Height / 2;
        return m_Height;
      }
    }
  }

  [Flags]
  public enum TileFlag : long
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
    StairRight = 0x80000000
  }

  public static class TileData
  {
    private static readonly byte[] m_StringBuffer = new byte[20];

    static TileData()
    {
      ItemTable = new ItemData[0x10000];
      LandTable = new LandData[0x4000];

      if (Core.IsRunningFromXUnit) return;

      var filePath = Core.FindDataFile("tiledata.mul");

      using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      var bin = new BinaryReader(fs);

      var is7090 = fs.Length == 3188736;
      bool is7000 = fs.Length == 1644544;

      for (var i = 0; i < 0x4000; i++)
      {
        // header
        if (is7090)
        {
          if (i == 1 || i > 0 && (i & 0x1F) == 0)
            bin.ReadInt32();
        }
        else if ((i & 0x1F) == 0)
          bin.ReadInt32();

        var flags = (TileFlag)(is7090 ? bin.ReadInt64() : bin.ReadInt32());
        bin.ReadInt16(); // skip 2 bytes -- textureID

        LandTable[i] = new LandData(ReadNameString(bin), flags);
      }

      int length = is7090 ? 0x10000 : is7000 ? 0x8000 : 0x4000;

      for (var i = 0; i < length; i++)
      {
        if ((i & 0x1F) == 0) bin.ReadInt32(); // header

        var flags = (TileFlag)(is7090 ? bin.ReadInt64() : bin.ReadInt32());
        int weight = bin.ReadByte();
        int quality = bin.ReadByte();
        bin.ReadInt16();
        bin.ReadByte();
        int quantity = bin.ReadByte();
        bin.ReadInt32();
        bin.ReadByte();
        int value = bin.ReadByte();
        int height = bin.ReadByte();

        ItemTable[i] = new ItemData(
          ReadNameString(bin), flags, weight, quality, quantity, value, height
        );
      }

      MaxLandValue = LandTable.Length - 1;
      MaxItemValue = ItemTable.Length - 1;
    }

    public static LandData[] LandTable { get; }

    public static ItemData[] ItemTable { get; }

    public static int MaxLandValue { get; }

    public static int MaxItemValue { get; }

    private static string ReadNameString(BinaryReader bin)
    {
      bin.Read(m_StringBuffer, 0, 20);

      int count = 0;

      while (count < 20)
      {
        if (m_StringBuffer[count] == 0)
          break;

        count++;
      }

      return Encoding.ASCII.GetString(new ReadOnlySpan<byte>(m_StringBuffer, 0, count));
    }
  }
}
