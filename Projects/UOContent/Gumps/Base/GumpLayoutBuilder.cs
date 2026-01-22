/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpLayoutBuilder.cs                                            *
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
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Text;

namespace Server.Gumps;

public ref struct GumpLayoutBuilder
{
    private static readonly byte[] _staticLayoutBuffer = GC.AllocateUninitializedArray<byte>(0x80000);

    private byte[] _layoutBuffer;
    private int _bytesWritten;

    internal GumpFlags _flags;
    internal int _switches;
    internal int _textEntries;

    internal Span<byte> LayoutData => _layoutBuffer.AsSpan(0, _bytesWritten);

    // This struct uses static fields and is therefore not thread safe!
    // Do not instantiate/process multiple gumps at the same time!
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpLayoutBuilder() => _layoutBuffer = _staticLayoutBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoClose()
    {
        if ((_flags & GumpFlags.NoClose) == 0)
        {
            GrowIfNeeded(11);
            Write("{ noclose }"u8);
            _flags |= GumpFlags.NoClose;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoMove()
    {
        if ((_flags & GumpFlags.NoMove) == 0)
        {
            GrowIfNeeded(10);
            Write("{ nomove }"u8);
            _flags |= GumpFlags.NoMove;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoResize()
    {
        if ((_flags & GumpFlags.NoResize) == 0)
        {
            GrowIfNeeded(12);
            Write("{ noresize }"u8);
            _flags |= GumpFlags.NoResize;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoDispose()
    {
        if ((_flags & GumpFlags.NoDispose) == 0)
        {
            GrowIfNeeded(13);
            Write("{ nodispose }"u8);
            _flags |= GumpFlags.NoDispose;
        }
    }

    public void AddAlphaRegion(int x, int y, int width, int height)
    {
        GrowIfNeeded(8 + 12 + 36);
        WriteStart("checkertrans"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteEnd();
    }

    public void AddBackground(int x, int y, int width, int height, int gumpId)
    {
        GrowIfNeeded(9 + 9 + 45);
        WriteStart("resizepic"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(gumpId);
        WriteValue(width);
        WriteValue(height);
        WriteEnd();
    }

    public void AddButton(
        int x, int y, int normalId, int pressedId, int buttonId, GumpButtonType type = GumpButtonType.Reply, int param = 0
    )
    {
        GrowIfNeeded(11 + 6 + 54 + 2);
        WriteStart("button"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(normalId);
        WriteValue(pressedId);
        WriteValue(type == GumpButtonType.Reply);
        WriteValue(param);
        WriteValue(buttonId);
        WriteEnd();
    }

    public void AddCheckbox(int x, int y, int inactiveId, int activeId, bool selected, int switchId)
    {
        GrowIfNeeded(10 + 8 + 45 + 2);
        WriteStart("checkbox"u8);
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
        if (groupId == 1)
        {
            GrowIfNeeded(11);
            Write("{ group 1 }"u8);
            return;
        }

        GrowIfNeeded(5 + 7 + 9);
        WriteStart("group"u8);
        WriteValue(groupId);
        WriteEnd();
    }

    public int AddHtmlPlaceholder(int x, int y, int width, int height,
        bool background = false, bool scrollbar = false)
    {
        GrowIfNeeded(11 + 8 + 36 + 10);
        WriteStart("htmlgump"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        var position = _bytesWritten;
        Write("      "u8);
        WriteValue(background);
        WriteValue(scrollbar);
        WriteEnd();

        return position;
    }

    public void AddHtml(int x, int y, int width, int height, int text,
        bool background = false, bool scrollbar = false)
    {
        GrowIfNeeded(11 + 8 + 45 + 4);
        WriteStart("htmlgump"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(text);
        WriteValue(background);
        WriteValue(scrollbar);
        WriteEnd();
    }

    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, bool background = false, bool scrollbar = false
    )
    {
        GrowIfNeeded(11 + 11 + 45 + 4);
        WriteStart("xmfhtmlgump"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(number);
        WriteValue(background);
        WriteValue(scrollbar);
        WriteEnd();
    }

    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, int color, bool background = false, bool scrollbar = false
    )
    {
        GrowIfNeeded(12 + 16 + 45 + 5 + 4);
        WriteStart("xmfhtmlgumpcolor"u8);
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
        GrowIfNeeded(12 + 10 + 45 + 5 + 4 + (args.Length > 0 ? 3 + args.Length : 0));
        WriteStart("xmfhtmltok"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(background);
        WriteValue(scrollbar);
        WriteValue(color);
        WriteValue(number);

        if (args.Length > 0)
        {
            Write("@"u8);
            WriteValue(args);
            Write("@ "u8);
        }
        WriteEnd();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, ref RawInterpolatedStringHandler handler, int color,
        bool background = false, bool scrollbar = false
    )
    {
        AddHtmlLocalized(x, y, width, height, number, handler.Text, color, background, scrollbar);

        // Dispose of the handler
        handler.Clear();
    }

    public void AddImage(int x, int y, int gumpId, int hue = 0, ReadOnlySpan<char> cls = default)
    {
        GrowIfNeeded(7 + 7 + 36 + (hue != 0 ? 14 : 0) + (cls.Length > 0 ? 7 + cls.Length : 0));
        WriteStart("gumppic"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(gumpId);

        if (hue != 0)
        {
            Write("hue="u8);
            WriteValue(hue);
            Write(" "u8);
        }

        if (!cls.IsEmpty)
        {
            Write("class="u8);
            WriteValue(cls);
            Write(" "u8);
        }

        WriteEnd();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImage(int x, int y, int gumpId, ref RawInterpolatedStringHandler handler)
    {
        AddImage(x, y, gumpId, 0, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImage(int x, int y, int gumpId, int hue, ref RawInterpolatedStringHandler handler)
    {
        AddImage(x, y, gumpId, hue, handler.Text);
        handler.Clear();
    }

    public void AddImageTiledButton(int x, int y, int normalId, int pressedId, int buttonId, GumpButtonType type, int param,
        int itemId, int hue, int width, int height, int localizedTooltip = -1)
    {
        GrowIfNeeded(15 + 13 + 90 + 2);
        WriteStart("buttontileart"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(normalId);
        WriteValue(pressedId);
        WriteValue(type == GumpButtonType.Reply);
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

        AddTooltip(localizedTooltip);
    }

    public void AddImageTiled(int x, int y, int width, int height, int gumpId)
    {
        GrowIfNeeded(9 + 12 + 45);
        WriteStart("gumppictiled"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(gumpId);
        WriteEnd();
    }

    public void AddItem(int x, int y, int itemId, int hue = 0)
    {
        GrowIfNeeded(7 + 36 + (hue != 0 ? 20 : 7));
        WriteStart(hue == 0 ? "tilepic"u8 : "tilepichue"u8);
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
        GrowIfNeeded(5 + 12 + 9);
        WriteStart("itemproperty"u8);
        WriteValue(serial.Value);
        WriteEnd();
    }

    public int AddLabelPlaceholder(int x, int y, int hue)
    {
        GrowIfNeeded(8 + 4 + 27 + 6);
        WriteStart("text"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(hue);
        var position = _bytesWritten;
        Write("      "u8);
        WriteEnd();
        return position;
    }

    public void AddLabel(int x, int y, int hue, int text)
    {
        GrowIfNeeded(8 + 4 + 36 + 6);
        WriteStart("text"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(hue);
        WriteValue(text);
        WriteEnd();
    }

    public int AddLabelCroppedPlaceholder(int x, int y, int width, int height, int hue)
    {
        GrowIfNeeded(10 + 11 + 45 + 6);
        WriteStart("croppedtext"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        var position = _bytesWritten;
        Write("      "u8);
        WriteEnd();
        return position;
    }

    public void AddLabelCropped(int x, int y, int width, int height, int hue, int text)
    {
        GrowIfNeeded(10 + 11 + 54 + 6);
        WriteStart("croppedtext"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValue(text);
        WriteEnd();
    }

    public void AddGumpIdOverride(int gumpId)
    {
        GrowIfNeeded(5 + 10 + 9);
        WriteStart("mastergump"u8);
        WriteValue(gumpId);
        WriteEnd();
    }

    public void AddPage(int page = 0)
    {
        if (page == 0)
        {
            GrowIfNeeded(10);
            Write("{ page 0 }"u8);
            return;
        }

        GrowIfNeeded(5 + 4 + 9);
        WriteStart("page"u8);
        WriteValue(page);
        WriteEnd();
    }

    public void AddRadio(int x, int y, int inactiveId, int activeId, bool selected, int switchId)
    {
        GrowIfNeeded(10 + 5 + 45 + 2);
        WriteStart("radio"u8);
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
        GrowIfNeeded(11 + 8 + 63);
        WriteStart("picinpic"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(gumpId);
        WriteValue(width);
        WriteValue(height);
        WriteValue(sx);
        WriteValue(sy);
        WriteEnd();
    }

    public int AddTextEntryPlaceholder(
        int x, int y, int width, int height, int hue, int entryId
    )
    {
        GrowIfNeeded(11 + 9 + 54 + 6);
        WriteStart("textentry"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValue(entryId);
        var position = _bytesWritten;
        Write("      "u8);
        WriteEnd();

        _textEntries++;
        return position;
    }

    public void AddTextEntry(
        int x, int y, int width, int height, int hue, int entryId, int initialText
    )
    {
        GrowIfNeeded(11 + 9 + 63 + 6);
        WriteStart("textentry"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValue(entryId);
        WriteValue(initialText);
        WriteEnd();

        _textEntries++;
    }

    public int AddTextEntryLimitedPlaceholder(
        int x, int y, int width, int height, int hue, int entryId, int size = 0
    )
    {
        GrowIfNeeded(12 + 16 + 63 + 6);
        WriteStart("textentrylimited"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValue(entryId);
        var position = _bytesWritten;
        Write("      "u8);
        WriteValue(size);
        WriteEnd();

        _textEntries++;
        return position;
    }

    public void AddTextEntryLimited(
        int x, int y, int width, int height, int hue, int entryId, int initialText, int size = 0
    )
    {
        GrowIfNeeded(12 + 16 + 72);
        WriteStart("textentrylimited"u8);
        WriteValue(x);
        WriteValue(y);
        WriteValue(width);
        WriteValue(height);
        WriteValue(hue);
        WriteValue(entryId);
        WriteValue(initialText);
        WriteValue(size);
        WriteEnd();

        _textEntries++;
    }

    public void AddTooltip(int number)
    {
        GrowIfNeeded(5 + 7 + 9);
        WriteStart("tooltip"u8);
        WriteValue(number);
        WriteEnd();
    }

    public void AddTooltip(int number, ReadOnlySpan<char> args)
    {
        GrowIfNeeded(5 + 7 + 9 + (args.Length > 0 ? 3 + args.Length : 0));
        WriteStart("tooltip"u8);
        WriteValue(number);

        if (args.Length > 0)
        {
            Write("@"u8);
            WriteValue(args);
            Write("@ "u8);
        }
        WriteEnd();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteStart(ReadOnlySpan<byte> value)
    {
        Write("{ "u8);
        Write(value);
        Write(" "u8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEnd() => Write("}"u8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteValue(bool value) => Write(value ? "1 "u8 : "0 "u8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteValue(ReadOnlySpan<char> value)
    {
        if (!value.IsEmpty)
        {
            _bytesWritten += value.GetBytesLatin1(_layoutBuffer.AsSpan(_bytesWritten));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteValue<T>(T value, ReadOnlySpan<char> format = default) where T : IUtf8SpanFormattable
    {
        if (!value.TryFormat(_layoutBuffer.AsSpan(_bytesWritten), out var bytesWritten, format, null))
        {
            throw new InvalidOperationException($"Failed to format '{value}' with the given format '{format}'");
        }

        _bytesWritten += bytesWritten;
        Write(" "u8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write(ReadOnlySpan<byte> span)
    {
        span.CopyTo(_layoutBuffer.AsSpan(_bytesWritten));
        _bytesWritten += span.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void FinalizeLayout()
    {
        GrowIfNeeded(1);
        _layoutBuffer[_bytesWritten++] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowIfNeeded(int count)
    {
        if (_bytesWritten + count <= _layoutBuffer.Length)
        {
            return;
        }

        var newSize = Math.Max(_bytesWritten + count, _layoutBuffer.Length * 2);
        var poolArray = STArrayPool<byte>.Shared.Rent(newSize);

        _layoutBuffer.AsSpan(0, _bytesWritten).CopyTo(poolArray);

        var toReturn = _layoutBuffer;
        _layoutBuffer = poolArray;

        if (toReturn != _staticLayoutBuffer)
        {
            STArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_layoutBuffer != _staticLayoutBuffer)
        {
            STArrayPool<byte>.Shared.Return(_layoutBuffer);
        }

        this = default;
    }
}
