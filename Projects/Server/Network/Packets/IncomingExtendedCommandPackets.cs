/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingExtendedCommandPackets.cs                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using Server.ContextMenus;

namespace Server.Network
{
    public static class IncomingExtendedCommandPackets
    {
        private static readonly PacketHandler[] m_ExtendedHandlersLow = new PacketHandler[0x100];
        private static readonly Dictionary<int, PacketHandler> m_ExtendedHandlersHigh = new Dictionary<int, PacketHandler>();

        // TODO: Change to outside configuration
        public static int[] ValidAnimations { get; set; } =
        {
            6, 21, 32, 33,
            100, 101, 102, 103,
            104, 105, 106, 107,
            108, 109, 110, 111,
            112, 113, 114, 115,
            116, 117, 118, 119,
            120, 121, 123, 124,
            125, 126, 127, 128
        };

        public static void Configure()
        {
            IncomingPackets.Register(0xBF, 0, true, ExtendedCommand);

            RegisterExtended(0x05, false, ScreenSize);
            RegisterExtended(0x06, true, PartyMessage);
            RegisterExtended(0x07, true, QuestArrow);
            RegisterExtended(0x09, true, DisarmRequest);
            RegisterExtended(0x0A, true, StunRequest);
            RegisterExtended(0x0B, false, Language);
            RegisterExtended(0x0C, true, CloseStatus);
            RegisterExtended(0x0E, true, Animate);
            RegisterExtended(0x0F, false, Empty); // What's this?
            RegisterExtended(0x10, true, QueryProperties);
            RegisterExtended(0x13, true, ContextMenuRequest);
            RegisterExtended(0x15, true, ContextMenuResponse);
            RegisterExtended(0x1A, true, StatLockChange);
            RegisterExtended(0x1C, true, CastSpell);
            RegisterExtended(0x24, false, UnhandledBF);
            RegisterExtended(0x2C, true, BandageTarget);
            RegisterExtended(0x2D, true, TargetedSpell);
            RegisterExtended(0x2E, true, TargetedSkillUse);
            RegisterExtended(0x30, true, TargetByResourceMacro);
            RegisterExtended(0x32, true, ToggleFlying);
        }

        private static void UnhandledBF(NetState state, CircularBufferReader reader)
        {
        }

        public static void Empty(NetState state, CircularBufferReader reader)
        {
        }

