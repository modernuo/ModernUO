/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.cs                                                      *
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
using System.Collections.Generic;

namespace Server.Network
{
    public static partial class Packets
    {
        private static readonly PacketHandler[] m_6017Handlers = new PacketHandler[0x100];

        private static readonly PacketHandler[] m_ExtendedHandlersLow = new PacketHandler[0x100];
        private static readonly Dictionary<int, PacketHandler> m_ExtendedHandlersHigh = new Dictionary<int, PacketHandler>();

        private static readonly EncodedPacketHandler[] m_EncodedHandlersLow = new EncodedPacketHandler[0x100];

        private static readonly Dictionary<int, EncodedPacketHandler> m_EncodedHandlersHigh =
            new Dictionary<int, EncodedPacketHandler>();

        static Packets()
        {
            Register(0x00, 104, false, CreateCharacter);
            Register(0x01, 5, false, Disconnect);
            Register(0x02, 7, true, MovementReq);
            Register(0x03, 0, true, AsciiSpeech);
            Register(0x05, 5, true, AttackReq);
            Register(0x06, 5, true, UseReq);
            Register(0x07, 7, true, LiftReq);
            Register(0x08, 14, true, DropReq);
            Register(0x09, 5, true, LookReq);
            Register(0x12, 0, true, TextCommand);
            Register(0x13, 10, true, EquipReq);
            Register(0x22, 3, true, Resynchronize);
            Register(0x2C, 2, true, DeathStatusResponse);
            Register(0x34, 10, true, MobileQuery);
            Register(0x3A, 0, true, ChangeSkillLock);
            Register(0x3B, 0, true, VendorBuyReply);
            Register(0x5D, 73, false, PlayCharacter);
            Register(0x6C, 19, true, TargetResponse);
            Register(0x6F, 0, true, SecureTrade);
            Register(0x72, 5, true, SetWarMode);
            Register(0x73, 2, false, PingReq);
            Register(0x75, 35, true, RenameRequest);
            Register(0x7D, 13, true, MenuResponse);
            Register(0x80, 62, false, AccountLogin);
            Register(0x83, 39, false, DeleteCharacter);
            Register(0x91, 65, false, GameLogin);
            Register(0x95, 9, true, HuePickerResponse);
            Register(0x98, 0, true, MobileNameRequest);
            Register(0x9A, 0, true, AsciiPromptResponse);
            Register(0x9B, 258, true, HelpRequest);
            Register(0x9F, 0, true, VendorSellReply);
            Register(0xA0, 3, false, PlayServer);
            Register(0xA4, 149, false, SystemInfo);
            Register(0xA7, 4, true, RequestScrollWindow);
            Register(0xAD, 0, true, UnicodeSpeech);
            Register(0xB1, 0, true, DisplayGumpResponse);
            Register(0xB6, 9, true, ObjectHelpRequest);
            Register(0xB8, 0, true, ProfileReq);
            Register(0xBB, 9, false, AccountID);
            Register(0xBD, 0, false, ClientVersion);
            Register(0xBE, 0, true, AssistVersion);
            Register(0xBF, 0, true, ExtendedCommand);
            Register(0xC2, 0, true, UnicodePromptResponse);
            Register(0xC8, 2, true, SetUpdateRange);
            Register(0xCF, 0, false, AccountLogin);
            Register(0xD0, 0, true, ConfigurationFile);
            Register(0xD1, 2, true, LogoutReq);
            Register(0xD6, 0, true, BatchQueryProperties);
            Register(0xD7, 0, true, EncodedCommand);
            Register(0xE1, 0, false, ClientType);
            Register(0xEF, 21, false, LoginServerSeed);
            Register(0xEC, 0, false, EquipMacro);
            Register(0xED, 0, false, UnequipMacro);
            Register(0xF4, 0, false, CrashReport);
            Register(0xF8, 106, false, CreateCharacter);
            Register(0xFB, 2, false, ShowPublicHouseContent);

            Register6017(0x08, 15, true, DropReq6017);

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

            RegisterEncoded(0x19, true, SetAbility);
            RegisterEncoded(0x28, true, GuildGumpRequest);

            RegisterEncoded(0x32, true, QuestGumpRequest);
        }

        public static PacketHandler[] Handlers { get; } = new PacketHandler[0x100];

        public static void Register(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            Handlers[packetID] = new PacketHandler(packetID, length, ingame, onReceive);
            m_6017Handlers[packetID] ??= new PacketHandler(packetID, length, ingame, onReceive);
        }

