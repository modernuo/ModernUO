/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpHtmlBuilder.cs                                              *
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
using System.Drawing;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Text;

namespace Server.Gumps;

public enum TextAlignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2
}

public ref struct GumpHtmlBuilder
{
    private int _color;
    private byte _fontStyle;
    private sbyte _size;
    private TextAlignment _align;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Color(int color)
    {
        _color = color;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Color(Color color)
    {
        _color = color.ToArgb() & 0xFFFFFF;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Style(byte fontStyle)
    {
        _fontStyle = fontStyle;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Size(sbyte size)
    {
        _size = size;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Align(TextAlignment alignment)
    {
        _align = alignment;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Left() => Align(TextAlignment.Left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Center() => Align(TextAlignment.Center);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GumpHtmlBuilder Right() => Align(TextAlignment.Right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Build(
        ref DynamicGumpBuilder _builder,
        int x,
        int y,
        int width,
        int height,
        ref RawInterpolatedStringHandler handler,
        bool background = false,
        bool scrollbar = false
    )
    {
        Build(ref _builder, x, y, width, height, handler.Text, background, scrollbar);
        handler.Clear();
    }

    public void Build(
        ref DynamicGumpBuilder _builder,
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<char> text,
        bool background = false,
        bool scrollbar = false
    )
    {
        if (_align == TextAlignment.Left && _color == 0 && _size == -1 && _fontStyle == 0)
        {
            _builder.AddHtml(x, y, width, height, text, background, scrollbar);
            return;
        }

        var builder = ValueStringBuilder.Create(128);
        BuildHtml(text, ref builder);

        _builder.AddHtml(x, y, width, height, builder.AsSpan(true), background, scrollbar);
        builder.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Build(
        ref StaticGumpBuilder _builder,
        int x,
        int y,
        int width,
        int height,
        ref RawInterpolatedStringHandler handler,
        bool background = false,
        bool scrollbar = false
    )
    {
        Build(ref _builder, x, y, width, height, handler.Text, background, scrollbar);
        handler.Clear();
    }

    public void Build(
        ref StaticGumpBuilder _builder,
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<char> text,
        bool background = false,
        bool scrollbar = false
    )
    {
        if (_align == TextAlignment.Left && _color == 0 && _size == -1 && _fontStyle == 0)
        {
            _builder.AddHtml(x, y, width, height, text, background, scrollbar);
            return;
        }

        var builder = ValueStringBuilder.Create(128);
        BuildHtml(text, ref builder);

        _builder.AddHtml(x, y, width, height, builder.AsSpan(true), background, scrollbar);
        builder.Dispose();
    }

    public void BuildHtml(ReadOnlySpan<char> text, scoped ref ValueStringBuilder builder)
    {
        if (_align == TextAlignment.Right)
        {
            builder.Append("<RIGHT>");
        }
        else if (_align == TextAlignment.Center)
        {
            builder.Append("<CENTER>");
        }

        if (_color != 0 || _size > -1 || _fontStyle > 0)
        {
            builder.Append("<BASEFONT");

            if (_color != 0)
            {
                builder.Append(" COLOR=");
                builder.Append($"#{_color:X6}");
            }

            if (_size > -1)
            {
                builder.Append(" SIZE=");
                builder.Append(_size);
            }

            if (_fontStyle > 0)
            {
                builder.Append(" STYLE=");
                builder.Append(_fontStyle);
            }

            builder.Append('>');
            builder.Append(text);
            builder.Append("</BASEFONT>");
        }
        else
        {
            builder.Append(text);
        }

        if (_align == TextAlignment.Right)
        {
            builder.Append("</RIGHT>");
        }
        else if (_align == TextAlignment.Center)
        {
            builder.Append("</CENTER>");
        }
    }
}
