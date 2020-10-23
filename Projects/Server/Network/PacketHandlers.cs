/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PacketHandlers.cs                                               *
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
using System.IO;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Targeting;
using CV = Server.ClientVersion;

namespace Server.Network
{
    [Flags]
    public enum MessageType
    {
        Regular = 0x00,
        System = 0x01,
        Emote = 0x02,
        Label = 0x06,
        Focus = 0x07,
        Whisper = 0x08,
        Yell = 0x09,
        Spell = 0x0A,

        Guild = 0x0D,
        Alliance = 0x0E,
        Command = 0x0F,

        Encoded = 0xC0
    }

    public static class PacketHandlers
    {
        public delegate void PlayCharCallback(NetState state, bool val);

        private const int m_AuthIDWindowSize = 128;
        private static readonly PacketHandler[] m_6017Handlers = new PacketHandler[0x100];

        private static readonly PacketHandler[] m_ExtendedHandlersLow = new PacketHandler[0x100];
        private static readonly Dictionary<int, PacketHandler> m_ExtendedHandlersHigh = new Dictionary<int, PacketHandler>();

        private static readonly EncodedPacketHandler[] m_EncodedHandlersLow = new EncodedPacketHandler[0x100];

        private static readonly Dictionary<int, EncodedPacketHandler> m_EncodedHandlersHigh =
            new Dictionary<int, EncodedPacketHandler>();

        private static readonly int[] m_EmptyInts = Array.Empty<int>();

        private static readonly KeywordList m_KeywordList = new KeywordList();

        private static readonly Dictionary<int, AuthIDPersistence> m_AuthIDWindow =
            new Dictionary<int, AuthIDPersistence>(m_AuthIDWindowSize);

        static PacketHandlers()
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
            Register(0xF8, 106, false, CreateCharacter70160);
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

        public static PlayCharCallback ThirdPartyAuthCallback { get; set; }
        public static PlayCharCallback ThirdPartyHackedCallback { get; set; }

        public static PacketHandler[] Handlers { get; } = new PacketHandler[0x100];

        public static bool SingleClickProps { get; set; }

        // TODO: Change to outside configuration
        public static int[] ValidAnimations { get; set; } =
        {
            6, 21, 32, 33,
            100, 101, 102,
            103, 104, 105,
            106, 107, 108,
            109, 110, 111,
            112, 113, 114,
            115, 116, 117,
            118, 119, 120,
            121, 123, 124,
            125, 126, 127,
            128
        };

        public static bool ClientVerification { get; set; } = true;

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

