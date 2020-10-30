/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.WorldEntities.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.ContextMenus;

namespace Server.Network
{
    public static partial class Packets
    {
        public static void ObjectHelpRequest(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            Serial serial = reader.ReadUInt32();
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

        public static void UseReq(this NetState state, CircularBufferReader reader)
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
                    Serial s = value;

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

        public static void LookReq(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            Serial s = reader.ReadUInt32();

            if (s.IsMobile)
            {
                var m = World.FindMobile(s);

                if (m != null && from.CanSee(m) && Utility.InUpdateRange(from, m))
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

        public static void BandageTarget(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            if (from.AccessLevel >= AccessLevel.Counselor || Core.TickCount - from.NextActionTime >= 0)
            {
                var bandage = World.FindItem(reader.ReadUInt32());

                if (bandage == null)
                {
                    return;
                }

                var target = World.FindMobile(reader.ReadUInt32());

                if (target == null)
                {
                    return;
                }

                EventSink.InvokeBandageTargetRequest(from, bandage, target);

                from.NextActionTime = Core.TickCount + Mobile.ActionDelay;
            }
            else
            {
                from.SendActionMessage();
            }
        }

        public static void BatchQueryProperties(this NetState state, CircularBufferReader reader)
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
                Serial s = reader.ReadUInt32();

                if (s.IsMobile)
                {
                    var m = World.FindMobile(s);

                    if (m != null && from.CanSee(m) && Utility.InUpdateRange(from, m))
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

        public static void QueryProperties(this NetState state, CircularBufferReader reader)
        {
            if (!ObjectPropertyList.Enabled)
            {
                return;
            }

            var from = state.Mobile;

            Serial s = reader.ReadUInt32();

            if (s.IsMobile)
            {
                var m = World.FindMobile(s);

                if (m != null && from.CanSee(m) && Utility.InUpdateRange(from, m))
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

        public static void ContextMenuResponse(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var menu = from.ContextMenu;

            from.ContextMenu = null;

            if (menu != null && from == menu.From)
            {
                var entity = World.FindEntity(reader.ReadUInt32());

                if (entity != null && entity == menu.Target && from.CanSee(entity))
                {
                    Point3D p;

                    if (entity is Mobile)
                    {
                        p = entity.Location;
                    }
                    else if (entity is Item item)
                    {
                        p = item.GetWorldLocation();
                    }
                    else
                    {
                        return;
                    }

                    int index = reader.ReadUInt16();

                    if (index >= 0 && index < menu.Entries.Length)
                    {
                        var e = menu.Entries[index];

                        var range = e.Range;

                        if (range == -1)
                        {
                            range = 18;
                        }

                        if (e.Enabled && from.InRange(p, range))
                        {
                            e.OnClick();
                        }
                    }
                }
            }
        }

        public static void ContextMenuRequest(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            var target = World.FindEntity(reader.ReadUInt32());

            if (from != null && target != null && from.Map == target.Map && from.CanSee(target))
            {
                var item = target as Item;

                var checkLocation = item?.GetWorldLocation() ?? target.Location;
                if (!(Utility.InUpdateRange(from.Location, checkLocation) && from.CheckContextMenuDisplay(target)))
                {
                    return;
                }

                var c = new ContextMenu(from, target);

                if (c.Entries.Length > 0)
                {
                    if (item?.RootParent is Mobile mobile && mobile != from && mobile.AccessLevel >= from.AccessLevel)
                    {
                        for (var i = 0; i < c.Entries.Length; ++i)
                        {
                            if (!c.Entries[i].NonLocalUse)
                            {
                                c.Entries[i].Enabled = false;
                            }
                        }
                    }

                    from.ContextMenu = c;
                }
            }
        }
    }
}
