/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpInterpolatedStringHandler.cs                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace Server.Gumps.Components.Interpolation;

[InterpolatedStringHandler]
public ref struct GumpInterpolatedStringHandler<TStringHandler, TFormatter>
    where TStringHandler : struct, IStringsHandler
    where TFormatter : struct, IGumpInterpolationTextFormatter<TFormatter>
{
    private static readonly char[] _buffer = HandlerFields.Buffer;

    private MemoryExtensions.TryWriteInterpolatedStringHandler _handler;
    private readonly TFormatter _formatter;

    public bool Success => HandlerFields.GetHandlerSuccessStatus(ref _handler);

    public GumpInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
    {
        _handler = new(literalLength, formattedCount, _buffer, out isEnabled);
        _formatter = TFormatter.Create();

        string? prefix = _formatter.Begin;

        if (prefix is not null)
        {
            _handler.AppendLiteral(prefix);
        }
    }

    public GumpInterpolatedStringHandler(int literalLength, int formattedCount, int color, out bool isEnabled)
    {
        _handler = new(literalLength, formattedCount, _buffer, out isEnabled);
        _formatter = TFormatter.Create(color);

        string? prefix = _formatter.Begin;

        if (prefix is not null)
        {
            _handler.AppendLiteral(prefix);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendLiteral(string value)
        => _handler.AppendLiteral(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<T>(T value)
        => _handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<T>(T value, string? format)
        => _handler.AppendFormatted(value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<T>(T value, int alignment)
        => _handler.AppendFormatted(value, alignment);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<T>(T value, int alignment, string? format)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(scoped ReadOnlySpan<char> value)
        => _handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(string? value)
        => _handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(string? value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(object? value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> ToSpanAndClose()
    {
        string? suffix = _formatter.End;

        if (suffix is not null)
        {
            _handler.AppendLiteral(suffix);
        }

        return _buffer.AsSpan(..HandlerFields.GetHandlerBufferPosition(ref _handler));
    }
}

static file class HandlerFields
{
    public static readonly char[] Buffer = GC.AllocateUninitializedArray<char>(1024);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_pos")]
    public static extern ref int GetHandlerBufferPosition(ref MemoryExtensions.TryWriteInterpolatedStringHandler @this);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_success")]
    public static extern ref bool GetHandlerSuccessStatus(ref MemoryExtensions.TryWriteInterpolatedStringHandler @this);
}
