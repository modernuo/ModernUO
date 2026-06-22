/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: OplTextBlock.cs                                                 *
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
using Server.Text;

namespace Server;

// Accumulates '\n'-joined free-text lines and emits ONE cycling passthrough entry on dispose.
// Use with `using var block = list.TextBlock();`. ref struct: single-threaded OPL build only.
public ref struct OplTextBlock
{
    private readonly ObjectPropertyList _list;
    internal ValueStringBuilder _builder;
    private bool _any;

    internal OplTextBlock(ObjectPropertyList list)
    {
        _list = list;
        _builder = ValueStringBuilder.Create();
        _any = false;
    }

    public void Add(scoped ReadOnlySpan<char> line)
    {
        if (line.IsEmpty)
        {
            return;
        }

        _builder.Append(line);
        _builder.Append('\n', 1); // ValueStringBuilder has no single-char Append
        _any = true;
    }

    // Zero-alloc interpolated overload: block.Add($"Luck Bonus: +{v}%").
    public void Add([InterpolatedStringHandlerArgument("")] scoped ref OplInterpolationHandler handler)
    {
        var wrote = handler._wrote;
        this = handler._block; // reconcile possibly-grown builder
        if (wrote)
        {
            _builder.Append('\n', 1);
            _any = true;
        }
    }

    public void Dispose()
    {
        if (_any)
        {
            // Strip the trailing '\n' (Length >= 2 whenever _any: content + separator).
            _list.Add(_builder.AsSpan(0, _builder.Length - 1));
        }

        _builder.Dispose();
    }

    [InterpolatedStringHandler]
    public ref struct OplInterpolationHandler
    {
        internal OplTextBlock _block;
        internal bool _wrote;

        public OplInterpolationHandler(int literalLength, int formattedCount, OplTextBlock block)
        {
            _block = block;
            _wrote = false;
            _block._builder.EnsureCapacity(_block._builder.Length + literalLength + formattedCount * 11);
        }

        public void AppendLiteral(string value)
        {
            _block._builder.Append(value);
            _wrote = true;
        }

        public void AppendFormatted<T>(T value)
        {
            _block._builder.Append(value);
            _wrote = true;
        }

        public void AppendFormatted<T>(T value, string format)
        {
            _block._builder.Append(value, format);
            _wrote = true;
        }

        public void AppendFormatted(scoped ReadOnlySpan<char> value)
        {
            _block._builder.Append(value);
            _wrote = true;
        }
    }
}
