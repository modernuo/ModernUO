using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Server.Guilds;

namespace Server
{
  public sealed class BinaryFileReader : IGenericReader
  {
    private BinaryReader m_File;

    public BinaryFileReader(BinaryReader br) => m_File = br;

    public long Position => m_File.BaseStream.Position;

    public void Close()
    {
      m_File.Close();
    }

    public long Seek(long offset, SeekOrigin origin) => m_File.BaseStream.Seek(offset, origin);

    public string ReadString() => ReadByte() != 0 ? m_File.ReadString() : null;

    public DateTime ReadDeltaTime()
    {
      long ticks = m_File.ReadInt64();
      long now = DateTime.UtcNow.Ticks;

      if (ticks > 0 && ticks + now < 0)
        return DateTime.MaxValue;
      if (ticks < 0 && ticks + now < 0)
        return DateTime.MinValue;

      try
      {
        return new DateTime(now + ticks);
      }
      catch
      {
        if (ticks > 0) return DateTime.MaxValue;
        return DateTime.MinValue;
      }
    }

    public IPAddress ReadIPAddress() => new IPAddress(m_File.ReadInt64());

    public int ReadEncodedInt()
    {
      int v = 0, shift = 0;
      byte b;

      do
      {
        b = m_File.ReadByte();
        v |= (b & 0x7F) << shift;
        shift += 7;
      } while (b >= 0x80);

      return v;
    }

    public DateTime ReadDateTime() => new DateTime(m_File.ReadInt64());

    public DateTimeOffset ReadDateTimeOffset()
    {
      long ticks = m_File.ReadInt64();
      TimeSpan offset = new TimeSpan(m_File.ReadInt64());

      return new DateTimeOffset(ticks, offset);
    }

    public TimeSpan ReadTimeSpan() => new TimeSpan(m_File.ReadInt64());

    public decimal ReadDecimal() => m_File.ReadDecimal();

    public long ReadLong() => m_File.ReadInt64();

    public ulong ReadULong() => m_File.ReadUInt64();

    public int ReadInt() => m_File.ReadInt32();

    public uint ReadUInt() => m_File.ReadUInt32();

    public short ReadShort() => m_File.ReadInt16();

    public ushort ReadUShort() => m_File.ReadUInt16();

    public double ReadDouble() => m_File.ReadDouble();

    public float ReadFloat() => m_File.ReadSingle();

    public char ReadChar() => m_File.ReadChar();

    public byte ReadByte() => m_File.ReadByte();

    public sbyte ReadSByte() => m_File.ReadSByte();

    public bool ReadBool() => m_File.ReadBoolean();

    public Point3D ReadPoint3D() => new Point3D(ReadInt(), ReadInt(), ReadInt());

    public Point2D ReadPoint2D() => new Point2D(ReadInt(), ReadInt());

    public Rectangle2D ReadRect2D() => new Rectangle2D(ReadPoint2D(), ReadPoint2D());

    public Rectangle3D ReadRect3D() => new Rectangle3D(ReadPoint3D(), ReadPoint3D());

    public Map ReadMap() => Map.Maps[ReadByte()];

    public IEntity ReadEntity()
    {
      Serial serial = ReadUInt();
      IEntity entity = World.FindEntity(serial);
      if (entity == null)
        return new Entity(serial, new Point3D(0, 0, 0), Map.Internal);
      return entity;
    }

    public Item ReadItem() => World.FindItem(ReadUInt());

    public Mobile ReadMobile() => World.FindMobile(ReadUInt());

    public BaseGuild ReadGuild() => BaseGuild.Find(ReadUInt());

    public T ReadItem<T>() where T : Item => ReadItem() as T;

    public T ReadMobile<T>() where T : Mobile => ReadMobile() as T;

    public T ReadGuild<T>() where T : BaseGuild => ReadGuild() as T;

    public List<Item> ReadStrongItemList() => ReadStrongItemList<Item>();

    public List<T> ReadStrongItemList<T>() where T : Item
    {
      int count = ReadInt();

      if (count > 0)
      {
        List<T> list = new List<T>(count);

        for (int i = 0; i < count; ++i)
          if (ReadItem() is T item)
            list.Add(item);

        return list;
      }

      return new List<T>();
    }

    public HashSet<Item> ReadItemSet() => ReadItemSet<Item>();

    public HashSet<T> ReadItemSet<T>() where T : Item
    {
      int count = ReadInt();

      if (count > 0)
      {
        HashSet<T> set = new HashSet<T>();

        for (int i = 0; i < count; ++i)
          if (ReadItem() is T item)
            set.Add(item);

        return set;
      }

      return new HashSet<T>();
    }

    public List<Mobile> ReadStrongMobileList() => ReadStrongMobileList<Mobile>();

    public List<T> ReadStrongMobileList<T>() where T : Mobile
    {
      int count = ReadInt();

      if (count > 0)
      {
        List<T> list = new List<T>(count);

        for (int i = 0; i < count; ++i)
          if (ReadMobile() is T m)
            list.Add(m);

        return list;
      }

      return new List<T>();
    }

    public HashSet<Mobile> ReadMobileSet() => ReadMobileSet<Mobile>();

    public HashSet<T> ReadMobileSet<T>() where T : Mobile
    {
      int count = ReadInt();

      if (count > 0)
      {
        HashSet<T> set = new HashSet<T>();

        for (int i = 0; i < count; ++i)
          if (ReadMobile() is T item)
            set.Add(item);

        return set;
      }

      return new HashSet<T>();
    }

    public List<BaseGuild> ReadStrongGuildList() => ReadStrongGuildList<BaseGuild>();

    public List<T> ReadStrongGuildList<T>() where T : BaseGuild
    {
      int count = ReadInt();

      if (count > 0)
      {
        List<T> list = new List<T>(count);

        for (int i = 0; i < count; ++i)
          if (ReadGuild() is T g)
            list.Add(g);

        return list;
      }

      return new List<T>();
    }

    public HashSet<BaseGuild> ReadGuildSet() => ReadGuildSet<BaseGuild>();

    public HashSet<T> ReadGuildSet<T>() where T : BaseGuild
    {
      int count = ReadInt();

      if (count > 0)
      {
        HashSet<T> set = new HashSet<T>();

        for (int i = 0; i < count; ++i)
          if (ReadGuild() is T item)
            set.Add(item);

        return set;
      }

      return new HashSet<T>();
    }

    public Race ReadRace() => Race.Races[ReadByte()];

    public bool End() => m_File.PeekChar() == -1;
  }

}
