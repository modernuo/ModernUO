/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpBuilder.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Components.Interpolation;
using Server.Gumps.Enums;
using Server.Gumps.Interfaces;
using Server.Network;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using static Server.Gumps.Components.Interpolation.GumpInterpolatedStringHandler;

namespace Server.Gumps.Components
{
    public static class GumpBuilder
    {
        internal const string DynamicStringPlaceholder = "§";

        internal static readonly byte[] HueAttr = Encoding.ASCII.GetBytes("hue=");
        internal static readonly byte[] ClassAttr = Encoding.ASCII.GetBytes("class=");
        internal static readonly byte[] LayoutBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
        internal static readonly byte[] StringsBuffer = GC.AllocateUninitializedArray<byte>(0x20000);

        public static GumpBuilder<StaticStringsHandler> ForStaticStrings(GumpFlags flags = GumpFlags.None)
            => new(flags);
        public static GumpBuilder<DynamicStringsHandler> ForDynamicStrings(GumpFlags flags = GumpFlags.None)
            => new(flags);
        public static string Dynamic(string s)
            => $"{DynamicStringPlaceholder}{s}";
    }

    public ref struct GumpBuilder<T>
        where T : struct, IStringsHandler
    {
        private static readonly byte[] _layoutBuffer = GumpBuilder.LayoutBuffer;

        internal T StringsWriter;
        private int _switches;
        private int _textEntries;
        private int _layoutPosition;

        internal readonly ReadOnlySpan<byte> Layout => _layoutBuffer.AsSpan(0, _layoutPosition);
        internal readonly int LayoutSize => _layoutPosition;

        public GumpBuilder()
            : this(GumpFlags.None)
        { }

        internal GumpBuilder(GumpFlags flags)
        {
            _layoutPosition = 0;
            StringsWriter = new();

            WriteProperty(Properties.NoMove, flags.HasFlag(GumpFlags.NoDraggable));
            WriteProperty(Properties.NoClose, flags.HasFlag(GumpFlags.NoClosable));
            WriteProperty(Properties.NoDispose, flags.HasFlag(GumpFlags.NoDisposable));
            WriteProperty(Properties.NoResize, flags.HasFlag(GumpFlags.NoResizable));
        }

        public void AddAlphaRegion(int x, int y, int width, int height)
        {
            WriteStart(Labels.Alpha);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteEnd();
        }

        public void AddBackground(int x, int y, int width, int height, int gumpId)
        {
            WriteStart(Labels.Background);
            WriteValue(x);
            WriteValue(y);
            WriteValue(gumpId);
            WriteValue(width);
            WriteValue(height);
            WriteEnd();
        }

        public void AddButton(int x, int y, int normalId, int pressedId, int buttonId, GumpButtonType type = GumpButtonType.Reply, int param = 0)
        {
            WriteStart(Labels.Button);
            WriteValue(x);
            WriteValue(y);
            WriteValue(normalId);
            WriteValue(pressedId);
            WriteValue(type, "D");
            WriteValue(param);
            WriteValue(buttonId);
            WriteEnd();
        }

        public void AddCheckbox(int x, int y, int inactiveId, int activeId, bool selected, int switchId)
        {
            WriteStart(Labels.Checkbox);
            WriteValue(x);
            WriteValue(y);
            WriteValue(inactiveId);
            WriteValue(activeId);
            WriteValue(selected);
            WriteValue(switchId);
            WriteEnd();

            _switches++;
        }

        public void AddGroup(int groupId)
        {
            WriteStart(Labels.Group);
            WriteValue(groupId);
            WriteEnd();
        }

        public void AddHtml(int x, int y, int width, int height, ReadOnlySpan<char> text,
            bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized(text);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtml(int x, int y, int width, int height, ref GumpInterpolatedStringHandler<T, None> handler,
            bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized(ref handler);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtml(int x, int y, int width, int height, int color, string? text, bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized($"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>");
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "used by interpolated string handler")]
        public void AddHtml(int x, int y, int width, int height, int color,
            [InterpolatedStringHandlerArgument(nameof(color))] scoped ref GumpInterpolatedStringHandler<T, Colored> handler,
            bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized(ref handler);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtmlCentered(int x, int y, int width, int height, string? text, bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized($"<CENTER>{text}</CENTER>");
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtmlCentered(int x, int y, int width, int height, ref GumpInterpolatedStringHandler<T, Centered> handler,
            bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized(ref handler);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtmlCentered(int x, int y, int width, int height, int color, string? text, bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized($"</BASEFONT COLOR=#{color:X6}><CENTER>{text}<CENTER></BASECOLOR>");
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "used by interpolated string handler")]
        public void AddHtmlCentered(int x, int y, int width, int height, int color,
            [InterpolatedStringHandlerArgument(nameof(color))] ref GumpInterpolatedStringHandler<T, Centered> handler,
            bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.Html);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValueInternalized(ref handler);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.HtmlLocalized);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(number);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteEnd();
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, short color, bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.HtmlLocalizedWithColor);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(number);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteValue(color);
            WriteEnd();
        }

        public void AddHtmlLocalized(int x, int y, int width, int height, int number, ReadOnlySpan<char> args, int color,
            bool background = false, bool scrollbar = false)
        {
            WriteStart(Labels.HtmlLocalizedWithArgs);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(background);
            WriteValue(scrollbar);
            WriteValue(color);
            WriteValue(number);
            WriteValue('@');
            WriteValue(args);
            WriteValue('@');
            WriteEnd();
        }

        public void AddImage(int x, int y, int gumpId, int hue = 0, ReadOnlySpan<char> @class = default)
        {
            WriteStart(Labels.Image);
            WriteValue(x);
            WriteValue(y);
            WriteValue(gumpId);

            if (hue != 0)
            {
                Write(GumpBuilder.HueAttr);
                WriteValue(hue);
            }

            if (!@class.IsEmpty)
            {
                Write(GumpBuilder.ClassAttr);
                WriteValue(@class);
            }

            WriteEnd();
        }

        public void AddImageTiledButton(int x, int y, int normalId, int pressedId, int buttonId, GumpButtonType type, int param,
            int itemId, int hue, int width, int height, int localizedTooltip = -1)
        {
            WriteStart(Labels.ImageTileButton);
            WriteValue(x);
            WriteValue(y);
            WriteValue(normalId);
            WriteValue(pressedId);
            WriteValue(type, "D");
            WriteValue(param);
            WriteValue(buttonId);
            WriteValue(itemId);
            WriteValue(hue);
            WriteValue(width);
            WriteValue(height);
            WriteEnd();

            if (localizedTooltip <= 0)
            {
                return;
            }

            WriteStart(Labels.Tooltip);
            WriteValue(localizedTooltip);
            WriteEnd();
        }

        public void AddImageTiled(int x, int y, int width, int height, int gumpId)
        {
            WriteStart(Labels.ImageTiled);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(gumpId);
            WriteEnd();
        }

        public void AddItem(int x, int y, int itemId, int hue = 0)
        {
            WriteStart(hue == 0 ? Labels.Item : Labels.ItemHued);
            WriteValue(x);
            WriteValue(y);
            WriteValue(itemId);

            if (hue != 0)
            {
                WriteValue(hue);
            }

            WriteEnd();
        }

        public void AddItemProperty(Serial serial)
        {
            WriteStart(Labels.ItemProperty);
            WriteValue(serial.Value);
            WriteEnd();
        }

        public void AddLabel(int x, int y, int hue, ReadOnlySpan<char> text)
        {
            WriteStart(Labels.Label);
            WriteValue(x);
            WriteValue(y);
            WriteValue(hue);
            WriteValueInternalized(text);
            WriteEnd();
        }

        public void AddLabelCropped(int x, int y, int width, int height, int hue, ReadOnlySpan<char> text)
        {
            WriteStart(Labels.LabelCropped);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(hue);
            WriteValueInternalized(text);
            WriteEnd();
        }

        public void AddGumpIdOverride(int gumpId)
        {
            WriteStart(Labels.MasterGump);
            WriteValue(gumpId);
            WriteEnd();
        }

        public void AddPage(int page = 0)
        {
            WriteStart(Labels.Page);
            WriteValue(page);
            WriteEnd();
        }

        public void AddRadio(int x, int y, int inactiveId, int activeId, bool selected, int switchId)
        {
            WriteStart(Labels.Radio);
            WriteValue(x);
            WriteValue(y);
            WriteValue(inactiveId);
            WriteValue(activeId);
            WriteValue(selected);
            WriteValue(switchId);
            WriteEnd();

            _switches++;
        }

        public void AddSpriteImage(int x, int y, int gumpId, int width, int height, int sx, int sy)
        {
            WriteStart(Labels.SpriteImage);
            WriteValue(x);
            WriteValue(y);
            WriteValue(gumpId);
            WriteValue(width);
            WriteValue(height);
            WriteValue(sx);
            WriteValue(sy);
            WriteEnd();
        }

        public void AddTextEntry(int x, int y, int width, int height, int hue, int entryId, ReadOnlySpan<char> initialText = default)
        {
            WriteStart(Labels.TextEntry);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(hue);
            WriteValue(entryId);
            WriteValueInternalized(initialText);
            WriteEnd();

            _textEntries++;
        }

        public void AddTextEntryLimited(int x, int y, int width, int height, int hue, int entryId, ReadOnlySpan<char> initialText = default, int size = 0)
        {
            WriteStart(Labels.TextEntryLimited);
            WriteValue(x);
            WriteValue(y);
            WriteValue(width);
            WriteValue(height);
            WriteValue(hue);
            WriteValue(entryId);
            WriteValueInternalized(initialText);
            WriteValue(size);
            WriteEnd();

            _textEntries++;
        }

        public void AddTooltip(int number)
        {
            WriteStart(Labels.Tooltip);
            WriteValue(number);
            WriteEnd();
        }

        public void AddTooltip(int number, ReadOnlySpan<char> args)
        {
            WriteStart(Labels.Tooltip);
            WriteValue(number);
            WriteValue('@');
            WriteValue(args);
            WriteValue('@');
            WriteEnd();
        }

        internal void FinalizeLayout()
        {
            _layoutBuffer[_layoutPosition++] = 0;
        }


        #region Writers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteValue<TValue>(TValue value, ReadOnlySpan<char> format = default, int bufferSize = 16)
            where TValue : ISpanFormattable
        {
            Span<char> buffer = stackalloc char[bufferSize];

            if (!value.TryFormat(buffer, out int charsWritten, format, null))
            {
                throw new Exception();
            }

            OperationStatus result = Ascii.FromUtf16(buffer[..charsWritten], _layoutBuffer.AsSpan(_layoutPosition), out int bytesWritten);
            _layoutPosition += bytesWritten;

            Debug.Assert(result == OperationStatus.Done);

            WriteValue(' ');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteValue(char value)
        {
            _layoutBuffer[_layoutPosition++] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteValue(ReadOnlySpan<char> value)
        {
            if (!value.IsEmpty)
            {
                OperationStatus result = Ascii.FromUtf16(value, _layoutBuffer.AsSpan(_layoutPosition), out int bytesWritten);
                _layoutPosition += bytesWritten;

                Debug.Assert(result == OperationStatus.Done);
            }

            WriteValue(' ');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteValueInternalized(ReadOnlySpan<char> value)
        {
            int index = StringsWriter.Internalize(value);
            WriteValue(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteValueInternalized<TFormatter>(ref GumpInterpolatedStringHandler<T, TFormatter> handler)
            where TFormatter : struct, IGumpInterpolationTextFormatter<TFormatter>
        {
            if (!handler.Success)
            {
                throw new Exception("Cannot convert interpolated string for gump content");
            }

            int index = StringsWriter.Internalize(handler.ToSpanAndClose());
            WriteValue(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteValue(bool value)
        {
            WriteValue(value ? '1' : '0');
            WriteValue(' ');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteProperty(Range name, bool condition)
        {
            if (!condition)
            {
                return;
            }

            Write(Properties.Buffer[name]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStart(Range entryName)
        {
            WriteValue('{');
            WriteValue(' ');
            Write(Labels.Buffer[entryName]);
            WriteValue(' ');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteEnd()
        {
            WriteValue('}');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(ReadOnlySpan<byte> span)
        {
            span.CopyTo(_layoutBuffer.AsSpan(_layoutPosition));
            _layoutPosition += span.Length;
        }
        #endregion Writers


        public void Send(NetState ns, Serial serial, int typeId, int x, int y, out int switches, out int textEntries)
        {
            switches = this._switches;
            textEntries = this._textEntries;

            int worstLayoutLength = Zlib.MaxPackSize(_layoutPosition);
            int worstStringsLength = Zlib.MaxPackSize(StringsWriter.BytesWritten);

            int maxLength = 40 + worstLayoutLength + worstStringsLength;

            SpanWriter writer = new(maxLength);
            writer.Write((byte)0xDD); // Packet ID
            writer.Seek(2, SeekOrigin.Current);

            writer.Write(serial);
            writer.Write(typeId);
            writer.Write(x);
            writer.Write(y);

            FinalizeLayout();
            OutgoingGumpPackets.WritePacked(_layoutBuffer.AsSpan(0, _layoutPosition), ref writer);

            writer.Write(StringsWriter.Count);
            StringsWriter.WriteCompressed(ref writer);

            writer.WritePacketLength();

            ns.Send(writer.Span);

            writer.Dispose();
        }

        internal Span<byte> Reserve(int size)
        {
            Span<byte> toRet = _layoutBuffer.AsSpan(_layoutPosition, size);
            _layoutPosition += size;
            return toRet;
        }

        public void Dispose()
        {
            StringsWriter.Dispose();
        }

        private static class Properties
        {
            public static readonly byte[] Buffer = Encoding.ASCII.GetBytes("{ nomove }{ noclose }{ nodispose }{ noresize }");

            public static readonly Range NoMove = 0..10;
            public static readonly Range NoClose = 10..21;
            public static readonly Range NoDispose = 21..34;
            public static readonly Range NoResize = 34..46;
        }

        private static class Labels
        {
            public static readonly byte[] Buffer = Encoding.ASCII.GetBytes(
                "checkertrans|resizepic|button|checkbox|group|htmlgump|xmfhtmlgump|xmfhtmlgumpcolor|xmfhtmltok|gumppic|buttontileart|tooltip|" +
                "gumppictiled|tilepic|tilepichue|itemproperty|text|croppedtext|mastergump|page|radio|picinpic|textentry|textentrylimited"
            );

            public static readonly Range Alpha = 0..12;
            public static readonly Range Background = 13..22;
            public static readonly Range Button = 23..29;
            public static readonly Range Checkbox = 30..38;
            public static readonly Range Group = 39..44;
            public static readonly Range Html = 45..53;
            public static readonly Range HtmlLocalized = 54..65;
            public static readonly Range HtmlLocalizedWithColor = 66..82;
            public static readonly Range HtmlLocalizedWithArgs = 83..93;
            public static readonly Range Image = 94..101;
            public static readonly Range ImageTileButton = 102..115;
            public static readonly Range Tooltip = 116..123;
            public static readonly Range ImageTiled = 124..136;
            public static readonly Range Item = 137..144;
            public static readonly Range ItemHued = 145..155;
            public static readonly Range ItemProperty = 156..168;
            public static readonly Range Label = 169..173;
            public static readonly Range LabelCropped = 174..185;
            public static readonly Range MasterGump = 186..196;
            public static readonly Range Page = 197..201;
            public static readonly Range Radio = 202..207;
            public static readonly Range SpriteImage = 208..216;
            public static readonly Range TextEntry = 217..226;
            public static readonly Range TextEntryLimited = 227..243;
        }
    }

    public static class GumpBuilderExtensions
    {
        public static void CompileCompressed(this in GumpBuilder<DynamicStringsHandler> builder, out LayoutEntry layout, out DynamicStringsEntry strings)
        {
            builder.FinalizeLayout();

            SpanWriter compressedLayoutWriter = new(Zlib.MaxPackSize(builder.LayoutSize));
            OutgoingGumpPackets.WritePacked(builder.Layout, ref compressedLayoutWriter);
            layout = new(compressedLayoutWriter.Span.ToArray(), true, builder.LayoutSize - 1);

            compressedLayoutWriter.Dispose();

            builder.StringsWriter.Finalize(out strings);
        }

        public static void CompileCompressed(this in GumpBuilder<StaticStringsHandler> builder, out LayoutEntry layout, out StringsEntry strings)
        {
            ref readonly StaticStringsHandler stringsWriter = ref builder.StringsWriter;

            builder.FinalizeLayout();

            int worstLayoutLength = Zlib.MaxPackSize(builder.LayoutSize);
            int worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);

            SpanWriter compressedLayoutWriter = new(int.Max(worstLayoutLength, worstStringsLength));
            OutgoingGumpPackets.WritePacked(builder.Layout, ref compressedLayoutWriter);

            layout = new(compressedLayoutWriter.Span.ToArray(), true, builder.LayoutSize - 1);
            strings = new(StaticStringsHandler.ToCompressedArray(compressedLayoutWriter.RawBuffer),
                stringsWriter.Count, true, stringsWriter.BytesWritten);

            compressedLayoutWriter.Dispose();
        }

        public static void Compile(this in GumpBuilder<StaticStringsHandler> builder, out LayoutEntry layout, out StringsEntry strings)
        {
            builder.FinalizeLayout();
            layout = new(builder.Layout.ToArray(), false, builder.LayoutSize);

            ref readonly StaticStringsHandler handler = ref builder.StringsWriter;
            strings = new(handler.ToArray(), handler.Count, false, handler.BytesWritten);
        }
    }
}
