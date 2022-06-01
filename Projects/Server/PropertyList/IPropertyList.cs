/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IPropertyList.cs                                                *
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

namespace Server;

public interface IPropertyList
{
    public void Reset();
    public void Terminate();
    // TODO: Add custom string interpolator
    public void Add(int number, string arguments = null);
    public void Add(string text);

    // String Interpolator
    public void Add([InterpolatedStringHandlerArgument("")] ref PropertyListInterpolatedStringHandler handler);
    public void Add(int number, [InterpolatedStringHandlerArgument("")] ref PropertyListInterpolatedStringHandler handler);
    public void InitializePropertyListInterpolation(int literalLength, int formattedCount);
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
    public ref struct PropertyListInterpolatedStringHandler
    {
        private IPropertyList _propertyList;

        public PropertyListInterpolatedStringHandler(int literalLength, int formattedCount, IPropertyList propertyList)
        {
            _propertyList = propertyList;
            _propertyList.InitializePropertyListInterpolation(literalLength, formattedCount);
        }

        public void AppendLiteral(string value) => _propertyList.AppendLiteral(value);

        public void AppendFormatted<T>(T value) => _propertyList.AppendFormatted(value);

        public void AppendFormatted<T>(T value, string? format) => _propertyList.AppendFormatted(value, format);

        public void AppendFormatted<T>(T value, int alignment) => _propertyList.AppendFormatted(value, alignment);

        public void AppendFormatted<T>(T value, int alignment, string? format) =>
            _propertyList.AppendFormatted(value, alignment, format);

        public void AppendFormatted(ReadOnlySpan<char> value) => _propertyList.AppendFormatted(value);

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format = null) =>
            _propertyList.AppendFormatted(value, alignment, format);

        public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
            _propertyList.AppendFormatted(value, alignment, format);

        public void AppendFormatted(string? value) => _propertyList.AppendFormatted(value);

        public void AppendFormatted(string? value, int alignment, string? format = null) =>
            _propertyList.AppendFormatted(value, alignment, format);
    }
}
