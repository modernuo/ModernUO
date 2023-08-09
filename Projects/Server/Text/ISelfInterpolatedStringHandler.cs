/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ISelfInterpolatedStringHandler.cs                               *
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

namespace Server.Text;

public interface ISelfInterpolatedStringHandler
{
    public void Add([InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler);
    public void InitializeInterpolation(int literalLength, int formattedCount);
    public void AppendLiteral(string value);
    public void AppendFormatted<T>(T value);
    public void AppendFormatted<T>(T value, string? format);
    public void AppendFormatted<T>(T value, int alignment);
    public void AppendFormatted<T>(T value, int alignment, string? format);
    public void AppendFormatted(ReadOnlySpan<char> value);
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format = null);
    public void AppendFormatted(object? value, int alignment = 0, string? format = null);
    public void AppendFormatted(string? value);
    public void AppendFormatted(string? value, int alignment, string? format = null);

    [InterpolatedStringHandler]
    public ref struct InterpolatedStringHandler
    {
        private ISelfInterpolatedStringHandler _parent;

        public InterpolatedStringHandler(int literalLength, int formattedCount, ISelfInterpolatedStringHandler parent)
        {
            _parent = parent;
            _parent.InitializeInterpolation(literalLength, formattedCount);
        }

        public void AppendLiteral(string value) => _parent.AppendLiteral(value);

        public void AppendFormatted<T>(T value) => _parent.AppendFormatted(value);

        public void AppendFormatted<T>(T value, string? format) => _parent.AppendFormatted(value, format);

        public void AppendFormatted<T>(T value, int alignment) => _parent.AppendFormatted(value, alignment);

        public void AppendFormatted<T>(T value, int alignment, string? format) =>
            _parent.AppendFormatted(value, alignment, format);

        public void AppendFormatted(ReadOnlySpan<char> value) => _parent.AppendFormatted(value);

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format = null) =>
            _parent.AppendFormatted(value, alignment, format);

        public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
            _parent.AppendFormatted(value, alignment, format);

        public void AppendFormatted(string? value) => _parent.AppendFormatted(value);

        public void AppendFormatted(string? value, int alignment, string? format = null) =>
            _parent.AppendFormatted(value, alignment, format);
    }
}