        public static void RegisterExtended(int packetID, bool ingame, OnPacketReceive onReceive)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                m_ExtendedHandlersLow[packetID] = new PacketHandler(packetID, 0, ingame, onReceive);
            }
            else
            {
                m_ExtendedHandlersHigh[packetID] = new PacketHandler(packetID, 0, ingame, onReceive);
            }
        }

        public static PacketHandler GetExtendedHandler(int packetID)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                return m_ExtendedHandlersLow[packetID];
            }

            m_ExtendedHandlersHigh.TryGetValue(packetID, out var handler);
            return handler;
        }

        public static void RemoveExtendedHandler(int packetID)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                m_ExtendedHandlersLow[packetID] = null;
            }
            else
            {
                m_ExtendedHandlersHigh.Remove(packetID);
            }
        }

        public static void ExtendedCommand(NetState state, CircularBufferReader reader)
        {
            int packetID = reader.ReadUInt16();

            var ph = GetExtendedHandler(packetID);

            if (ph == null)
            {
                reader.Trace(state);
                return;
            }

            if (ph.Ingame && state.Mobile?.Deleted != false)
            {
                if (state.Mobile == null)
                {
                    state.WriteConsole(
                        "Sent in-game packet (0xBFx{0:X2}) before having been attached to a mobile",
                        packetID
                    );
                }

                state.Dispose();
            }
            else
            {
                ph.OnReceive(state, reader);
            }
        }

        public static void ScreenSize(NetState state, CircularBufferReader reader)
        {
            var width = reader.ReadInt32();
            var unk = reader.ReadInt32();
        }

        // TODO: Move out of the core
        public static void PartyMessage(NetState state, CircularBufferReader reader)
        {
            if (state.Mobile == null)
            {
                return;
            }

            switch (reader.ReadByte())
            {
                case 0x01:
                    PartyMessage_AddMember(state, reader);
                    break;
                case 0x02:
                    PartyMessage_RemoveMember(state, reader);
                    break;
                case 0x03:
                    PartyMessage_PrivateMessage(state, reader);
                    break;
                case 0x04:
                    PartyMessage_PublicMessage(state, reader);
                    break;
                case 0x06:
                    PartyMessage_SetCanLoot(state, reader);
                    break;
                case 0x08:
                    PartyMessage_Accept(state, reader);
                    break;
                case 0x09:
                    PartyMessage_Decline(state, reader);
                    break;
                default:
                    reader.Trace(state);
                    break;
            }
        }

        public static void PartyMessage_AddMember(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnAdd(state.Mobile);
        }

        public static void PartyMessage_RemoveMember(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnRemove(state.Mobile, World.FindMobile(reader.ReadUInt32()));
        }

        public static void PartyMessage_PrivateMessage(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnPrivateMessage(
                state.Mobile,
                World.FindMobile(reader.ReadUInt32()),
                reader.ReadBigUniSafe()
            );
        }

        public static void PartyMessage_PublicMessage(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnPublicMessage(state.Mobile, reader.ReadBigUniSafe());
        }

        public static void PartyMessage_SetCanLoot(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnSetCanLoot(state.Mobile, reader.ReadBoolean());
        }

        public static void PartyMessage_Accept(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnAccept(state.Mobile, World.FindMobile(reader.ReadUInt32()));
        }

        public static void PartyMessage_Decline(NetState state, CircularBufferReader reader)
        {
            PartyCommands.Handler?.OnDecline(state.Mobile, World.FindMobile(reader.ReadUInt32()));
        }

        public static void QuestArrow(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var rightClick = reader.ReadBoolean();

            from.QuestArrow?.OnClick(rightClick);
        }

        public static void Animate(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var action = reader.ReadInt32();

            var ok = false;

            for (var i = 0; !ok && i < ValidAnimations.Length; ++i)
            {
                ok = action == ValidAnimations[i];
            }

            if (ok && from.Alive && from.Body.IsHuman && !from.Mounted)
            {
                from.Animate(action, 7, 1, true, false, 0);
            }
        }

        public static void CastSpell(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            Item spellbook = reader.ReadInt16() == 1 ? World.FindItem(reader.ReadUInt32()) : null;

            var spellID = reader.ReadInt16() - 1;
            EventSink.InvokeCastSpellRequest(from, spellID, spellbook);
        }

        public static void ToggleFlying(NetState state, CircularBufferReader reader)
        {
            state.Mobile?.ToggleFlying();
        }

        public static void StunRequest(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            EventSink.InvokeStunRequest(from);
        }

        public static void DisarmRequest(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            EventSink.InvokeDisarmRequest(from);
        }

        public static void StatLockChange(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            int stat = reader.ReadByte();
            int lockValue = reader.ReadByte();

            if (lockValue > 2)
            {
                lockValue = 0;
            }

            switch (stat)
            {
                case 0:
                    from.StrLock = (StatLockType)lockValue;
                    break;
                case 1:
                    from.DexLock = (StatLockType)lockValue;
                    break;
                case 2:
                    from.IntLock = (StatLockType)lockValue;
                    break;
            }
        }

        public static void CloseStatus(NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();
        }

        public static void Language(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            from.Language = reader.ReadAscii(4);
        }

        public static void QueryProperties(NetState state, CircularBufferReader reader)
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

        public static void ContextMenuResponse(NetState state, CircularBufferReader reader)
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

        public static void ContextMenuRequest(NetState state, CircularBufferReader reader)
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

        public static void BandageTarget(NetState state, CircularBufferReader reader)
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

        public static void TargetedSpell(NetState state, CircularBufferReader reader)
        {
            var spellId = (short)(reader.ReadInt16() - 1); // zero based;

            EventSink.InvokeTargetedSpell(state.Mobile, World.FindEntity(reader.ReadUInt32()), spellId);
        }

        public static void TargetedSkillUse(NetState state, CircularBufferReader reader)
        {
            var skillId = reader.ReadInt16();

            EventSink.InvokeTargetedSkillUse(state.Mobile, World.FindEntity(reader.ReadUInt32()), skillId);
        }

        public static void TargetByResourceMacro(NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();

            if (serial.IsItem)
            {
                EventSink.InvokeTargetByResourceMacro(state.Mobile, World.FindItem(serial), reader.ReadInt16());
            }
        }
    }
}