        public static PacketHandler GetHandler(int packetID) => Handlers[packetID];

        public static void Register6017(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            m_6017Handlers[packetID] = new PacketHandler(packetID, length, ingame, onReceive);
        }

        public static PacketHandler Get6017Handler(int packetID) => m_6017Handlers[packetID];

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

        public static void RegisterEncoded(int packetID, bool ingame, OnEncodedPacketReceive onReceive)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                m_EncodedHandlersLow[packetID] = new EncodedPacketHandler(packetID, ingame, onReceive);
            }
            else
            {
                m_EncodedHandlersHigh[packetID] = new EncodedPacketHandler(packetID, ingame, onReceive);
            }
        }

        public static EncodedPacketHandler GetEncodedHandler(int packetID)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                return m_EncodedHandlersLow[packetID];
            }

            m_EncodedHandlersHigh.TryGetValue(packetID, out var handler);
            return handler;
        }

        public static void RemoveEncodedHandler(int packetID)
        {
            if (packetID >= 0 && packetID < 0x100)
            {
                m_EncodedHandlersLow[packetID] = null;
            }
            else
            {
                m_EncodedHandlersHigh.Remove(packetID);
            }
        }

        public static void RegisterThrottler(int packetID, ThrottlePacketCallback t)
        {
            var ph = GetHandler(packetID);

            if (ph != null)
            {
                ph.ThrottleCallback = t;
            }

            ph = Get6017Handler(packetID);

            if (ph != null)
            {
                ph.ThrottleCallback = t;
            }
        }

        public static int ProcessPacket(this NetState ns, ArraySegment<byte>[] buffer)
        {
            var reader = new CircularBufferReader(buffer);

            var packetId = reader.ReadByte();

            if (!ns.Seeded)
            {
                if (packetId == 0xEF)
                {
                    // new packet in client 6.0.5.0 replaces the traditional seed method with a seed packet
                    // 0xEF = 239 = multicast IP, so this should never appear in a normal seed. So this is backwards compatible with older clients.
                    ns.Seeded = true;
                }
                else
                {
                    var seed = (packetId << 24) | (reader.ReadByte() << 16) | (reader.ReadByte() << 8) | reader.ReadByte();

                    if (seed == 0)
                    {
                        ns.WriteConsole("Invalid client detected, disconnecting");
                        return -1;
                    }

                    ns.m_Seed = seed;
                    ns.Seeded = true;

                    return 4;
                }
            }

            if (ns.CheckEncrypted(packetId))
            {
                return -1;
            }

            // Get Handlers
            var handler = ns.GetHandler(packetId);

            if (handler == null)
            {
                reader.Trace(ns);
                return -1;
            }

            var packetLength = handler.Length;
            if (handler.Length <= 0 && reader.Length >= 3)
            {
                packetLength = reader.ReadUInt16();
                if (packetLength < 3)
                {
                    return -1;
                }
            }

            // Not enough data, let's wait for more to come in
            if (reader.Length < packetLength)
            {
                return 0;
            }

            if (handler.Ingame && ns.Mobile?.Deleted != false)
            {
                ns.WriteConsole("Sent ingame packet (0x{1:X2}) without being attached to a valid mobile.", ns, packetId);
                return -1;
            }

            var throttled = handler.ThrottleCallback?.Invoke(ns) ?? TimeSpan.Zero;

            if (throttled > TimeSpan.Zero)
            {
                ns.ThrottledUntil = DateTime.UtcNow + throttled;
            }

            handler.OnReceive(ns, reader);

            return packetLength;
        }

        private static void UnhandledBF(NetState state, CircularBufferReader reader)
        {
        }

        public static void Empty(NetState state, CircularBufferReader reader)
        {
        }

        public static void EncodedCommand(this NetState state, CircularBufferReader reader)
        {
            var e = World.FindEntity(reader.ReadUInt32());
            int packetId = reader.ReadUInt16();

            var ph = GetEncodedHandler(packetId);

            if (ph == null)
            {
                reader.Trace(state);
                return;
            }

            if (ph.Ingame && state.Mobile == null)
            {
                state.WriteConsole(
                    "Sent ingame packet (0xD7x{0:X2}) before having been attached to a mobile",
                    packetId
                );
                state.Dispose();
            }
            else if (ph.Ingame && state.Mobile.Deleted)
            {
                state.Dispose();
            }
            else
            {
                ph.OnReceive(state, e, new EncodedReader(reader));
            }
        }

        public static void ExtendedCommand(this NetState state, CircularBufferReader reader)
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
    }
}
