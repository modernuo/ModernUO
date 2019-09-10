/***************************************************************************
 *                          StaticPacketHandlers.cs
 *                            -------------------
 *   begin                : March 15, 2019
 *   copyright            : (C) The ModernUO Team
 *   email                : hi@modernuo.com
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

using System.Collections.Concurrent;

namespace Server.Network
{
  public static class StaticPacketHandlers
  {
    private static ConcurrentDictionary<IPropertyListObject,OPLInfo> OPLInfoPackets = new ConcurrentDictionary<IPropertyListObject,OPLInfo>();
    private static ConcurrentDictionary<IPropertyListObject,ObjectPropertyList> ObjectPropertyListPackets = new ConcurrentDictionary<IPropertyListObject,ObjectPropertyList>();

    private static ConcurrentDictionary<Item,WorldItem> WorldItemPackets = new ConcurrentDictionary<Item,WorldItem>();
    private static ConcurrentDictionary<Item,WorldItemSA> WorldItemSAPackets = new ConcurrentDictionary<Item,WorldItemSA>();
    private static ConcurrentDictionary<Item,WorldItemHS> WorldItemHSPackets = new ConcurrentDictionary<Item,WorldItemHS>();

    public static OPLInfo GetOPLInfoPacket(IPropertyListObject obj)
    {
      return OPLInfoPackets.GetOrAdd(obj, value =>
      {
        OPLInfo packet = new OPLInfo(value.PropertyList);
        packet.SetStatic();
        return packet;
      });
    }

    public static OPLInfo FreeOPLInfoPacket(IPropertyListObject obj)
    {
      if (OPLInfoPackets.TryRemove(obj, out OPLInfo p))
        Packet.Release(p);

      return p;
    }

    public static ObjectPropertyList GetOPLPacket(IPropertyListObject obj)
    {
      return ObjectPropertyListPackets.GetOrAdd(obj, value =>
      {
        ObjectPropertyList list = new ObjectPropertyList(value);

        value.GetProperties(list);
        if (value is Item item)
          item.AppendChildProperties(list);

        list.Terminate();
        list.SetStatic();
        return list;
      });
    }

    public static ObjectPropertyList FreeOPLPacket(IPropertyListObject obj)
    {
      if (ObjectPropertyListPackets.TryRemove(obj, out ObjectPropertyList list))
        Packet.Release(list);

      return list;
    }

    public static WorldItem GetWorldItemPacket(Item item)
    {
      return WorldItemPackets.GetOrAdd(item, value =>
      {
        WorldItem packet = new WorldItem(value);
        packet.SetStatic();
        return packet;
      });
    }

    public static WorldItemSA GetWorldItemSAPacket(Item item)
    {
      return WorldItemSAPackets.GetOrAdd(item, value =>
      {
        WorldItemSA packet = new WorldItemSA(value);
        packet.SetStatic();
        return packet;
      });
    }

    public static WorldItemHS GetWorldItemHSPacket(Item item)
    {
      return WorldItemHSPackets.GetOrAdd(item, value =>
      {
        WorldItemHS packet = new WorldItemHS(value);
        packet.SetStatic();
        return packet;
      });
    }

    public static void FreeWorldItemPackets(Item item)
    {
      if (WorldItemPackets.TryRemove(item, out WorldItem wi))
        Packet.Release(wi);

      if (WorldItemSAPackets.TryRemove(item, out WorldItemSA wisa))
        Packet.Release(wisa);

      if (WorldItemHSPackets.TryRemove(item, out WorldItemHS wihs))
        Packet.Release(wihs);
    }
  }
}
