using System;
using System.Collections.Generic;
using System.Net;
using Server.Guilds;

namespace Server
{
  public interface IGenericReader
  {
    string ReadString();
    DateTime ReadDateTime();
    DateTimeOffset ReadDateTimeOffset();
    TimeSpan ReadTimeSpan();
    DateTime ReadDeltaTime();
    decimal ReadDecimal();
    long ReadLong();
    ulong ReadULong();
    int ReadInt();
    uint ReadUInt();
    short ReadShort();
    ushort ReadUShort();
    double ReadDouble();
    float ReadFloat();
    char ReadChar();
    byte ReadByte();
    sbyte ReadSByte();
    bool ReadBool();
    int ReadEncodedInt();
    IPAddress ReadIPAddress();
    Point3D ReadPoint3D();
    Point2D ReadPoint2D();
    Rectangle2D ReadRect2D();
    Rectangle3D ReadRect3D();
    Map ReadMap();
    IEntity ReadEntity();
    Item ReadItem();
    Mobile ReadMobile();
    BaseGuild ReadGuild();
    T ReadItem<T>() where T : Item;
    T ReadMobile<T>() where T : Mobile;
    T ReadGuild<T>() where T : BaseGuild;
    List<Item> ReadStrongItemList();
    List<T> ReadStrongItemList<T>() where T : Item;
    List<Mobile> ReadStrongMobileList();
    List<T> ReadStrongMobileList<T>() where T : Mobile;
    List<BaseGuild> ReadStrongGuildList();
    List<T> ReadStrongGuildList<T>() where T : BaseGuild;
    HashSet<Item> ReadItemSet();
    HashSet<T> ReadItemSet<T>() where T : Item;
    HashSet<Mobile> ReadMobileSet();
    HashSet<T> ReadMobileSet<T>() where T : Mobile;
    HashSet<BaseGuild> ReadGuildSet();
    HashSet<T> ReadGuildSet<T>() where T : BaseGuild;
    Race ReadRace();
    bool End();
  }
}
