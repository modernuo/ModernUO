/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingEntityPackets.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network;

public static class IncomingEntityPackets
{
    public static bool SingleClickProps { get; set; }

    public static void Configure()
    {
        IncomingPackets.Register(0x06, 5, true, UseReq);
        IncomingPackets.Register(0x09, 5, true, LookReq);
        IncomingPackets.Register(0xB6, 9, true, ObjectHelpRequest);
        IncomingPackets.Register(0xD6, 0, true, BatchQueryProperties);
    }

    public static void ObjectHelpRequest(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;

        var serial = (Serial)reader.ReadUInt32();
        int unk = reader.ReadByte();
        var lang = reader.ReadAscii(3);

        if (serial.IsItem)
        {
            var item = World.FindItem(serial);

            if (item != null && from.Map == item.Map && Utility.InUpdateRange(item.GetWorldLocation(), from.Location) &&
                from.CanSee(item))
            {
                item.OnHelpRequest(from);
            }
        }
        else if (serial.IsMobile)
        {
            var m = World.FindMobile(serial);

            if (m != null && from.Map == m.Map && Utility.InUpdateRange(m.Location, from.Location) && from.CanSee(m))
            {
                m.OnHelpRequest(m);
            }
        }
    }

    public static void UseReq(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from.AccessLevel >= AccessLevel.Counselor || Core.TickCount - from.NextActionTime >= 0)
        {
            var value = reader.ReadUInt32();

            if ((value & ~0x7FFFFFFF) != 0)
            {
                from.OnPaperdollRequest();
            }
            else
            {
                Serial s = (Serial)value;

                if (s.IsMobile)
                {
                    var m = World.FindMobile(s);

                    if (m?.Deleted == false)
                    {
                        from.Use(m);
                    }
                }
                else if (s.IsItem)
                {
                    var item = World.FindItem(s);

                    if (item?.Deleted == false)
                    {
                        from.Use(item);
                    }
                }
            }

            from.NextActionTime = Core.TickCount + Mobile.ActionDelay;
        }
        else
        {
            from.SendActionMessage();
        }
    }

    public static void LookReq(NetState state, CircularBufferReader reader, int packetLength)
    {
        var from = state.Mobile;

        Serial s = (Serial)reader.ReadUInt32();

        if (s.IsMobile)
        {
            var m = World.FindMobile(s);

            if (m != null && from.CanSee(m) && Utility.InUpdateRange(from.Location, m.Location))
            {
                if (SingleClickProps)
                {
                    m.OnAosSingleClick(from);
                }
                else
                {
                    if (from.Region.OnSingleClick(from, m))
                    {
                        m.OnSingleClick(from);
                    }
                }
            }
        }
        else if (s.IsItem)
        {
            var item = World.FindItem(s);

            if (item?.Deleted == false && from.CanSee(item) &&
                Utility.InUpdateRange(from.Location, item.GetWorldLocation()))
            {
                if (SingleClickProps)
                {
                    item.OnAosSingleClick(from);
                }
                else if (from.Region.OnSingleClick(from, item))
                {
                    if (item.Parent is Item parentItem)
                    {
                        parentItem.OnSingleClickContained(from, item);
                    }

                    item.OnSingleClick(from);
                }
            }
        }
    }

    public static void BatchQueryProperties(NetState state, CircularBufferReader reader, int packetLength)
    {
        if (!ObjectPropertyList.Enabled)
        {
            return;
        }

        var from = state.Mobile;

        var length = reader.Remaining;

        if (length % 4 != 0)
        {
            return;
        }

        while (reader.Remaining > 0)
        {
            Serial s = (Serial)reader.ReadUInt32();

            if (s.IsMobile)
            {
                var m = World.FindMobile(s);

                if (m != null && from.CanSee(m) && Utility.InUpdateRange(from.Location, m.Location))
                {
                    m.SendPropertiesTo(from);
                }
            }
            else if (s.IsItem)
            {
                var item = World.FindItem(s);

                if (item?.Deleted == false && from.CanSee(item) &&
                    Utility.InUpdateRange(from.Location, item.GetWorldLocation()))
                {
                    item.SendPropertiesTo(from);
                }
            }
        }
    }
}
