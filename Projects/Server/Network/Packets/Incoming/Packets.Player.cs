/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.Player.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Diagnostics;
using Server.Gumps;

namespace Server.Network
{
    public static partial class Packets
    {
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

        public static void DeathStatusResponse(this NetState state, CircularBufferReader reader)
        {
            // Ignored
        }

        public static void RequestScrollWindow(this NetState state, CircularBufferReader reader)
        {
            int lastTip = reader.ReadInt16();
            int type = reader.ReadByte();
        }

        public static void AttackReq(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var m = World.FindMobile(reader.ReadUInt32());

            if (m != null)
            {
                from.Attack(m);
            }
        }

        public static void HuePickerResponse(this NetState state, CircularBufferReader reader)
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

        public static void SystemInfo(this NetState state, CircularBufferReader reader)
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

        public static void TextCommand(this NetState state, CircularBufferReader reader)
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
                        if (!int.TryParse(command.Split(' ')[0], out var skillIndex))
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
                        var split = command.Split(' ');

                        if (split.Length > 0)
                        {
                            var spellID = Utility.ToInt32(split[0]) - 1;
                            var serial = split.Length > 1 ? Utility.ToUInt32(split[1]) : (uint)Serial.MinusOne;

                            EventSink.InvokeCastSpellRequest(from, spellID, World.FindItem(serial));
                        }

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

                        EventSink.InvokeVirtueMacroRequest(from, virtueID);

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

        public static void AsciiPromptResponse(this NetState state, CircularBufferReader reader)
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

        public static void UnicodePromptResponse(this NetState state, CircularBufferReader reader)
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

        public static void MenuResponse(this NetState state, CircularBufferReader reader)
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

        public static void Disconnect(this NetState state, CircularBufferReader reader)
        {
            var minusOne = reader.ReadInt32();
        }

        public static void ConfigurationFile(this NetState state, CircularBufferReader reader)
        {
        }

        public static void LogoutReq(this NetState state, CircularBufferReader reader)
        {
            state.Send(new LogoutAck());
        }

        public static void ChangeSkillLock(this NetState state, CircularBufferReader reader)
        {
            var s = state.Mobile.Skills[reader.ReadInt16()];

            s?.SetLockNoRelay((SkillLock)reader.ReadByte());
        }

        public static void HelpRequest(this NetState state, CircularBufferReader reader)
        {
            EventSink.InvokeHelpRequest(state.Mobile);
        }

        public static void DisplayGumpResponse(this NetState state, CircularBufferReader reader)
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

        public static void SetWarMode(this NetState state, CircularBufferReader reader)
        {
            state.Mobile?.DelayChangeWarmode(reader.ReadBoolean());
        }

        // TODO: Throttle/make this more safe
        public static void Resynchronize(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            if (state.StygianAbyss)
            {
                state.Send(new MobileUpdate(from));
            }
            else
            {
                state.Send(new MobileUpdateOld(from));
            }

            state.Send(MobileIncoming.Create(state, from, from));

            from.SendEverything();

            state.Sequence = 0;

            from.ClearFastwalkStack();
        }

        public static void PingReq(this NetState state, CircularBufferReader reader)
        {
            state.Send(PingAck.Instantiate(reader.ReadByte()));
        }

        public static void SetUpdateRange(this NetState state, CircularBufferReader reader)
        {
            state.Send(ChangeUpdateRange.Instantiate(18));
        }

        // TODO: Change to OSI fastwalk stack
        public static void MovementReq(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var dir = (Direction)reader.ReadByte();
            int seq = reader.ReadByte();
            var key = reader.ReadInt32(); // Fastwalk stack key

            if (state.Sequence == 0 && seq != 0 || !from.Move(dir))
            {
                state.Send(new MovementRej(seq, from));
                state.Sequence = 0;

                from.ClearFastwalkStack();
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

        public static void Animate(this NetState state, CircularBufferReader reader)
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

        public static void QuestArrow(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            var rightClick = reader.ReadBoolean();

            from.QuestArrow?.OnClick(rightClick);
        }

        public static void CastSpell(this NetState state, CircularBufferReader reader)
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

        public static void ToggleFlying(this NetState state, CircularBufferReader reader)
        {
            state.Mobile?.ToggleFlying();
        }

        public static void StunRequest(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            EventSink.InvokeStunRequest(from);
        }

        public static void DisarmRequest(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            EventSink.InvokeDisarmRequest(from);
        }

        public static void StatLockChange(this NetState state, CircularBufferReader reader)
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

        public static void ScreenSize(this NetState state, CircularBufferReader reader)
        {
            var width = reader.ReadInt32();
            var unk = reader.ReadInt32();
        }

        public static void CloseStatus(this NetState state, CircularBufferReader reader)
        {
            Serial serial = reader.ReadUInt32();
        }

        public static void Language(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;

            if (from == null)
            {
                return;
            }

            from.Language = reader.ReadAscii(4);
        }

        public static void MobileQuery(this NetState state, CircularBufferReader reader)
        {
            var from = state.Mobile;
            if (from == null)
            {
                return;
            }

            reader.ReadInt32(); // 0xEDEDEDED
            int type = reader.ReadByte();
            var m = World.FindMobile(reader.ReadUInt32());

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
                        reader.Trace(state);
                        break;
                    }
            }
        }

        public static void CrashReport(this NetState state, CircularBufferReader reader)
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

        public static void SetAbility(this NetState state, IEntity e, EncodedReader reader)
        {
            EventSink.InvokeSetAbility(state.Mobile, reader.ReadInt32());
        }

        public static void GuildGumpRequest(this NetState state, IEntity e, EncodedReader reader)
        {
            EventSink.InvokeGuildGumpRequest(state.Mobile);
        }

        public static void QuestGumpRequest(this NetState state, IEntity e, EncodedReader reader)
        {
            EventSink.InvokeQuestGumpRequest(state.Mobile);
        }
    }
}
