/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: StaticPacketHandlers.cs                                         *
 * Created: 2019/03/15 - Updated: 2019/12/24                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Concurrent;

namespace Server.Network
{
    public static class StaticPacketHandlers
    {
        private static readonly ConcurrentDictionary<IPropertyListObject, OPLInfo> OPLInfoPackets =
            new ConcurrentDictionary<IPropertyListObject, OPLInfo>();

        private static readonly ConcurrentDictionary<IEntity, RemoveEntity> RemoveEntityPackets =
            new ConcurrentDictionary<IEntity, RemoveEntity>();

        private static readonly ConcurrentDictionary<Item, WorldItem> WorldItemPackets =
            new ConcurrentDictionary<Item, WorldItem>();

        private static readonly ConcurrentDictionary<Item, WorldItemSA> WorldItemSAPackets =
            new ConcurrentDictionary<Item, WorldItemSA>();

        private static readonly ConcurrentDictionary<Item, WorldItemHS> WorldItemHSPackets =
            new ConcurrentDictionary<Item, WorldItemHS>();

        public static OPLInfo GetOPLInfoPacket(IPropertyListObject obj)
        {
            return OPLInfoPackets.GetOrAdd(
                obj,
                value =>
                {
                    var packet = new OPLInfo(value.PropertyList.Entity.Serial, value.PropertyList.Hash);
                    packet.SetStatic();
                    return packet;
                }
            );
        }

        public static OPLInfo FreeOPLInfoPacket(IPropertyListObject obj)
        {
            if (OPLInfoPackets.TryRemove(obj, out var p))
                Packet.Release(p);

            return p;
        }

        public static RemoveEntity GetRemoveEntityPacket(IEntity entity)
        {
            return RemoveEntityPackets.GetOrAdd(
                entity,
                value =>
                {
                    var packet = new RemoveEntity(value.Serial);
                    packet.SetStatic();
                    return packet;
                }
            );
        }

        public static void FreeRemoveItemPacket(IEntity entity)
        {
            if (RemoveEntityPackets.TryRemove(entity, out var p))
                Packet.Release(p);
        }

        public static WorldItem GetWorldItemPacket(Item item)
        {
            return WorldItemPackets.GetOrAdd(
                item,
                value =>
                {
                    var packet = new WorldItem(value);
                    packet.SetStatic();
                    return packet;
                }
            );
        }

        public static WorldItemSA GetWorldItemSAPacket(Item item)
        {
            return WorldItemSAPackets.GetOrAdd(
                item,
                value =>
                {
                    var packet = new WorldItemSA(value);
                    packet.SetStatic();
                    return packet;
                }
            );
        }

        public static WorldItemHS GetWorldItemHSPacket(Item item)
        {
            return WorldItemHSPackets.GetOrAdd(
                item,
                value =>
                {
                    var packet = new WorldItemHS(value);
                    packet.SetStatic();
                    return packet;
                }
            );
        }

        public static void FreeWorldItemPackets(Item item)
        {
            if (WorldItemPackets.TryRemove(item, out var wi))
                Packet.Release(wi);

            if (WorldItemSAPackets.TryRemove(item, out var wisa))
                Packet.Release(wisa);

            if (WorldItemHSPackets.TryRemove(item, out var wihs))
                Packet.Release(wihs);
        }
    }
}