        public static int ProcessPacket(NetState ns, ArraySegment<byte>[] segments)
        {
            var reader = new CircularBufferReader(segments);

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

        public static void SetAbility(NetState state, IEntity e, EncodedReader reader)
        {
            EventSink.InvokeSetAbility(state.Mobile, reader.ReadInt32());
        }

        public static void GuildGumpRequest(NetState state, IEntity e, EncodedReader reader)
        {
            EventSink.InvokeGuildGumpRequest(state.Mobile);
        }

        public static void QuestGumpRequest(NetState state, IEntity e, EncodedReader reader)
        {
            EventSink.InvokeQuestGumpRequest(state.Mobile);
        }

        public static void EncodedCommand(NetState state, CircularBufferReader reader)
        {
            var e = World.FindEntity(reader.ReadUInt32());
            int packetId = reader.ReadUInt16();

            var ph = GetEncodedHandler(packetId);

            if (ph != null)
            {
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
            else
            {
                reader.Trace(state);
            }
        }

        public static void RenameRequest(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            var targ = World.FindMobile(reader.ReadUInt32());

            if (targ != null)
            {
                EventSink.InvokeRenameRequest(from, targ, reader.ReadAsciiSafe());
            }
        }

        public static void SecureTrade(NetState state, CircularBufferReader reader)
        {
            switch (reader.ReadByte())
            {
                case 1: // Cancel
                    {
                        Serial serial = reader.ReadUInt32();

                        if (World.FindItem(serial) is SecureTradeContainer cont && cont.Trade != null &&
                            (cont.Trade.From.Mobile == state.Mobile || cont.Trade.To.Mobile == state.Mobile))
                        {
                            cont.Trade.Cancel();
                        }

                        break;
                    }
                case 2: // Check
                    {
                        Serial serial = reader.ReadUInt32();

                        if (World.FindItem(serial) is SecureTradeContainer cont)
                        {
                            var trade = cont.Trade;

                            var value = reader.ReadInt32() != 0;

                            if (trade != null && trade.From.Mobile == state.Mobile)
                            {
                                trade.From.Accepted = value;
                                trade.Update();
                            }
                            else if (trade != null && trade.To.Mobile == state.Mobile)
                            {
                                trade.To.Accepted = value;
                                trade.Update();
                            }
                        }

                        break;
                    }
                case 3: // Update Gold
                    {
                        Serial serial = reader.ReadUInt32();

                        if (World.FindItem(serial) is SecureTradeContainer cont)
                        {
                            var gold = reader.ReadInt32();
                            var plat = reader.ReadInt32();

                            var trade = cont.Trade;

                            if (trade != null)
                            {
                                if (trade.From.Mobile == state.Mobile)
                                {
                                    trade.From.Gold = gold;
                                    trade.From.Plat = plat;
                                    trade.UpdateFromCurrency();
                                }
                                else if (trade.To.Mobile == state.Mobile)
                                {
                                    trade.To.Gold = gold;
                                    trade.To.Plat = plat;
                                    trade.UpdateToCurrency();
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public static void VendorBuyReply(NetState state, CircularBufferReader reader)
        {
            var vendor = World.FindMobile(reader.ReadUInt32());
            var flag = reader.ReadByte();

            if (vendor == null)
            {
                return;
            }

            if (vendor.Deleted || !Utility.RangeCheck(vendor.Location, state.Mobile.Location, 10))
            {
                state.Send(new EndVendorBuy(vendor));
                return;
            }

            if (flag == 0x02)
            {
                var msgSize = reader.Remaining;

                if (msgSize / 7 > 100)
                {
                    return;
                }

                var buyList = new List<BuyItemResponse>(msgSize / 7);
                while (msgSize > 0)
                {
                    var layer = reader.ReadByte();
                    Serial serial = reader.ReadUInt32();
                    int amount = reader.ReadInt16();

                    buyList.Add(new BuyItemResponse(serial, amount));
                    msgSize -= 7;
                }

                if (buyList.Count > 0 && vendor is IVendor v && v.OnBuyItems(state.Mobile, buyList))
                {
                    state.Send(new EndVendorBuy(vendor));
                }
            }
            else
            {
                state.Send(new EndVendorBuy(vendor));
            }
        }

        public static void VendorSellReply(NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();
            var vendor = World.FindMobile(serial);

            if (vendor == null)
            {
                return;
            }

            if (vendor.Deleted || !Utility.RangeCheck(vendor.Location, state.Mobile.Location, 10))
            {
                state.Send(new EndVendorSell(vendor));
                return;
            }

            int count = reader.ReadUInt16();

            if (count >= 100 || reader.Remaining != count * 6)
            {
                return;
            }

            var sellList = new List<SellItemResponse>(count);

            for (var i = 0; i < count; i++)
            {
                var item = World.FindItem(reader.ReadUInt32());
                int amount = reader.ReadInt16();

                if (item != null && amount > 0)
                {
                    sellList.Add(new SellItemResponse(item, amount));
                }
            }

            if (sellList.Count > 0 && vendor is IVendor v && v.OnSellItems(state.Mobile, sellList))
            {
                state.Send(new EndVendorSell(vendor));
            }
        }

        public static void DeleteCharacter(NetState state, CircularBufferReader reader)
        {
            reader.Seek(30, SeekOrigin.Current);
            var index = reader.ReadInt32();

            EventSink.InvokeDeleteRequest(state, index);
        }

        public static void DeathStatusResponse(NetState state, CircularBufferReader reader)
        {
            // Ignored
        }

        public static void ObjectHelpRequest(NetState state, CircularBufferReader reader)
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

        public static void MobileNameRequest(NetState state, CircularBufferReader reader)
        {
            var m = World.FindMobile(reader.ReadUInt32());

            if (m != null && Utility.InUpdateRange(state.Mobile, m) && state.Mobile.CanSee(m))
            {
                state.Send(new MobileName(m));
            }
        }

        public static void RequestScrollWindow(NetState state, CircularBufferReader reader)
        {
            int lastTip = reader.ReadInt16();
            int type = reader.ReadByte();
        }

        public static void AttackReq(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            var m = World.FindMobile(reader.ReadUInt32());

            if (m != null)
            {
                from.Attack(m);
            }
        }

        public static void HuePickerResponse(NetState state, CircularBufferReader reader)
        {
            var serial = reader.ReadUInt32();
            _ = reader.ReadInt16(); // Item ID
            var hue = reader.ReadInt16() & 0x3FFF;

            hue = Utility.ClipDyedHue(hue);

            foreach (var huePicker in state.HuePickers)
            {
                if (huePicker.Serial == serial)
                {
                    state.RemoveHuePicker(huePicker);

                    huePicker.OnResponse(hue);

                    break;
                }
            }
        }

        public static void SystemInfo(NetState state, CircularBufferReader reader)
        {
            int v1 = reader.ReadByte();
            int v2 = reader.ReadUInt16();
            int v3 = reader.ReadByte();
            var s1 = reader.ReadAscii(32);
            var s2 = reader.ReadAscii(32);
            var s3 = reader.ReadAscii(32);
            var s4 = reader.ReadAscii(32);
            int v4 = reader.ReadUInt16();
            int v5 = reader.ReadUInt16();
            var v6 = reader.ReadInt32();
            var v7 = reader.ReadInt32();
            var v8 = reader.ReadInt32();
        }

        public static void AccountID(NetState state, CircularBufferReader reader)
        {
        }

        public static void TextCommand(NetState state, CircularBufferReader reader)
        {
            int type = reader.ReadByte();
            var command = reader.ReadAscii();

            var m = state.Mobile;

            switch (type)
            {
                case 0xC7: // Animate
                    {
                        EventSink.InvokeAnimateRequest(m, command);

                        break;
                    }
                case 0x24: // Use skill
                    {
                        if (!int.TryParse(command.Split(' ')[0], out var skillIndex))
                        {
                            break;
                        }

                        Skills.UseSkill(m, skillIndex);

                        break;
                    }
                case 0x43: // Open spellbook
                    {
                        if (!int.TryParse(command, out var booktype))
                        {
                            booktype = 1;
                        }

                        EventSink.InvokeOpenSpellbookRequest(m, booktype);

                        break;
                    }
                case 0x27: // Cast spell from book
                    {
                        var split = command.Split(' ');

                        if (split.Length > 0)
                        {
                            var spellID = Utility.ToInt32(split[0]) - 1;
                            var serial = split.Length > 1 ? Utility.ToUInt32(split[1]) : (uint)Serial.MinusOne;

                            EventSink.InvokeCastSpellRequest(m, spellID, World.FindItem(serial));
                        }

                        break;
                    }
                case 0x58: // Open door
                    {
                        EventSink.InvokeOpenDoorMacroUsed(m);

                        break;
                    }
                case 0x56: // Cast spell from macro
                    {
                        var spellID = Utility.ToInt32(command) - 1;

                        EventSink.InvokeCastSpellRequest(m, spellID, null);

                        break;
                    }
                case 0xF4: // Invoke virtues from macro
                    {
                        var virtueID = Utility.ToInt32(command) - 1;

                        EventSink.InvokeVirtueMacroRequest(m, virtueID);

                        break;
                    }
                case 0x2F: // Old scroll double click
                    {
                        /*
                         * This command is still sent for items 0xEF3 - 0xEF9
                         *
                         * Command is one of three, depending on the item ID of the scroll:
                         * - [scroll serial]
                         * - [scroll serial] [target serial]
                         * - [scroll serial] [x] [y] [z]
                         */
                        break;
                    }
                default:
                    {
                        state.WriteConsole("Unknown text-command type 0x{0:X2}: {1}", state, type, command);
                        break;
                    }
            }
        }

        public static void AsciiPromptResponse(NetState state, CircularBufferReader reader)
        {
            var serial = reader.ReadUInt32();
            var prompt = reader.ReadInt32();
            var type = reader.ReadInt32();
            var text = reader.ReadAsciiSafe();

            if (text.Length > 128)
            {
                return;
            }

            var from = state.Mobile;
            var p = from.Prompt;

            if (p != null && p.Serial == serial && p.Serial == prompt)
            {
                from.Prompt = null;

                if (type == 0)
                {
                    p.OnCancel(from);
                }
                else
                {
                    p.OnResponse(from, text);
                }
            }
        }

        public static void UnicodePromptResponse(NetState state, CircularBufferReader reader)
        {
            var serial = reader.ReadUInt32();
            var prompt = reader.ReadInt32();
            var type = reader.ReadInt32();
            var lang = reader.ReadAscii(4);
            var text = reader.ReadLittleUniSafe();

            if (text.Length > 128)
            {
                return;
            }

            var from = state.Mobile;
            var p = from.Prompt;

            if (p != null && p.Serial == serial && p.Serial == prompt)
            {
                from.Prompt = null;

                if (type == 0)
                {
                    p.OnCancel(from);
                }
                else
                {
                    p.OnResponse(from, text);
                }
            }
        }

        public static void MenuResponse(NetState state, CircularBufferReader reader)
        {
            var serial = reader.ReadUInt32();
            int menuID = reader.ReadInt16(); // unused in our implementation
            int index = reader.ReadInt16();
            int itemID = reader.ReadInt16();
            int hue = reader.ReadInt16();

            index -= 1; // convert from 1-based to 0-based

            foreach (var menu in state.Menus)
            {
                if (menu.Serial == serial)
                {
                    state.RemoveMenu(menu);

                    if (index >= 0 && index < menu.EntryLength)
                    {
                        menu.OnResponse(state, index);
                    }
                    else
                    {
                        menu.OnCancel(state);
                    }

                    break;
                }
            }
        }

        public static void ProfileReq(NetState state, CircularBufferReader reader)
        {
            int type = reader.ReadByte();
            Serial serial = reader.ReadUInt32();

            var beholder = state.Mobile;
            var beheld = World.FindMobile(serial);

            if (beheld == null)
            {
                return;
            }

            switch (type)
            {
                case 0x00: // display request
                    {
                        EventSink.InvokeProfileRequest(beholder, beheld);

                        break;
                    }
                case 0x01: // edit request
                    {
                        reader.ReadInt16(); // Skip
                        int length = reader.ReadUInt16();

                        if (length > 511)
                        {
                            return;
                        }

                        var text = reader.ReadBigUni(length);

                        EventSink.InvokeChangeProfileRequest(beholder, beheld, text);

                        break;
                    }
            }
        }

        public static void Disconnect(NetState state, CircularBufferReader reader)
        {
            var minusOne = reader.ReadInt32();
        }

        public static void LiftReq(NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();
            int amount = reader.ReadUInt16();
            var item = World.FindItem(serial);

            state.Mobile.Lift(item, amount, out var rejected, out var reject);
        }

        public static void EquipReq(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            var item = from.Holding;

            var valid = item != null && item.HeldBy == from && item.Map == Map.Internal;

            from.Holding = null;

            if (!valid)
            {
                return;
            }

            reader.Seek(5, SeekOrigin.Current);
            var to = World.FindMobile(reader.ReadUInt32()) ?? from;

            if (!to.AllowEquipFrom(from) || !to.EquipItem(item))
            {
                item.Bounce(from);
            }

            item.ClearBounce();
        }

        public static void DropReq(NetState state, CircularBufferReader reader)
        {
            reader.ReadInt32(); // serial, ignored
            int x = reader.ReadInt16();
            int y = reader.ReadInt16();
            int z = reader.ReadSByte();
            Serial dest = reader.ReadUInt32();

            var loc = new Point3D(x, y, z);

            var from = state.Mobile;

            if (dest.IsMobile)
            {
                from.Drop(World.FindMobile(dest), loc);
            }
            else if (dest.IsItem)
            {
                var item = World.FindItem(dest);

                if (item is BaseMulti multi && multi.AllowsRelativeDrop)
                {
                    loc.m_X += multi.X;
                    loc.m_Y += multi.Y;
                    from.Drop(loc);
                }
                else
                {
                    from.Drop(item, loc);
                }
            }
            else
            {
                from.Drop(loc);
            }
        }

        public static void DropReq6017(NetState state, CircularBufferReader reader)
        {
            reader.ReadInt32(); // serial, ignored
            int x = reader.ReadInt16();
            int y = reader.ReadInt16();
            int z = reader.ReadSByte();
            reader.ReadByte(); // Grid Location?
            Serial dest = reader.ReadUInt32();

            var loc = new Point3D(x, y, z);

            var from = state.Mobile;

            if (dest.IsMobile)
            {
                from.Drop(World.FindMobile(dest), loc);
            }
            else if (dest.IsItem)
            {
                var item = World.FindItem(dest);

                if (item is BaseMulti multi && multi.AllowsRelativeDrop)
                {
                    loc.m_X += multi.X;
                    loc.m_Y += multi.Y;
                    from.Drop(loc);
                }
                else
                {
                    from.Drop(item, loc);
                }
            }
            else
            {
                from.Drop(loc);
            }
        }

        public static void ConfigurationFile(NetState state, CircularBufferReader reader)
        {
        }

        public static void LogoutReq(NetState state, CircularBufferReader reader)
        {
            state.Send(new LogoutAck());
        }

        public static void ChangeSkillLock(NetState state, CircularBufferReader reader)
        {
            var s = state.Mobile.Skills[reader.ReadInt16()];

            s?.SetLockNoRelay((SkillLock)reader.ReadByte());
        }

        public static void HelpRequest(NetState state, CircularBufferReader reader)
        {
            EventSink.InvokeHelpRequest(state.Mobile);
        }

        public static void TargetResponse(NetState state, CircularBufferReader reader)
        {
            int type = reader.ReadByte();
            var targetID = reader.ReadInt32();
            int flags = reader.ReadByte();
            Serial serial = reader.ReadUInt32();
            int x = reader.ReadInt16(), y = reader.ReadInt16(), z = reader.ReadInt16();
            int graphic = reader.ReadUInt16();

            if (targetID == unchecked((int)0xDEADBEEF))
            {
                return;
            }

            var from = state.Mobile;

            var t = from.Target;

            if (t == null)
            {
                return;
            }

            var prof = TargetProfile.Acquire(t.GetType());
            prof?.Start();

            try
            {
                if (x == -1 && y == -1 && !serial.IsValid)
                {
                    // User pressed escape
                    t.Cancel(from, TargetCancelType.Canceled);
                }
                else if (t.TargetID != targetID)
                {
                    // Sanity, prevent fake target
                }
                else
                {
                    object toTarget;

                    if (type == 1)
                    {
                        if (graphic == 0)
                        {
                            toTarget = new LandTarget(new Point3D(x, y, z), from.Map);
                        }
                        else
                        {
                            var map = from.Map;

                            if (map == null || map == Map.Internal)
                            {
                                t.Cancel(from, TargetCancelType.Canceled);
                                return;
                            }
                            else
                            {
                                var tiles = map.Tiles.GetStaticTiles(x, y, !t.DisallowMultis);

                                var valid = false;

                                if (state.HighSeas)
                                {
                                    var id = TileData.ItemTable[graphic & TileData.MaxItemValue];
                                    if (id.Surface)
                                    {
                                        z -= id.Height;
                                    }
                                }

                                for (var i = 0; !valid && i < tiles.Length; ++i)
                                {
                                    if (tiles[i].Z == z && tiles[i].ID == graphic)
                                    {
                                        valid = true;
                                    }
                                }

                                if (!valid)
                                {
                                    t.Cancel(from, TargetCancelType.Canceled);
                                    return;
                                }
                                else
                                {
                                    toTarget = new StaticTarget(new Point3D(x, y, z), graphic);
                                }
                            }
                        }
                    }
                    else if (serial.IsMobile)
                    {
                        toTarget = World.FindMobile(serial);
                    }
                    else if (serial.IsItem)
                    {
                        toTarget = World.FindItem(serial);
                    }
                    else
                    {
                        t.Cancel(from, TargetCancelType.Canceled);
                        return;
                    }

                    t.Invoke(from, toTarget);
                }
            }
            finally
            {
                prof?.Finish();
            }
        }

        public static void DisplayGumpResponse(NetState state, CircularBufferReader reader)
        {
            var serial = reader.ReadUInt32();
            var typeID = reader.ReadInt32();
            var buttonID = reader.ReadInt32();

            foreach (var gump in state.Gumps)
            {
                if (gump.Serial != serial || gump.TypeID != typeID)
                {
                    continue;
                }

                var buttonExists = buttonID == 0; // 0 is always 'close'

                if (!buttonExists)
                {
                    foreach (var e in gump.Entries)
                    {
                        if (e is GumpButton button && button.ButtonID == buttonID)
                        {
                            buttonExists = true;
                            break;
                        }

                        if (e is GumpImageTileButton tileButton && tileButton.ButtonID == buttonID)
                        {
                            buttonExists = true;
                            break;
                        }
                    }
                }

                if (!buttonExists)
                {
                    state.WriteConsole("Invalid gump response, disconnecting...");
                    state.Dispose();
                    return;
                }

                var switchCount = reader.ReadInt32();

                if (switchCount < 0 || switchCount > gump.m_Switches)
                {
                    state.WriteConsole("Invalid gump response, disconnecting...");
                    state.Dispose();
                    return;
                }

                var switches = new int[switchCount];

                for (var j = 0; j < switches.Length; ++j)
                {
                    switches[j] = reader.ReadInt32();
                }

                var textCount = reader.ReadInt32();

                if (textCount < 0 || textCount > gump.m_TextEntries)
                {
                    state.WriteConsole("Invalid gump response, disconnecting...");
                    state.Dispose();
                    return;
                }

                var textEntries = new TextRelay[textCount];

                for (var j = 0; j < textEntries.Length; ++j)
                {
                    int entryID = reader.ReadUInt16();
                    int textLength = reader.ReadUInt16();

                    if (textLength > 239)
                    {
                        state.WriteConsole("Invalid gump response, disconnecting...");
                        state.Dispose();
                        return;
                    }

                    var text = reader.ReadBigUniSafe(textLength);
                    textEntries[j] = new TextRelay(entryID, text);
                }

                state.RemoveGump(gump);

                var prof = GumpProfile.Acquire(gump.GetType());

                prof?.Start();

                gump.OnResponse(state, new RelayInfo(buttonID, switches, textEntries));

                prof?.Finish();

                return;
            }

            if (typeID == 461)
            {
                // Virtue gump
                var switchCount = reader.ReadInt32();

                if (buttonID == 1 && switchCount > 0)
                {
                    var beheld = World.FindMobile(reader.ReadUInt32());

                    if (beheld != null)
                    {
                        EventSink.InvokeVirtueGumpRequest(state.Mobile, beheld);
                    }
                }
                else
                {
                    var beheld = World.FindMobile(serial);

                    if (beheld != null)
                    {
                        EventSink.InvokeVirtueItemRequest(state.Mobile, beheld, buttonID);
                    }
                }
            }
        }

        public static void SetWarMode(NetState state, CircularBufferReader reader)
        {
            state.Mobile.DelayChangeWarmode(reader.ReadBoolean());
        }

        public static void Resynchronize(NetState state, CircularBufferReader reader)
        {
            var m = state.Mobile;

            if (state.StygianAbyss)
            {
                state.Send(new MobileUpdate(m));
            }
            else
            {
                state.Send(new MobileUpdateOld(m));
            }

            state.Send(MobileIncoming.Create(state, m, m));

            m.SendEverything();

            state.Sequence = 0;

            m.ClearFastwalkStack();
        }

        public static void AsciiSpeech(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            var type = (MessageType)reader.ReadByte();
            int hue = reader.ReadInt16();
            reader.ReadInt16(); // font
            var text = reader.ReadAsciiSafe().Trim();

            if (text.Length <= 0 || text.Length > 128)
            {
                return;
            }

            if (!Enum.IsDefined(typeof(MessageType), type))
            {
                type = MessageType.Regular;
            }

            from.DoSpeech(text, m_EmptyInts, type, Utility.ClipDyedHue(hue));
        }

        public static void UnicodeSpeech(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            var type = (MessageType)reader.ReadByte();
            int hue = reader.ReadInt16();
            reader.ReadInt16(); // font
            var lang = reader.ReadAscii(4);
            string text;

            var isEncoded = (type & MessageType.Encoded) != 0;
            int[] keywords;

            if (isEncoded)
            {
                int value = reader.ReadInt16();
                var count = (value & 0xFFF0) >> 4;
                var hold = value & 0xF;

                if (count < 0 || count > 50)
                {
                    return;
                }

                var keyList = m_KeywordList;

                for (var i = 0; i < count; ++i)
                {
                    int speechID;

                    if ((i & 1) == 0)
                    {
                        hold <<= 8;
                        hold |= reader.ReadByte();
                        speechID = hold;
                        hold = 0;
                    }
                    else
                    {
                        value = reader.ReadInt16();
                        speechID = (value & 0xFFF0) >> 4;
                        hold = value & 0xF;
                    }

                    if (!keyList.Contains(speechID))
                    {
                        keyList.Add(speechID);
                    }
                }

                text = reader.ReadUTF8Safe();

                keywords = keyList.ToArray();
            }
            else
            {
                text = reader.ReadBigUniSafe();

                keywords = m_EmptyInts;
            }

            text = text.Trim();

            if (text.Length <= 0 || text.Length > 128)
            {
                return;
            }

            type &= ~MessageType.Encoded;

            if (!Enum.IsDefined(typeof(MessageType), type))
            {
                type = MessageType.Regular;
            }

            from.Language = lang;
            from.DoSpeech(text, keywords, type, Utility.ClipDyedHue(hue));
        }

        public static void UseReq(NetState state, CircularBufferReader reader)
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

        public static void LookReq(NetState state, CircularBufferReader reader)
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

        public static void PingReq(NetState state, CircularBufferReader reader)
        {
            state.Send(PingAck.Instantiate(reader.ReadByte()));
        }

        public static void SetUpdateRange(NetState state, CircularBufferReader reader)
        {
            state.Send(ChangeUpdateRange.Instantiate(18));
        }

        public static void MovementReq(NetState state, CircularBufferReader reader)
        {
            var dir = (Direction)reader.ReadByte();
            int seq = reader.ReadByte();
            var key = reader.ReadInt32();

            var m = state.Mobile;

            if (state.Sequence == 0 && seq != 0 || !m.Move(dir))
            {
                state.Send(new MovementRej(seq, m));
                state.Sequence = 0;

                m.ClearFastwalkStack();
            }
            else
            {
                ++seq;

                if (seq == 256)
                {
                    seq = 1;
                }

                state.Sequence = seq;
            }
        }

        public static void Animate(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            var action = reader.ReadInt32();

            var ok = false;

            for (var i = 0; !ok && i < ValidAnimations.Length; ++i)
            {
                ok = action == ValidAnimations[i];
            }

            if (from != null && ok && from.Alive && from.Body.IsHuman && !from.Mounted)
            {
                from.Animate(action, 7, 1, true, false, 0);
            }
        }

        public static void QuestArrow(NetState state, CircularBufferReader reader)
        {
            var rightClick = reader.ReadBoolean();
            var from = state.Mobile;

            from?.QuestArrow?.OnClick(rightClick);
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

        public static void CastSpell(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            Item spellbook = null;

            if (reader.ReadInt16() == 1)
            {
                spellbook = World.FindItem(reader.ReadUInt32());
            }

            var spellID = reader.ReadInt16() - 1;

            EventSink.InvokeCastSpellRequest(from, spellID, spellbook);
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

        public static void ToggleFlying(NetState state, CircularBufferReader reader)
        {
            state.Mobile.ToggleFlying();
        }

        public static void BatchQueryProperties(NetState state, CircularBufferReader reader)
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

        public static void StunRequest(NetState state, CircularBufferReader reader)
        {
            EventSink.InvokeStunRequest(state.Mobile);
        }

        public static void DisarmRequest(NetState state, CircularBufferReader reader)
        {
            EventSink.InvokeDisarmRequest(state.Mobile);
        }

        public static void StatLockChange(NetState state, CircularBufferReader reader)
        {
            int stat = reader.ReadByte();
            int lockValue = reader.ReadByte();

            if (lockValue > 2)
            {
                lockValue = 0;
            }

            var m = state.Mobile;

            if (m != null)
            {
                switch (stat)
                {
                    case 0:
                        m.StrLock = (StatLockType)lockValue;
                        break;
                    case 1:
                        m.DexLock = (StatLockType)lockValue;
                        break;
                    case 2:
                        m.IntLock = (StatLockType)lockValue;
                        break;
                }
            }
        }

        public static void ScreenSize(NetState state, CircularBufferReader reader)
        {
            var width = reader.ReadInt32();
            var unk = reader.ReadInt32();
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
                if (target is Mobile && !Utility.InUpdateRange(from.Location, target.Location))
                {
                    return;
                }

                var item = target as Item;

                if (item != null && !Utility.InUpdateRange(from.Location, item.GetWorldLocation()))
                {
                    return;
                }

                if (!from.CheckContextMenuDisplay(target))
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

        public static void CloseStatus(NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();
        }

        public static void Language(NetState state, CircularBufferReader reader)
        {
            var lang = reader.ReadAscii(4);

            if (state.Mobile != null)
            {
                state.Mobile.Language = lang;
            }
        }

        public static void AssistVersion(NetState state, CircularBufferReader reader)
        {
            var unk = reader.ReadInt32();
            var av = reader.ReadAscii();
        }

        public static void ClientVersion(NetState state, CircularBufferReader reader)
        {
            var version = state.Version = new CV(reader.ReadAscii());

            EventSink.InvokeClientVersionReceived(state, version);
        }

        public static void ClientType(NetState state, CircularBufferReader reader)
        {
            reader.ReadUInt16();

            int type = reader.ReadUInt16();
            var version = state.Version = new CV(reader.ReadAscii());

            EventSink.InvokeClientVersionReceived(state, version);
        }

        public static void MobileQuery(NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            reader.ReadInt32(); // 0xEDEDEDED
            int type = reader.ReadByte();
            var m = World.FindMobile(reader.ReadUInt32());

            if (m != null)
            {
                switch (type)
                {
                    case 0x04: // Stats
                        {
                            m.OnStatsQuery(from);
                            break;
                        }
                    case 0x05:
                        {
                            m.OnSkillsQuery(from);
                            break;
                        }
                    default:
                        {
                            reader.Trace(state);
                            break;
                        }
                }
            }
        }

        public static void PlayCharacter(NetState state, CircularBufferReader reader)
        {
            reader.ReadInt32(); // 0xEDEDEDED

            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);

            var flags = reader.ReadInt32();

            reader.Seek(24, SeekOrigin.Current);

            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();

            var a = state.Account;

            if (a == null || charSlot < 0 || charSlot >= a.Length)
            {
                state.Dispose();
            }
            else
            {
                var m = a[charSlot];

                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal && check != m)
                    {
                        state.WriteConsole("Account in use");
                        state.Send(new PopupMessage(PMMessage.CharInWorld));
                        return;
                    }
                }

                if (m == null)
                {
                    state.Dispose();
                    return;
                }

                m.NetState?.Dispose();

                // TODO: Make this wait one tick so we don't have to call it unnecessarily
                NetState.ProcessDisposedQueue();

                state.Send(new ClientVersionReq());

                state.BlockAllPackets = true;

                state.Flags = (ClientFlags)flags;

                state.Mobile = m;
                m.NetState = state;

                new LoginTimer(state, m).Start();
            }
        }

        public static void ShowPublicHouseContent(NetState state, CircularBufferReader reader)
        {
            var showPublicHouseContent = reader.ReadBoolean();
        }

        public static void DoLogin(NetState state, Mobile m)
        {
            state.Send(new LoginConfirm(m));

            if (m.Map != null)
            {
                state.Send(new MapChange(m.Map));
            }

            if (!Core.SE && state.ProtocolChanges < ProtocolChanges.Version6000)
            {
                state.Send(new MapPatches());
            }

            state.Send(SeasonChange.Instantiate(m.GetSeason(), true));

            state.Send(SupportedFeatures.Instantiate(state));

            state.Sequence = 0;

            if (state.NewMobileIncoming)
            {
                state.Send(new MobileUpdate(m));
                state.Send(new MobileUpdate(m));

                m.CheckLightLevels(true);

                state.Send(new MobileUpdate(m));

                state.Send(new MobileIncoming(m, m));
                // state.Send( new MobileAttributes( m ) );
                state.Send(new MobileStatus(m, m));
                state.Send(Network.SetWarMode.Instantiate(m.Warmode));

                m.SendEverything();

                state.Send(SupportedFeatures.Instantiate(state));
                state.Send(new MobileUpdate(m));
                // state.Send( new MobileAttributes( m ) );
                state.Send(new MobileStatus(m, m));
                state.Send(Network.SetWarMode.Instantiate(m.Warmode));
                state.Send(new MobileIncoming(m, m));
            }
            else if (state.StygianAbyss)
            {
                state.Send(new MobileUpdate(m));
                state.Send(new MobileUpdate(m));

                m.CheckLightLevels(true);

                state.Send(new MobileUpdate(m));

                state.Send(new MobileIncomingSA(m, m));
                // state.Send( new MobileAttributes( m ) );
                state.Send(new MobileStatus(m, m));
                state.Send(Network.SetWarMode.Instantiate(m.Warmode));

                m.SendEverything();

                state.Send(SupportedFeatures.Instantiate(state));
                state.Send(new MobileUpdate(m));
                // state.Send( new MobileAttributes( m ) );
                state.Send(new MobileStatus(m, m));
                state.Send(Network.SetWarMode.Instantiate(m.Warmode));
                state.Send(new MobileIncomingSA(m, m));
            }
            else
            {
                state.Send(new MobileUpdateOld(m));
                state.Send(new MobileUpdateOld(m));

                m.CheckLightLevels(true);

                state.Send(new MobileUpdateOld(m));

                state.Send(new MobileIncomingOld(m, m));
                // state.Send( new MobileAttributes( m ) );
                state.Send(new MobileStatus(m, m));
                state.Send(Network.SetWarMode.Instantiate(m.Warmode));

                m.SendEverything();

                state.Send(SupportedFeatures.Instantiate(state));
                state.Send(new MobileUpdateOld(m));
                // state.Send( new MobileAttributes( m ) );
                state.Send(new MobileStatus(m, m));
                state.Send(Network.SetWarMode.Instantiate(m.Warmode));
                state.Send(new MobileIncomingOld(m, m));
            }

            state.Send(LoginComplete.Instance);
            state.Send(new CurrentTime());
            state.Send(SeasonChange.Instantiate(m.GetSeason(), true));
            if (m.Map != null)
            {
                state.Send(new MapChange(m.Map));
            }

            EventSink.InvokeLogin(m);

            m.ClearFastwalkStack();
        }

        public static void CreateCharacter(NetState state, CircularBufferReader reader)
        {
            var unk1 = reader.ReadInt32();
            var unk2 = reader.ReadInt32();
            int unk3 = reader.ReadByte();
            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);
            var flags = reader.ReadInt32();
            reader.Seek(8, SeekOrigin.Current);
            int prof = reader.ReadByte();
            reader.Seek(15, SeekOrigin.Current);

            int genderRace = reader.ReadByte();

            int str = reader.ReadByte();
            int dex = reader.ReadByte();
            int intl = reader.ReadByte();
            int is1 = reader.ReadByte();
            int vs1 = reader.ReadByte();
            int is2 = reader.ReadByte();
            int vs2 = reader.ReadByte();
            int is3 = reader.ReadByte();
            int vs3 = reader.ReadByte();
            int hue = reader.ReadUInt16();
            int hairVal = reader.ReadInt16();
            int hairHue = reader.ReadInt16();
            int hairValf = reader.ReadInt16();
            int hairHuef = reader.ReadInt16();
            reader.ReadByte();
            int cityIndex = reader.ReadByte();
            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();
            int shirtHue = reader.ReadInt16();
            int pantsHue = reader.ReadInt16();

            /*
            Pre-7.0.0.0:
            0x00, 0x01 -> Human Male, Human Female
            0x02, 0x03 -> Elf Male, Elf Female

            Post-7.0.0.0:
            0x00, 0x01
            0x02, 0x03 -> Human Male, Human Female
            0x04, 0x05 -> Elf Male, Elf Female
            0x05, 0x06 -> Gargoyle Male, Gargoyle Female
            */

            var female = genderRace % 2 != 0;

            Race race;

            if (state.StygianAbyss)
            {
                var raceID = (byte)(genderRace < 4 ? 0 : genderRace / 2 - 1);
                race = Race.Races[raceID];
            }
            else
            {
                race = Race.Races[(byte)(genderRace / 2)];
            }

            race ??= Race.DefaultRace;

            var info = state.CityInfo;
            var a = state.Account;

            if (info == null || a == null || cityIndex < 0 || cityIndex >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal)
                    {
                        state.WriteConsole("Account in use");
                        state.Send(new PopupMessage(PMMessage.CharInWorld));
                        return;
                    }
                }

                state.Flags = (ClientFlags)flags;

                var args = new CharacterCreatedEventArgs(
                    state,
                    a,
                    name,
                    female,
                    hue,
                    str,
                    dex,
                    intl,
                    info[cityIndex],
                    new[]
                    {
                        new SkillNameValue((SkillName)is1, vs1),
                        new SkillNameValue((SkillName)is2, vs2),
                        new SkillNameValue((SkillName)is3, vs3)
                    },
                    shirtHue,
                    pantsHue,
                    hairVal,
                    hairHue,
                    hairValf,
                    hairHuef,
                    prof,
                    race
                );

                state.Send(new ClientVersionReq());

                state.BlockAllPackets = true;

                EventSink.InvokeCharacterCreated(args);

                var m = args.Mobile;

                if (m != null)
                {
                    state.Mobile = m;
                    m.NetState = state;
                    new LoginTimer(state, m).Start();
                }
                else
                {
                    state.BlockAllPackets = false;
                    state.Dispose();
                }
            }
        }

        public static void CreateCharacter70160(NetState state, CircularBufferReader reader)
        {
            var unk1 = reader.ReadInt32();
            var unk2 = reader.ReadInt32();
            int unk3 = reader.ReadByte();
            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);
            var flags = reader.ReadInt32();
            reader.Seek(8, SeekOrigin.Current);
            int prof = reader.ReadByte();
            reader.Seek(15, SeekOrigin.Current);

            int genderRace = reader.ReadByte();

            int str = reader.ReadByte();
            int dex = reader.ReadByte();
            int intl = reader.ReadByte();
            int is1 = reader.ReadByte();
            int vs1 = reader.ReadByte();
            int is2 = reader.ReadByte();
            int vs2 = reader.ReadByte();
            int is3 = reader.ReadByte();
            int vs3 = reader.ReadByte();
            int is4 = reader.ReadByte();
            int vs4 = reader.ReadByte();

            int hue = reader.ReadUInt16();
            int hairVal = reader.ReadInt16();
            int hairHue = reader.ReadInt16();
            int hairValf = reader.ReadInt16();
            int hairHuef = reader.ReadInt16();
            reader.ReadByte();
            int cityIndex = reader.ReadByte();
            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();
            int shirtHue = reader.ReadInt16();
            int pantsHue = reader.ReadInt16();

            /*
            0x00, 0x01
            0x02, 0x03 -> Human Male, Human Female
            0x04, 0x05 -> Elf Male, Elf Female
            0x05, 0x06 -> Gargoyle Male, Gargoyle Female
            */

            var female = genderRace % 2 != 0;

            Race race;

            var raceID = (byte)(genderRace < 4 ? 0 : genderRace / 2 - 1);
            race = Race.Races[raceID] ?? Race.DefaultRace;

            var info = state.CityInfo;
            var a = state.Account;

            if (info == null || a == null || cityIndex < 0 || cityIndex >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal)
                    {
                        state.WriteConsole("Account in use");
                        state.Send(new PopupMessage(PMMessage.CharInWorld));
                        return;
                    }
                }

                state.Flags = (ClientFlags)flags;

                var args = new CharacterCreatedEventArgs(
                    state,
                    a,
                    name,
                    female,
                    hue,
                    str,
                    dex,
                    intl,
                    info[cityIndex],
                    new[]
                    {
                        new SkillNameValue((SkillName)is1, vs1),
                        new SkillNameValue((SkillName)is2, vs2),
                        new SkillNameValue((SkillName)is3, vs3),
                        new SkillNameValue((SkillName)is4, vs4)
                    },
                    shirtHue,
                    pantsHue,
                    hairVal,
                    hairHue,
                    hairValf,
                    hairHuef,
                    prof,
                    race
                );

                state.Send(new ClientVersionReq());

                state.BlockAllPackets = true;

                EventSink.InvokeCharacterCreated(args);

                var m = args.Mobile;

                if (m != null)
                {
                    state.Mobile = m;
                    m.NetState = state;
                    new LoginTimer(state, m).Start();
                }
                else
                {
                    state.BlockAllPackets = false;
                    state.Dispose();
                }
            }
        }

        private static int GenerateAuthID(NetState state)
        {
            if (m_AuthIDWindow.Count == m_AuthIDWindowSize)
            {
                var oldestID = 0;
                var oldest = DateTime.MaxValue;

                foreach (var kvp in m_AuthIDWindow)
                {
                    if (kvp.Value.Age < oldest)
                    {
                        oldestID = kvp.Key;
                        oldest = kvp.Value.Age;
                    }
                }

                m_AuthIDWindow.Remove(oldestID);
            }

            int authID;

            do
            {
                authID = Utility.Random(1, int.MaxValue - 1);

                if (Utility.RandomBool())
                {
                    authID |= 1 << 31;
                }
            } while (m_AuthIDWindow.ContainsKey(authID));

            m_AuthIDWindow[authID] = new AuthIDPersistence(state.Version);

            return authID;
        }

        public static void GameLogin(NetState state, CircularBufferReader reader)
        {
            if (state.SentFirstPacket)
            {
                state.Dispose();
                return;
            }

            state.SentFirstPacket = true;

            var authID = reader.ReadInt32();

            if (m_AuthIDWindow.TryGetValue(authID, out var ap))
            {
                m_AuthIDWindow.Remove(authID);

                state.Version = ap.Version;
            }
            else if (ClientVerification)
            {
                state.WriteConsole("Invalid client detected, disconnecting");
                state.Dispose();
                return;
            }

            if (state.m_AuthID != 0 && authID != state.m_AuthID)
            {
                state.WriteConsole("Invalid client detected, disconnecting");
                state.Dispose();
                return;
            }

            if (state.m_AuthID == 0 && authID != state.m_Seed)
            {
                state.WriteConsole("Invalid client detected, disconnecting");
                state.Dispose();
                return;
            }

            var username = reader.ReadAscii(30);
            var password = reader.ReadAscii(30);

            var e = new GameLoginEventArgs(state, username, password);

            EventSink.InvokeGameLogin(e);

            if (e.Accepted)
            {
                state.CityInfo = e.CityInfo;
                state.CompressionEnabled = true;

                state.Send(SupportedFeatures.Instantiate(state));

                if (state.NewCharacterList)
                {
                    state.Send(new CharacterList(state.Account, state.CityInfo));
                }
                else
                {
                    state.Send(new CharacterListOld(state.Account, state.CityInfo));
                }
            }
            else
            {
                state.Dispose();
            }
        }

        public static void PlayServer(NetState state, CircularBufferReader reader)
        {
            int index = reader.ReadInt16();
            var info = state.ServerInfo;
            var a = state.Account;

            if (info == null || a == null || index < 0 || index >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                var si = info[index];

                state.m_AuthID = PlayServerAck.m_AuthID = GenerateAuthID(state);

                state.SentFirstPacket = false;
                state.Send(new PlayServerAck(si));
            }
        }

        public static void LoginServerSeed(NetState state, CircularBufferReader reader)
        {
            state.m_Seed = reader.ReadInt32();
            state.Seeded = true;

            if (state.m_Seed == 0)
            {
                state.WriteConsole("Invalid client detected, disconnecting");
                state.Dispose();
                return;
            }

            var clientMaj = reader.ReadInt32();
            var clientMin = reader.ReadInt32();
            var clientRev = reader.ReadInt32();
            var clientPat = reader.ReadInt32();

            state.Version = new ClientVersion(clientMaj, clientMin, clientRev, clientPat);
        }

        public static void CrashReport(NetState state, CircularBufferReader reader)
        {
            var clientMaj = reader.ReadByte();
            var clientMin = reader.ReadByte();
            var clientRev = reader.ReadByte();
            var clientPat = reader.ReadByte();

            var x = reader.ReadUInt16();
            var y = reader.ReadUInt16();
            var z = reader.ReadSByte();
            var map = reader.ReadByte();

            var account = reader.ReadAscii(32);
            var character = reader.ReadAscii(32);
            var ip = reader.ReadAscii(15);

            var unk1 = reader.ReadInt32();
            var exception = reader.ReadInt32();

            var process = reader.ReadAscii(100);
            var report = reader.ReadAscii(100);

            reader.ReadByte(); // 0x00

            var offset = reader.ReadInt32();

            int count = reader.ReadByte();

            for (var i = 0; i < count; i++)
            {
                var address = reader.ReadInt32();
            }
        }

        public static void AccountLogin(NetState state, CircularBufferReader reader)
        {
            if (state.SentFirstPacket)
            {
                state.Dispose();
                return;
            }

            state.SentFirstPacket = true;

            var username = reader.ReadAscii(30);
            var password = reader.ReadAscii(30);

            var e = new AccountLoginEventArgs(state, username, password);

            EventSink.InvokeAccountLogin(e);

            if (e.Accepted)
            {
                AccountLogin_ReplyAck(state);
            }
            else
            {
                AccountLogin_ReplyRej(state, e.RejectReason);
            }
        }

        public static void AccountLogin_ReplyAck(NetState state)
        {
            var e = new ServerListEventArgs(state, state.Account);

            EventSink.InvokeServerList(e);

            if (e.Rejected)
            {
                state.Account = null;
                AccountLogin_ReplyRej(state, ALRReason.BadComm);
            }
            else
            {
                var info = e.Servers.ToArray();

                state.ServerInfo = info;

                state.Send(new AccountLoginAck(info));
            }
        }

        public static void AccountLogin_ReplyRej(NetState state, ALRReason reason)
        {
            state.Send(new AccountLoginRej(reason));
            state.Dispose();
        }

        public static void EquipMacro(NetState state, CircularBufferReader reader)
        {
            int count = reader.ReadByte();
            var serialList = new List<Serial>(count);
            for (var i = 0; i < count; ++i)
            {
                serialList.Add(reader.ReadUInt32());
            }

            EventSink.InvokeEquipMacro(state.Mobile, serialList);
        }

        public static void UnequipMacro(NetState state, CircularBufferReader reader)
        {
            int count = reader.ReadByte();
            var layers = new List<Layer>(count);
            for (var i = 0; i < count; ++i)
            {
                layers.Add((Layer)reader.ReadUInt16());
            }

            EventSink.InvokeUnequipMacro(state.Mobile, layers);
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

        private class LoginTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly NetState m_State;

            public LoginTimer(NetState state, Mobile m) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_State = state;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                if (m_State == null)
                {
                    Stop();
                    return;
                }

                if (m_State.Version != null)
                {
                    m_State.BlockAllPackets = false;
                    DoLogin(m_State, m_Mobile);
                    Stop();
                }
            }
        }

        internal struct AuthIDPersistence
        {
            public DateTime Age;
            public ClientVersion Version;

            public AuthIDPersistence(ClientVersion v)
            {
                Age = DateTime.UtcNow;
                Version = v;
            }
        }
    }
}
