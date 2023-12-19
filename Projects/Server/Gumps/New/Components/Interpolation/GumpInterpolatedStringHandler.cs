using Server.Gumps.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Server.Gumps.Components.Interpolation.GumpInterpolatedStringHandler;

namespace Server.Gumps.Components.Interpolation
{
    [InterpolatedStringHandler]
    public ref struct GumpInterpolatedStringHandler<TStringHandler, TFormatter>
        where TStringHandler : struct, IStringsHandler
        where TFormatter : struct, IGumpInterpolationTextFormatter<TFormatter>
    {
        private static readonly char[] buffer = GC.AllocateUninitializedArray<char>(1024);

        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "mutable struct")]
        private MemoryExtensions.TryWriteInterpolatedStringHandler handler;
        private readonly TFormatter formatter;

        public readonly bool Success => HandlerFields.GetHandlerSuccessStatus(in handler);

        public GumpInterpolatedStringHandler(int literalLength, int formattedCount, out bool isEnabled)
        {
            handler = new(literalLength, formattedCount, buffer, out isEnabled);
            formatter = TFormatter.Create();

            string? prefix = formatter.Begin;
            if (prefix is not null)
                handler.AppendLiteral(prefix);
        }

        public GumpInterpolatedStringHandler(int literalLength, int formattedCount, int color, out bool isEnabled)
        {
            handler = new(literalLength, formattedCount, buffer, out isEnabled);
            formatter = TFormatter.Create(color);

            string? prefix = formatter.Begin;
            if (prefix is not null)
                handler.AppendLiteral(prefix);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendLiteral(string value)
            => handler.AppendLiteral(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<T>(T value)
            => handler.AppendFormatted(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<T>(T value, string? format)
            => handler.AppendFormatted(value, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<T>(T value, int alignment)
            => handler.AppendFormatted(value, alignment);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted<T>(T value, int alignment, string? format)
            => handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(scoped ReadOnlySpan<char> value)
            => handler.AppendFormatted(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
            => handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(string? value)
            => handler.AppendFormatted(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(string? value, int alignment = 0, string? format = null)
            => handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AppendFormatted(object? value, int alignment = 0, string? format = null)
            => handler.AppendFormatted(value, alignment, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> ToSpanAndClose()
        {
            string? suffix = formatter.End;

            if (suffix is not null)
                handler.AppendLiteral(suffix);
            
            return buffer.AsSpan(..HandlerFields.GetHandlerBufferPosition(in handler));
        }
    }

    file static class HandlerFields
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_pos")]
        public extern static ref int GetHandlerBufferPosition(in MemoryExtensions.TryWriteInterpolatedStringHandler @this);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_success")]
        public extern static ref bool GetHandlerSuccessStatus(in MemoryExtensions.TryWriteInterpolatedStringHandler @this);
    }
}
