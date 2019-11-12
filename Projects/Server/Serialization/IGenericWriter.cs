using System;
using System.Collections.Generic;
using System.Net;
using Server.Guilds;

namespace Server
{
  public interface IGenericWriter
  {
    long Position { get; }

    void Close();

    void Write(string value);
    void Write(DateTime value);
    void Write(DateTimeOffset value);
    void Write(TimeSpan value);
    void Write(decimal value);
    void Write(long value);
    void Write(ulong value);
    void Write(int value);
    void Write(uint value);
    void Write(short value);
    void Write(ushort value);
    void Write(double value);
    void Write(float value);
    void Write(char value);
    void Write(byte value);
    void Write(sbyte value);
    void Write(bool value);
    void WriteEncodedInt(int value);
    void Write(IPAddress value);

    void WriteDeltaTime(DateTime value);

    void Write(Point3D value);
    void Write(Point2D value);
    void Write(Rectangle2D value);
    void Write(Rectangle3D value);
    void Write(Map value);

    void WriteEntity(IEntity value);
    void Write(Item value);
    void Write(Mobile value);
    void Write(BaseGuild value);

    void WriteItem<T>(T value) where T : Item;
    void WriteMobile<T>(T value) where T : Mobile;
    void WriteGuild<T>(T value) where T : BaseGuild;

    void Write(Race value);

    void Write(List<Item> list);
    void Write(List<Item> list, bool tidy);

    void WriteItemList<T>(List<T> list) where T : Item;
    void WriteItemList<T>(List<T> list, bool tidy) where T : Item;

    void Write(HashSet<Item> list);
    void Write(HashSet<Item> list, bool tidy);

    void WriteItemSet<T>(HashSet<T> set) where T : Item;
    void WriteItemSet<T>(HashSet<T> set, bool tidy) where T : Item;

    void Write(List<Mobile> list);
    void Write(List<Mobile> list, bool tidy);

    void WriteMobileList<T>(List<T> list) where T : Mobile;
    void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile;

    void Write(HashSet<Mobile> list);
    void Write(HashSet<Mobile> list, bool tidy);

    void WriteMobileSet<T>(HashSet<T> set) where T : Mobile;
    void WriteMobileSet<T>(HashSet<T> set, bool tidy) where T : Mobile;

    void Write(List<BaseGuild> list);
    void Write(List<BaseGuild> list, bool tidy);

    void WriteGuildList<T>(List<T> list) where T : BaseGuild;
    void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild;

    void Write(HashSet<BaseGuild> list);
    void Write(HashSet<BaseGuild> list, bool tidy);

    void WriteGuildSet<T>(HashSet<T> set) where T : BaseGuild;
    void WriteGuildSet<T>(HashSet<T> set, bool tidy) where T : BaseGuild;
  }
}
