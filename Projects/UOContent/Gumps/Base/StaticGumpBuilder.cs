/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StaticGumpBuilder.cs                                            *
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
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Text;

namespace Server.Gumps;

public ref struct StaticGumpBuilder
{
    private static readonly byte[] _staticStringsBuffer = GC.AllocateUninitializedArray<byte>(0x80000);

    // Position in the layout and the hash of the keyslot
    private static readonly (ulong, int)[] _stringSlotOffsets = GC.AllocateUninitializedArray<(ulong, int)>(0x8000); // Assume max 65535 strings

    private byte[] _stringsBuffer;
    private int _stringBytesWritten;
    internal int _stringsCount;
    private GumpLayoutBuilder _gumpBuilder;

    private int _stringOffsetCount;

    internal ReadOnlySpan<byte> LayoutData => _gumpBuilder.LayoutData;

    internal ReadOnlySpan<byte> StringsData => _stringsBuffer.AsSpan(0, _stringBytesWritten);

    internal ReadOnlySpan<(ulong, int)> StringSlotOffsets => _stringSlotOffsets.AsSpan(0, _stringOffsetCount);

    public int Switches => _gumpBuilder._switches;
    public int TextEntries => _gumpBuilder._textEntries;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StaticGumpBuilder()
    {
        _stringsBuffer = _staticStringsBuffer;

        // For corner cases where slots are not filled, reserve the first string index for empty
        _stringsBuffer.AsSpan(0, 2).Clear();
        _stringBytesWritten = 2;
        _stringsCount = 1;

        _gumpBuilder = new GumpLayoutBuilder();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoClose() => _gumpBuilder.SetNoClose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoMove() => _gumpBuilder.SetNoMove();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoResize() => _gumpBuilder.SetNoResize();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNoDispose() => _gumpBuilder.SetNoDispose();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddAlphaRegion(int x, int y, int width, int height) =>
        _gumpBuilder.AddAlphaRegion(x, y, width, height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddBackground(int x, int y, int width, int height, int gumpID) =>
        _gumpBuilder.AddBackground(x, y, width, height, gumpID);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddButton(
        int x, int y, int normalID, int pressedId, int buttonId, GumpButtonType type = GumpButtonType.Reply, int param = 0
    ) => _gumpBuilder.AddButton(x, y, normalID, pressedId, buttonId, type, param);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddCheckbox(int x, int y, int inactiveID, int activeID, bool selected, int switchId) =>
        _gumpBuilder.AddCheckbox(x, y, inactiveID, activeID, selected, switchId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddGroup(int groupId) => _gumpBuilder.AddGroup(groupId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlPlaceholder(
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<char> slotKey,
        bool background = false,
        bool scrollbar = false
    )
    {
        var index = _gumpBuilder.AddHtmlPlaceholder(x, y, width, height, background, scrollbar);
        WriteInternalizedStringSlot(slotKey, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlPlaceholder(
        int x,
        int y,
        int width,
        int height,
        ref RawInterpolatedStringHandler handler,
        bool background = false,
        bool scrollbar = false
    )
    {
        AddHtmlPlaceholder(x, y, width, height, handler.Text, background, scrollbar);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtml(
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<char> text,
        bool background = false,
        bool scrollbar = false
    )
    {
        WriteInternalizedString(text);
        _gumpBuilder.AddHtml(x, y, width, height, _stringsCount++, background, scrollbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtml(
        int x, int y, int width, int height, ref RawInterpolatedStringHandler handler,
        bool background = false, bool scrollbar = false
    )
    {
        AddHtml(x, y, width, height, handler.Text, background, scrollbar);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtml(
        int x,
        int y,
        int width,
        int height,
        int color,
        ReadOnlySpan<char> text,
        bool background = false,
        bool scrollbar = false
    )
    {
        var handler = text.Color(color);
        AddHtml(x, y, width, height, ref handler, background, scrollbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtml(
        int x,
        int y,
        int width,
        int height,
        int color,
        ref RawInterpolatedStringHandler handler,
        bool background = false,
        bool scrollbar = false
    )
    {
        AddHtml(x, y, width, height, color, handler.Text, background, scrollbar);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlCentered(
        int x, int y, int width, int height, ReadOnlySpan<char> text, bool background = false, bool scrollbar = false
    )
    {
        var handler = text.Center();
        AddHtml(x, y, width, height, ref handler, background, scrollbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlCentered(
        int x,
        int y,
        int width,
        int height,
        ref RawInterpolatedStringHandler handler,
        bool background = false,
        bool scrollbar = false
    )
    {
        var centerHandler = handler.Text.Center();
        AddHtml(x, y, width, height, ref centerHandler, background, scrollbar);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlCentered(
        int x,
        int y,
        int width,
        int height,
        int color,
        ReadOnlySpan<char> text,
        bool background = false,
        bool scrollbar = false
    )
    {
        var handler = text.Center(color);
        AddHtml(x, y, width, height,  ref handler, background, scrollbar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlCentered(
        int x,
        int y,
        int width,
        int height,
        int color,
        ref RawInterpolatedStringHandler handler,
        bool background = false,
        bool scrollbar = false
    )
    {
        AddHtmlCentered(x, y, width, height, color, handler.Text, background, scrollbar);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, bool background = false, bool scrollbar = false
    ) => _gumpBuilder.AddHtmlLocalized(x, y, width, height, number, background, scrollbar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, int color, bool background = false, bool scrollbar = false
    ) => _gumpBuilder.AddHtmlLocalized(x, y, width, height, number, color, background, scrollbar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlLocalized(
        int x,
        int y,
        int width,
        int height,
        int number,
        ReadOnlySpan<char> args,
        int color,
        bool background = false,
        bool scrollbar = false
    ) => _gumpBuilder.AddHtmlLocalized(x, y, width, height, number, args, color, background, scrollbar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddHtmlLocalized(
        int x, int y, int width, int height, int number, ref RawInterpolatedStringHandler handler, int color,
        bool background = false, bool scrollbar = false
    ) => _gumpBuilder.AddHtmlLocalized(x, y, width, height, number, ref handler, color, background, scrollbar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImage(int x, int y, int gumpId, int hue = 0, ReadOnlySpan<char> cls = default) =>
        _gumpBuilder.AddImage(x, y, gumpId, hue, cls);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImage(int x, int y, int gumpId, ref RawInterpolatedStringHandler handler) =>
        _gumpBuilder.AddImage(x, y, gumpId, ref handler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImage(int x, int y, int gumpId, int hue, ref RawInterpolatedStringHandler handler) =>
        _gumpBuilder.AddImage(x, y, gumpId, hue, ref handler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImageTiledButton(
        int x,
        int y,
        int normalId,
        int pressedId,
        int buttonId,
        GumpButtonType type,
        int param,
        int itemId,
        int hue,
        int width,
        int height,
        int localizedTooltip = -1
    ) => _gumpBuilder.AddImageTiledButton(
        x, y, normalId, pressedId, buttonId, type, param, itemId, hue, width, height, localizedTooltip
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddImageTiled(int x, int y, int width, int height, int gumpId) =>
        _gumpBuilder.AddImageTiled(x, y, width, height, gumpId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddItem(int x, int y, int itemId, int hue = 0) => _gumpBuilder.AddItem(x, y, itemId, hue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddItemProperty(Serial serial) => _gumpBuilder.AddItemProperty(serial);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabelPlaceholder(int x, int y, int hue, ReadOnlySpan<char> slotKey)
    {
        var index =  _gumpBuilder.AddLabelPlaceholder(x, y, hue);
        WriteInternalizedStringSlot(slotKey, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabelPlaceholder(int x, int y, int hue, ref RawInterpolatedStringHandler handler)
    {
        AddLabelPlaceholder(x, y, hue, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabel(int x, int y, int hue, ReadOnlySpan<char> text)
    {
        WriteInternalizedString(text);
        _gumpBuilder.AddLabel(x, y, hue, _stringsCount++);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabel(int x, int y, int hue, ref RawInterpolatedStringHandler handler)
    {
        AddLabel(x, y, hue, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabelCroppedPlaceholder(int x, int y, int width, int height, int hue, ref RawInterpolatedStringHandler handler)
    {
        AddLabelCroppedPlaceholder(x, y, width, height, hue, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabelCroppedPlaceholder(int x, int y, int width, int height, int hue, ReadOnlySpan<char> slotKey)
    {
        var index = _gumpBuilder.AddLabelCroppedPlaceholder(x, y, width, height, hue);
        WriteInternalizedStringSlot(slotKey, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabelCropped(int x, int y, int width, int height, int hue, ReadOnlySpan<char> text)
    {
        WriteInternalizedString(text);
        _gumpBuilder.AddLabelCropped(x, y, width, height, hue, _stringsCount++);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLabelCropped(int x, int y, int width, int height, int hue, ref RawInterpolatedStringHandler handler)
    {
        AddLabelCropped(x, y, width, height, hue, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddGumpIdOverride(int gumpId) => _gumpBuilder.AddGumpIdOverride(gumpId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddPage(int page = 0) => _gumpBuilder.AddPage(page);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRadio(int x, int y, int inactiveId, int activeId, bool selected, int switchId) =>
        _gumpBuilder.AddRadio(x, y, inactiveId, activeId, selected, switchId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSpriteImage(int x, int y, int gumpId, int width, int height, int sx, int sy) =>
        _gumpBuilder.AddSpriteImage(x, y, gumpId, width, height, sx, sy);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntryPlaceholder(
        int x, int y, int width, int height, int hue, int entryId, ref RawInterpolatedStringHandler handler
    )
    {
        AddTextEntryPlaceholder(x, y, width, height, hue, entryId, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntryPlaceholder(int x, int y, int width, int height, int hue, int entryId, ReadOnlySpan<char> slotKey)
    {
        var index = _gumpBuilder.AddTextEntryPlaceholder(x, y, width, height, hue, entryId);
        WriteInternalizedStringSlot(slotKey, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntry(
        int x, int y, int width, int height, int hue, int entryId, ReadOnlySpan<char> initialText = default
    )
    {
        WriteInternalizedString(initialText);
        _gumpBuilder.AddTextEntry(x, y, width, height, hue, entryId, _stringsCount++);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntry(
        int x, int y, int width, int height, int hue, int entryId, ref RawInterpolatedStringHandler handler
    )
    {
        AddTextEntry(x, y, width, height, hue, entryId, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntryLimitedPlaceholder(
        int x, int y, int width, int height, int hue, int entryId, ReadOnlySpan<char> slotKey, int size = 0
    )
    {
        var index = _gumpBuilder.AddTextEntryLimitedPlaceholder(x, y, width, height, hue, entryId, size);
        WriteInternalizedStringSlot(slotKey, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntryLimitedPlaceholder(
        int x, int y, int width, int height, int hue, int entryId, ref RawInterpolatedStringHandler handler, int size = 0
    )
    {
        AddTextEntryLimitedPlaceholder(x, y, width, height, hue, entryId, handler.Text, size);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntryLimited(
        int x, int y, int width, int height, int hue, int entryId, ReadOnlySpan<char> initialText = default, int size = 0
    )
    {
        WriteInternalizedString(initialText);
        _gumpBuilder.AddTextEntryLimited(x, y, width, height, hue, entryId, _stringsCount++, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTextEntryLimited(
        int x, int y, int width, int height, int hue, int entryId, ref RawInterpolatedStringHandler handler, int size = 0
    )
    {
        AddTextEntryLimited(x, y, width, height, hue, entryId, handler.Text, size);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTooltip(int number) => _gumpBuilder.AddTooltip(number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTooltip(int number, ReadOnlySpan<char> args) => _gumpBuilder.AddTooltip(number, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTooltip(int number, ref RawInterpolatedStringHandler handler)
    {
        _gumpBuilder.AddTooltip(number, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FinalizeLayout() => _gumpBuilder.FinalizeLayout();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteInternalizedString(ReadOnlySpan<char> text)
    {
        if (text.Length > ushort.MaxValue)
        {
            text = text[..ushort.MaxValue];
        }

        GrowStringsBufferIfNeeded(2 + text.Length * 2);
        BinaryPrimitives.WriteUInt16BigEndian(_stringsBuffer.AsSpan(_stringBytesWritten), (ushort)text.Length);

        _stringBytesWritten += 2;
        _stringBytesWritten += text.GetBytesBigUni(_stringsBuffer.AsSpan(_stringBytesWritten));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowStringsBufferIfNeeded(int needed)
    {
        if (needed + _stringBytesWritten <= _stringsBuffer.Length)
        {
            return;
        }

        var newSize = Math.Max(_stringBytesWritten + needed, _stringsBuffer.Length * 2);
        byte[] poolArray = STArrayPool<byte>.Shared.Rent(newSize);

        _stringsBuffer.AsSpan(0, _stringBytesWritten).CopyTo(poolArray);

        byte[] toReturn = _stringsBuffer;
        _stringsBuffer = poolArray;

        if (toReturn != _staticStringsBuffer)
        {
            STArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteInternalizedStringSlot(ReadOnlySpan<char> slotKey, int index)
    {
        var hash = HashUtility.ComputeHash64(slotKey);
        _stringSlotOffsets[_stringOffsetCount++] = (hash, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteSlotIndex(int offset, int index)
    {
        // Left padded final index for the slot
        index.TryFormat(_gumpBuilder.LayoutData[offset..], out _, "00000");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _gumpBuilder.Dispose();

        if (_stringsBuffer != _staticStringsBuffer)
        {
            STArrayPool<byte>.Shared.Return(_stringsBuffer);
        }

        this = default;
    }
}
