/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StaticGump.cs                                                   *
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
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Network;

namespace Server.Gumps;

public abstract class StaticGump<TSelf> : BaseGump where TSelf : StaticGump<TSelf>
{
    private static int _switches;
    private static int _textEntries;
    private static byte[] _compressedLayoutData;
    private static byte[] _compressedStringsData;

    private static bool _hasDynamicStrings;
    private static int _staticStringsCount;
    private static byte[] _staticStrings;

    // Turn this off if you want to generate a new gump each time for testing
    protected virtual bool Cached => true;

    public override int Switches => _switches;
    public override int TextEntries => _textEntries;

    public StaticGump(int x, int y) : base(x, y)
    {
    }

    protected abstract void BuildLayout(ref StaticGumpBuilder builder);

    protected virtual void BuildStrings(ref GumpStringsBuilder builder)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteLayout(ref SpanWriter writer, ref StaticGumpBuilder gumpBuilder)
    {
        var layoutPos = writer.Position;
        OutgoingGumpPackets.WritePacked(gumpBuilder.LayoutData, ref writer);
        var layoutLength = writer.Position - layoutPos;

        _compressedLayoutData = GC.AllocateUninitializedArray<byte>(layoutLength);
        writer.Span.Slice(layoutPos, layoutLength).CopyTo(_compressedLayoutData);
    }

    public override void Compile(ref SpanWriter writer)
    {
        writer.Write((byte)0xDD); // Packet ID
        writer.Seek(2, SeekOrigin.Current);

        writer.Write(Serial);
        writer.Write(TypeID);
        writer.Write(X);
        writer.Write(Y);

        if (Cached && _compressedLayoutData != null)
        {
            writer.Write(_compressedLayoutData);

            if (_compressedStringsData != null)
            {
                writer.Write(_staticStringsCount);
                writer.Write(_compressedStringsData);
            }
            else if (_hasDynamicStrings)
            {
                var stringsBuilder = new GumpStringsBuilder(false);
                BuildStrings(ref stringsBuilder);

                if (_staticStringsCount == 0)
                {
                    writer.Write(stringsBuilder._stringsCount);
                    OutgoingGumpPackets.WritePacked(stringsBuilder.StringsBuffer, ref writer);
                }
                else
                {
                    writer.Write(_staticStringsCount + stringsBuilder._stringsCount);

                    var stringsData = stringsBuilder.StringsBuffer;
                    var stringsLength = _staticStrings.Length + stringsData.Length;

                    var buffer = STArrayPool<byte>.Shared.Rent(stringsLength);
                    _staticStrings.CopyTo(buffer.AsSpan());
                    stringsData.CopyTo(buffer.AsSpan(_staticStrings.Length));

                    OutgoingGumpPackets.WritePacked(buffer.AsSpan(0, stringsLength), ref writer);
                    STArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else if (_staticStrings != null)
            {
                writer.Write(_staticStringsCount);
                OutgoingGumpPackets.WritePacked(_staticStrings, ref writer);
            }
        }
        else
        {
            StaticGumpBuilder gumpBuilder = new StaticGumpBuilder();
            BuildLayout(ref gumpBuilder);
            gumpBuilder.FinalizeLayout();

            _switches = gumpBuilder.Switches;
            _textEntries = gumpBuilder.TextEntries;
            _staticStringsCount = gumpBuilder._stringsCount;

            var staticStringsData = gumpBuilder.StringsData;
            var hasDynamicStrings = _hasDynamicStrings = gumpBuilder.StringSlotOffsets.Length > 0;

            if (hasDynamicStrings)
            {
                _staticStrings = GC.AllocateUninitializedArray<byte>(staticStringsData.Length);
                staticStringsData.CopyTo(_staticStrings);

                var stringsBuilder = new GumpStringsBuilder(true);
                BuildStrings(ref stringsBuilder);
                stringsBuilder.FinalizeStrings(ref gumpBuilder); // Modifies the layout

                WriteLayout(ref writer, ref gumpBuilder);

                writer.Write(_staticStringsCount + stringsBuilder._stringsCount);

                var stringsData = stringsBuilder.StringsBuffer;
                var stringsLength = staticStringsData.Length + stringsData.Length;

                var buffer = STArrayPool<byte>.Shared.Rent(stringsLength);
                staticStringsData.CopyTo(buffer.AsSpan());
                stringsData.CopyTo(buffer.AsSpan(staticStringsData.Length));

                OutgoingGumpPackets.WritePacked(buffer.AsSpan(0, stringsLength), ref writer);

                STArrayPool<byte>.Shared.Return(buffer);
                stringsBuilder.Dispose();
            }
            else
            {
                WriteLayout(ref writer, ref gumpBuilder);

                writer.Write(_staticStringsCount);

                var stringsPos = writer.Position;
                OutgoingGumpPackets.WritePacked(staticStringsData, ref writer);
                var stringsLength = writer.Position - stringsPos;

                _compressedStringsData = GC.AllocateUninitializedArray<byte>(stringsLength);
                writer.Span.Slice(stringsPos, stringsLength).CopyTo(_compressedStringsData);
            }

            gumpBuilder.Dispose();
        }

        writer.WritePacketLength();
    }
}
