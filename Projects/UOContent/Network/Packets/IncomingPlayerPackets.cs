/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingPlayerPackets.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.Diagnostics;
using Microsoft.Toolkit.HighPerformance;
using Server.Diagnostics;
using Server.Engines.Virtues;
using Server.Exceptions;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Network;

public static class IncomingPlayerPackets
{
    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x01, 5, false, &Disconnect);
        IncomingPackets.Register(0x05, 5, true, &AttackReq);
        IncomingPackets.Register(0x12, 0, true, &TextCommand);
        IncomingPackets.Register(0x22, 3, true, &Resynchronize);
        IncomingPackets.Register(0x2C, 2, true, &DeathStatusResponse);
        IncomingPackets.Register(0x34, 10, true, &MobileQuery);
        IncomingPackets.Register(0x3A, 0, true, &ChangeSkillLock);
        IncomingPackets.Register(0x72, 5, true, &SetWarMode);
        IncomingPackets.Register(0x73, 2, false, &PingReq);
        IncomingPackets.Register(0x7D, 13, true, &MenuResponse);
        IncomingPackets.Register(0x95, 9, true, &HuePickerResponse);
        IncomingPackets.Register(0x9A, 0, true, &AsciiPromptResponse);
        IncomingPackets.Register(0x9B, 258, true, &HelpRequest);
        IncomingPackets.Register(0xA4, 149, false, &SystemInfo);
        IncomingPackets.Register(0xA7, 4, true, &RequestScrollWindow);
        IncomingPackets.Register(0xB1, 0, true, &DisplayGumpResponse);
        IncomingPackets.Register(0xC2, 0, true, &UnicodePromptResponse);
        IncomingPackets.Register(0xC8, 2, true, &SetUpdateRange);
        IncomingPackets.Register(0xD0, 0, true, &ConfigurationFile);
        IncomingPackets.Register(0xD1, 2, true, &LogoutReq);
        IncomingPackets.Register(0xD7, 0, true, &EncodedCommand);
        IncomingPackets.Register(0xF4, 0, false, &CrashReport);

        IncomingPackets.RegisterEncoded(0x28, true, &GuildGumpRequest);
        IncomingPackets.RegisterEncoded(0x32, true, &QuestGumpRequest);
    }

    public static void DeathStatusResponse(NetState state, SpanReader reader, int packetLength)
    {
        // Ignored
    }

    public static void RequestScrollWindow(NetState state, SpanReader reader, int packetLength)
    {
        int lastTip = reader.ReadInt16();
        int type = reader.ReadByte();
    }

    public static void AttackReq(NetState state, SpanReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var m = World.FindMobile((Serial)reader.ReadUInt32());

        if (m != null)
        {
            from.Attack(m);
        }
    }

    public static void HuePickerResponse(NetState state, SpanReader reader, int packetLength)
    {
        var serial = reader.ReadUInt32();
        _ = reader.ReadInt16(); // Item ID
        var hue = Utility.ClipDyedHue(reader.ReadInt16() & 0x3FFF);

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

    public static void SystemInfo(NetState state, SpanReader reader, int packetLength)
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

    public static void TextCommand(NetState state, SpanReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        int type = reader.ReadByte();
        var command = reader.ReadAscii();

        switch (type)
        {
            case 0xC7: // Animate
                {
                    EventSink.InvokeAnimateRequest(from, command);

                    break;
                }
            case 0x24: // Use skill
                {
                    var tokenizer = command.Tokenize(' ');
                    if (!tokenizer.MoveNext() || !int.TryParse(tokenizer.Current, out var skillIndex))
                    {
                        break;
                    }

                    Skills.UseSkill(from, skillIndex);

                    break;
                }
            case 0x43: // Open spellbook
                {
                    if (!int.TryParse(command, out var booktype))
                    {
                        booktype = 1;
                    }

                    EventSink.InvokeOpenSpellbookRequest(from, booktype);

                    break;
                }
            case 0x27: // Cast spell from book
                {
                    var tokenizer = command.Tokenize(' ');
                    var spellID = (tokenizer.MoveNext() ? Utility.ToInt32(tokenizer.Current) : 0) - 1;
                    var serial = tokenizer.MoveNext() ? (Serial)Utility.ToUInt32(tokenizer.Current) : Serial.MinusOne;

                    EventSink.InvokeCastSpellRequest(from, spellID, World.FindItem(serial));

                    break;
                }
            case 0x58: // Open door
                {
                    EventSink.InvokeOpenDoorMacroUsed(from);

                    break;
                }
            case 0x56: // Cast spell from macro
                {
                    var spellID = Utility.ToInt32(command) - 1;

                    EventSink.InvokeCastSpellRequest(from, spellID, null);

                    break;
                }
            case 0xF4: // Invoke virtues from macro
                {
                    var virtueID = Utility.ToInt32(command) - 1;

                    VirtueGump.RequestVirtueMacro((PlayerMobile)from, virtueID);

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
                    state.LogInfo($"Unknown text-command type 0x{state:X2}: {type} ({command})");
                    break;
                }
        }
    }

    public static void AsciiPromptResponse(NetState state, SpanReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var serial = reader.ReadUInt32();
        var prompt = reader.ReadInt32();
        var type = reader.ReadInt32();
        var text = reader.ReadAsciiSafe();

        if (text.Length > 128)
        {
            return;
        }

        var p = from.Prompt;

        if (p?.Serial == serial && p.Serial == prompt)
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

    public static void UnicodePromptResponse(NetState state, SpanReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        var serial = reader.ReadUInt32();
        var prompt = reader.ReadInt32();
        var type = reader.ReadInt32();
        var lang = reader.ReadAscii(4);
        var text = reader.ReadLittleUniSafe();

        if (text.Length > 128)
        {
            return;
        }

        var p = from.Prompt;

        if (p?.Serial == serial && p.Serial == prompt)
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

    public static void MenuResponse(NetState state, SpanReader reader, int packetLength)
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

    public static void Disconnect(NetState state, SpanReader reader, int packetLength)
    {
        var minusOne = reader.ReadInt32();
    }

    public static void ConfigurationFile(NetState state, SpanReader reader, int packetLength)
    {
    }

    public static void LogoutReq(NetState state, SpanReader reader, int packetLength)
    {
        state.SendLogoutAck();
    }

    public static void ChangeSkillLock(NetState state, SpanReader reader, int packetLength)
    {
        var s = state.Mobile.Skills[reader.ReadInt16()];

        s?.SetLockNoRelay((SkillLock)reader.ReadByte());
    }

    public static void HelpRequest(NetState state, SpanReader reader, int packetLength)
    {
        EventSink.InvokeHelpRequest(state.Mobile);
    }

    public static void DisplayGumpResponse(NetState state, SpanReader reader, int packetLength)
    {
        var serial = (Serial)reader.ReadUInt32();
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
                state.LogInfo("Invalid gump response, disconnecting...");
                var exception = new InvalidGumpResponseException($"Button {buttonID} doesn't exist");
                exception.SetStackTrace(new StackTrace());
                NetState.TraceException(exception);
                state.Mobile?.SendMessage("Invalid gump response.");

                // state.Disconnect("Invalid gump response.");
                return;
            }

            var switchCount = reader.ReadInt32();

            if (switchCount < 0 || switchCount > gump.Switches)
            {
                state.LogInfo("Invalid gump response, disconnecting...");
                var exception = new InvalidGumpResponseException($"Bad switch count {switchCount}");
                exception.SetStackTrace(new StackTrace());
                NetState.TraceException(exception);
                state.Mobile?.SendMessage("Invalid gump response.");

                // state.Disconnect("Invalid gump response.");
                return;
            }

            var switches = new int[switchCount];

            for (var i = 0; i < switches.Length; ++i)
            {
                switches[i] = reader.ReadInt32();
            }

            var textCount = reader.ReadInt32();

            if (textCount < 0 || textCount > gump.TextEntries)
            {
                state.LogInfo("Invalid gump response, disconnecting...");
                var exception = new InvalidGumpResponseException($"Bad text entry count {textCount}");
                exception.SetStackTrace(new StackTrace());
                NetState.TraceException(exception);
                state.Mobile?.SendMessage("Invalid gump response.");

                // state.Disconnect("Invalid gump response.");
                return;
            }

            var textEntries = new TextRelay[textCount];

            for (var i = 0; i < textEntries.Length; ++i)
            {
                int entryID = reader.ReadUInt16();
                int textLength = reader.ReadUInt16();

                if (textLength > 239)
                {
                    state.LogInfo("Invalid gump response, disconnecting...");
                    var exception = new InvalidGumpResponseException($"Text entry {i} is too long ({textLength})");
                    exception.SetStackTrace(new StackTrace());
                    NetState.TraceException(exception);
                    state.Mobile?.SendMessage("Invalid gump response.");

                    // state.Disconnect("Invalid gump response.");
                    return;
                }

                var text = reader.ReadBigUniSafe(textLength);
                textEntries[i] = new TextRelay(entryID, text);
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
            var switchCount = reader.Remaining >= 4 ? reader.ReadInt32() : 0;

            if (buttonID == 1 && switchCount > 0)
            {
                var beheld = World.FindEntity<PlayerMobile>((Serial)reader.ReadUInt32());

                if (beheld != null)
                {
                    VirtueGump.RequestVirtueGump((PlayerMobile)state.Mobile, beheld);
                }
            }
            else
            {
                var beheld = World.FindMobile(serial);

                if (beheld != null)
                {
                    VirtueGump.RequestVirtueItem((PlayerMobile)state.Mobile, beheld, buttonID);
                }
            }
        }
    }

    public static void SetWarMode(NetState state, SpanReader reader, int packetLength)
    {
        state.Mobile?.DelayChangeWarmode(reader.ReadBoolean());
    }

    // TODO: Throttle/make this more safe
    public static void Resynchronize(NetState state, SpanReader reader, int packetLength)
    {
        var from = state.Mobile;

        if (from == null)
        {
            return;
        }

        state.SendMobileUpdate(from);
        state.SendMobileIncoming(from, from);

        from.SendEverything();

        state.Sequence = 0;
    }

    public static void PingReq(NetState state, SpanReader reader, int packetLength)
    {
        state.SendPingAck(reader.ReadByte());
    }

    public static void SetUpdateRange(NetState state, SpanReader reader, int packetLength)
    {
        state.SendChangeUpdateRange(18);
    }

    public static void MobileQuery(NetState state, SpanReader reader, int packetLength)
    {
        var from = state.Mobile;
        if (from == null)
        {
            return;
        }

        reader.ReadInt32(); // 0xEDEDEDED
        int type = reader.ReadByte();
        var m = World.FindMobile((Serial)reader.ReadUInt32());

        if (m == null)
        {
            return;
        }

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
                    state.Trace(reader.Buffer);
                    break;
                }
        }
    }

    public static void CrashReport(NetState state, SpanReader reader, int packetLength)
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

    public static void GuildGumpRequest(NetState state, IEntity e, EncodedReader reader)
    {
        EventSink.InvokeGuildGumpRequest(state.Mobile);
    }

    public static void QuestGumpRequest(NetState state, IEntity e, EncodedReader reader)
    {
        EventSink.InvokeQuestGumpRequest(state.Mobile);
    }

    public static unsafe void EncodedCommand(NetState state, SpanReader reader, int packetLength)
    {
        var e = World.FindEntity((Serial)reader.ReadUInt32());
        int packetId = reader.ReadUInt16();

        // We will add support if this is ever a real thing.
        if (packetId > 0xFF)
        {
            var reason = $"Sent unsupported encoded packet (0xD7x{packetId:X4}";
            state.LogInfo(reason);
            state.Disconnect(reason);
        }

        var ph = IncomingPackets.GetEncodedHandler(packetId);

        if (ph == null)
        {
            state.Trace(reader.Buffer);
            return;
        }

        if (ph.Ingame && state.Mobile == null)
        {
            var reason = $"Sent in-game packet (0xD7x{packetId:X4}) before being attached to a mobile.";
            state.LogInfo(reason);
            state.Disconnect(reason);
        }
        else if (ph.Ingame && state.Mobile.Deleted)
        {
            state.Disconnect($"Sent in-game packet(0xD7x{packetId:X4}) but mobile is deleted.");
        }
        else
        {
            ph.OnReceive(state, e, new EncodedReader(reader));
        }
    }
}
