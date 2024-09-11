/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingGumpPackets.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Engines.Virtues;
using Server.Exceptions;
using Server.Mobiles;
using Server.Network;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Server.Gumps;

public static partial class GumpSystem
{
    public static void DisplayGumpResponse(NetState state, SpanReader reader)
    {
        var serial = (Serial)reader.ReadUInt32();
        var typeId = reader.ReadInt32();
        var buttonId = reader.ReadInt32();

        BaseGump baseGump = null;

        if (_gumps.TryGetValue(state, out var gumps))
        {
            foreach (var g in gumps)
            {
                if (g.Serial != serial || g.TypeID != typeId)
                {
                    continue;
                }

                baseGump = g;
                break;
            }
        }

        if (baseGump != null)
        {
            if (baseGump is Gump gump)
            {
                var buttonExists = buttonId == 0; // 0 is always 'close'

                if (!buttonExists)
                {
                    foreach (var e in gump.Entries)
                    {
                        if ((e as GumpButton)?.ButtonID == buttonId)
                        {
                            buttonExists = true;
                            break;
                        }

                        if ((e as GumpImageTileButton)?.ButtonID == buttonId)
                        {
                            buttonExists = true;
                            break;
                        }
                    }
                }

                if (!buttonExists)
                {
                    state.LogInfo("Invalid gump response, disconnecting...");
                    var exception = new InvalidGumpResponseException($"Button {buttonId} doesn't exist");
                    exception.SetStackTrace(new StackTrace());
                    NetState.TraceException(exception);
                    return;
                }
            }

            var switchCount = reader.ReadInt32();

            if (switchCount < 0 || switchCount > baseGump.Switches)
            {
                state.LogInfo("Invalid gump response, disconnecting...");
                var exception = new InvalidGumpResponseException($"Bad switch count {switchCount}");
                exception.SetStackTrace(new StackTrace());
                NetState.TraceException(exception);
                return;
            }

            int switchByteCount = switchCount * 4;

            // Read all the integers
            ReadOnlySpan<int> switchBlock =
                MemoryMarshal.Cast<byte, int>(reader.Buffer.Slice(reader.Position, switchByteCount));

            reader.Seek(switchByteCount, SeekOrigin.Current);

            scoped ReadOnlySpan<int> switches;

            // Swap the endianness if necessary
            if (BitConverter.IsLittleEndian)
            {
                Span<int> reversedSwitches = stackalloc int[switchCount];
                BinaryPrimitives.ReverseEndianness(switchBlock, reversedSwitches);
                switches = reversedSwitches;
            }
            else
            {
                switches = switchBlock;
            }

            var textCount = reader.ReadInt32();
            if (textCount < 0 || textCount > baseGump.TextEntries)
            {
                state.LogInfo("Invalid gump response, disconnecting...");
                var exception = new InvalidGumpResponseException($"Bad text entry count {textCount}");
                exception.SetStackTrace(new StackTrace());
                NetState.TraceException(exception);
                return;
            }

            Span<ushort> textIds = stackalloc ushort[textCount];
            Span<Range> textFields = stackalloc Range[textCount];

            var textOffset = reader.Position;
            for (var i = 0; i < textCount; i++)
            {
                var textId = reader.ReadUInt16();
                var textLength = reader.ReadUInt16();

                if (textLength > 239)
                {
                    state.LogInfo("Invalid gump response, disconnecting...");
                    var exception = new InvalidGumpResponseException($"Text entry {i} is too long ({textLength})");
                    exception.SetStackTrace(new StackTrace());
                    NetState.TraceException(exception);
                    return;
                }

                textIds[i] = textId;
                var offset = reader.Position - textOffset;
                var length = textLength * 2;
                textFields[i] = offset..(offset + length);
                reader.Seek(length, SeekOrigin.Current);
            }

            var textBlock = reader.Buffer.Slice(textOffset, reader.Position - textOffset);

            Remove(state, baseGump);

            var relayInfo = new RelayInfo(
                buttonId,
                switches,
                textIds,
                textFields,
                textBlock
            );
            baseGump.OnResponse(state, relayInfo);
        }

        if (typeId == 461)
        {
            // Virtue gump
            var switchCount = reader.Remaining >= 4 ? reader.ReadInt32() : 0;

            if (buttonId == 1 && switchCount > 0)
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
                    VirtueGump.RequestVirtueItem((PlayerMobile)state.Mobile, beheld, buttonId);
                }
            }
        }
    }
}
