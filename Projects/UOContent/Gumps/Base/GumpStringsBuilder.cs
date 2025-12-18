/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpStringsBuilder.cs                                           *
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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Text;

namespace Server.Gumps;

public ref struct GumpStringsBuilder
{
    private static readonly byte[] _staticStringsBuffer = GC.AllocateUninitializedArray<byte>(0x80000);
    private static readonly Dictionary<ulong, int> _hashes = new();

    private readonly bool _finalizeLayout;
    private byte[] _stringsBuffer;
    private int _stringBytesWritten;
    internal int _stringsCount;

    internal ReadOnlySpan<byte> StringsBuffer => _stringsBuffer.AsSpan(0, _stringBytesWritten);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpStringsBuilder(bool finalizeLayout)
    {
        _finalizeLayout = finalizeLayout;
        _stringsBuffer = _staticStringsBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowStringsBufferIfNeeded(int needed)
    {
        var newLength = needed + _stringBytesWritten;
        if (newLength <= _stringsBuffer.Length)
        {
            return;
        }

        var newSize = Math.Max(newLength, _stringsBuffer.Length * 2);
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
    public void SetHtmlText(
        ref RawInterpolatedStringHandler slotKeyHandler, ref RawInterpolatedStringHandler handler, ReadOnlySpan<char> color = default, int size = -1,
        byte fontStyle = 0, TextAlignment align = TextAlignment.Left
    )
    {
        SetHtmlText(slotKeyHandler.Text, handler.Text, color, size, fontStyle, align);
        slotKeyHandler.Clear();
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHtmlText(
        ref RawInterpolatedStringHandler slotKeyHandler, ReadOnlySpan<char> value, ReadOnlySpan<char> color = default, int size = -1, byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left
    )
    {
        SetHtmlText(slotKeyHandler.Text, value, color, size, fontStyle, align);
        slotKeyHandler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHtmlText(
        ReadOnlySpan<char> slotKey, ref RawInterpolatedStringHandler handler, ReadOnlySpan<char> color = default, int size = -1, byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left
    )
    {
        SetHtmlText(slotKey, handler.Text, color, size, fontStyle, align);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHtmlText(
        ReadOnlySpan<char> slotKey, ReadOnlySpan<char> value, ReadOnlySpan<char> color = default, int size = -1, byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left
    )
    {
        var arr = STArrayPool<char>.Shared.Rent(Html.BuildCharCount(value, color));
        var bytesWritten = Html.Build(value, arr.AsSpan(), color, size, fontStyle, align);

        SetStringSlot(slotKey, arr.AsSpan(0, bytesWritten));
        STArrayPool<char>.Shared.Return(arr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringSlot(ReadOnlySpan<char> slotKey, ref RawInterpolatedStringHandler handler)
    {
        SetStringSlot(slotKey, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringSlot(ref RawInterpolatedStringHandler slotKeyHandler, ref RawInterpolatedStringHandler handler)
    {
        SetStringSlot(ref slotKeyHandler, handler.Text);
        handler.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringSlot(ref RawInterpolatedStringHandler slotKeyHandler, ReadOnlySpan<char> text)
    {
        SetStringSlot(slotKeyHandler.Text, text);
        slotKeyHandler.Clear();
    }

    public void SetStringSlot(ReadOnlySpan<char> slotKey, ReadOnlySpan<char> text)
    {
        var hash = HashUtility.ComputeHash64(slotKey);

        if (text.Length > ushort.MaxValue)
        {
            text = text[..ushort.MaxValue];
        }

        GrowStringsBufferIfNeeded(2 + text.Length * 2);
        BinaryPrimitives.WriteUInt16BigEndian(_stringsBuffer.AsSpan(_stringBytesWritten), (ushort)text.Length);
        _stringBytesWritten += 2;

        if (text.Length > 0)
        {
            _stringBytesWritten += text.GetBytesBigUni(_stringsBuffer.AsSpan(_stringBytesWritten));
        }

        if (_finalizeLayout)
        {
            _hashes[hash] = _stringsCount;
        }

        _stringsCount++;
    }

    public void FinalizeStrings(ref StaticGumpBuilder builder)
    {
        var startingIndex = builder._stringsCount;

        var stringSlotOffsets = builder.StringSlotOffsets;
        for (var i = 0; i < stringSlotOffsets.Length; i++)
        {
            var (hash, offset) = stringSlotOffsets[i];

            if (hash != 0 && _hashes.TryGetValue(hash, out var index))
            {
                builder.WriteSlotIndex(offset, index + startingIndex);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_finalizeLayout)
        {
            _hashes.Clear();
        }

        if (_stringsBuffer != _staticStringsBuffer)
        {
            STArrayPool<byte>.Shared.Return(_stringsBuffer);
        }
    }
}
