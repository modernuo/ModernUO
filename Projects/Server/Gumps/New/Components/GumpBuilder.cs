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
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using static Server.Gumps.Components.GumpBuilderExtensions;
using static Server.Gumps.Components.Interpolation.GumpInterpolationTextFormatters;

namespace Server.Gumps.Components;

public ref struct GumpBuilder<T> where T : struct, IStringsHandler
{
    private static readonly byte[] _layoutBuffer = LayoutBuffer;

    internal T StringsWriter;
    private int _switches;
    private int _textEntries;
    private int _layoutPosition;

    internal readonly ReadOnlySpan<byte> Layout => _layoutBuffer.AsSpan(0, _layoutPosition);
    internal readonly int LayoutSize => _layoutPosition;
    internal readonly int Switches => _switches;
    internal readonly int TextEntries => _textEntries;

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
            Write(HueAttr);
            WriteValue(hue);
        }

        if (!@class.IsEmpty)
        {
            Write(ClassAttr);
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

    public void AddLabel(int x, int y, int hue, ref GumpInterpolatedStringHandler<T, None> handler)
    {
        WriteStart(Labels.Label);
        WriteValue(x);
        WriteValue(y);
        WriteValue(hue);
        WriteValueInternalized(ref handler);
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

    public void AddLabelCropped(int x, int y, int width, int height, int hue, ref GumpInterpolatedStringHandler<T, None> handler)
    {
        WriteStart(Labels.LabelCropped);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValueInternalized(ref handler);
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

    public void AddTextEntryLimited(int x, int y, int width, int height, int hue, int entryId, ref GumpInterpolatedStringHandler<T, None> handler, int size = 0)
    {
        WriteStart(Labels.TextEntryLimited);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValue(entryId);
        WriteValueInternalized(ref handler);
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
            throw new Exception($"Failed to format '{value}' with the given format '{format}'");
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
    private void WriteProperty(byte[] name, bool condition)
    {
        if (!condition)
        {
            return;
        }

        Write(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteStart(byte[] entryName)
    {
        WriteValue('{');
        WriteValue(' ');
        Write(entryName);
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
}

public static class GumpBuilderExtensions
{
    internal static readonly byte[] LayoutBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    internal static readonly byte[] StringsBuffer = GC.AllocateUninitializedArray<byte>(0x20000);
    internal static readonly byte[] HueAttr = "hue="u8.ToArray();
    internal static readonly byte[] ClassAttr = "class="u8.ToArray();

    internal static void CompileCompressed(this ref GumpBuilder<DynamicStringsHandler> builder, out LayoutEntry layout, out DynamicStringsEntry strings)
    {
        builder.FinalizeLayout();

        SpanWriter compressedLayoutWriter = new(Zlib.MaxPackSize(builder.LayoutSize));
        OutgoingGumpPackets.WritePacked(builder.Layout, ref compressedLayoutWriter);

        layout = new(compressedLayoutWriter.Span.ToArray(), builder.LayoutSize - 1, builder.Switches, builder.TextEntries);
        strings = builder.StringsWriter.Finalize();

        compressedLayoutWriter.Dispose();
    }

    internal static void CompileCompressed(this ref GumpBuilder<StaticStringsHandler> builder, out LayoutEntry layout, out StaticStringsEntry strings)
    {
        ref readonly StaticStringsHandler stringsWriter = ref builder.StringsWriter;

        builder.FinalizeLayout();

        int worstLayoutLength = Zlib.MaxPackSize(builder.LayoutSize);
        int worstStringsLength = Zlib.MaxPackSize(stringsWriter.BytesWritten);

        SpanWriter compressedLayoutWriter = new(int.Max(worstLayoutLength, worstStringsLength));
        OutgoingGumpPackets.WritePacked(builder.Layout, ref compressedLayoutWriter);

        layout = new(compressedLayoutWriter.Span.ToArray(), builder.LayoutSize - 1, builder.Switches, builder.TextEntries);
        strings = builder.StringsWriter.Finalize(compressedLayoutWriter.RawBuffer);

        compressedLayoutWriter.Dispose();
    }

    public static void AddHtmlSlot(this ref GumpBuilder<DynamicStringsHandler> builder, int x, int y, int width, int height,
        ReadOnlySpan<char> slotKey, bool background = false, bool scrollbar = false)
    {
        DynamicStringsHandler.DynamicMode = true;
        builder.AddHtml(x, y, width, height, $"key_{slotKey}", background, scrollbar);
        DynamicStringsHandler.DynamicMode = false;
    }

    public static void AddLabelSlot(this ref GumpBuilder<DynamicStringsHandler> builder, int x, int y, int hue, ReadOnlySpan<char> slotKey)
    {
        DynamicStringsHandler.DynamicMode = true;
        builder.AddLabel(x, y, hue, $"key_{slotKey}");
        DynamicStringsHandler.DynamicMode = false;
    }

    public static void AddLabelCroppedSlot(this ref GumpBuilder<DynamicStringsHandler> builder, int x, int y,
        int width, int height, int hue, string slotKey)
    {
        DynamicStringsHandler.DynamicMode = true;
        builder.AddLabelCropped(x, y, width, height, hue, $"key_{slotKey}");
        DynamicStringsHandler.DynamicMode = false;
    }

    public static void AddTextEntrySlot(this ref GumpBuilder<DynamicStringsHandler> builder, int x, int y,
        int width, int height, int hue, int entryId, string slotKey)
    {
        DynamicStringsHandler.DynamicMode = true;
        builder.AddTextEntry(x, y, width, height, hue, entryId, $"key_{slotKey}");
        DynamicStringsHandler.DynamicMode = false;
    }

    public static void AddTextEntryLimitedSlot(this ref GumpBuilder<DynamicStringsHandler> builder, int x, int y,
        int width, int height, int hue, int entryId, ReadOnlySpan<char> slotKey, int size = 0)
    {
        DynamicStringsHandler.DynamicMode = true;
        builder.AddTextEntryLimited(x, y, width, height, hue, entryId, $"key_{slotKey}", size);
        DynamicStringsHandler.DynamicMode = false;
    }
}

static file class Properties
{
    public static readonly byte[] NoMove = "{ nomove }"u8.ToArray();
    public static readonly byte[] NoClose = "{ noclose }"u8.ToArray();
    public static readonly byte[] NoDispose = "{ nodispose }"u8.ToArray();
    public static readonly byte[] NoResize = "{ noresize }"u8.ToArray();
}

static file class Labels
{
    public static readonly byte[] Alpha = "checkertrans"u8.ToArray();
    public static readonly byte[] Background = "resizepic"u8.ToArray();
    public static readonly byte[] Button = "button"u8.ToArray();
    public static readonly byte[] Checkbox = "checkbox"u8.ToArray();
    public static readonly byte[] Group = "group"u8.ToArray();
    public static readonly byte[] Html = "htmlgump"u8.ToArray();
    public static readonly byte[] HtmlLocalized = "xmfhtmlgump"u8.ToArray();
    public static readonly byte[] HtmlLocalizedWithColor = "xmfhtmlgumpcolor"u8.ToArray();
    public static readonly byte[] HtmlLocalizedWithArgs = "xmfhtmltok"u8.ToArray();
    public static readonly byte[] Image = "gumppic"u8.ToArray();
    public static readonly byte[] ImageTileButton = "buttontileart"u8.ToArray();
    public static readonly byte[] Tooltip = "tooltip"u8.ToArray();
    public static readonly byte[] ImageTiled = "gumppictiled"u8.ToArray();
    public static readonly byte[] Item = "tilepic"u8.ToArray();
    public static readonly byte[] ItemHued = "tilepichue"u8.ToArray();
    public static readonly byte[] ItemProperty = "itemproperty"u8.ToArray();
    public static readonly byte[] Label = "text"u8.ToArray();
    public static readonly byte[] LabelCropped = "croppedtext"u8.ToArray();
    public static readonly byte[] MasterGump = "mastergump"u8.ToArray();
    public static readonly byte[] Page = "page"u8.ToArray();
    public static readonly byte[] Radio = "radio"u8.ToArray();
    public static readonly byte[] SpriteImage = "picinpic"u8.ToArray();
    public static readonly byte[] TextEntry = "textentry"u8.ToArray();
    public static readonly byte[] TextEntryLimited = "textentrylimited"u8.ToArray();
}
