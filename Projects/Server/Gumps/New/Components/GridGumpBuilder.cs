/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridGumpBuilder.cs                                              *
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
using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using static Server.Gumps.Components.Interpolation.GumpInterpolationTextFormatters;

namespace Server.Gumps.Components;

public ref struct GridGumpBuilder<T> where T : struct, IStringsHandler
{
    private GumpBuilder<T> _builder;

    private readonly ushort _borderSize;
    private readonly ushort _offsetSize;
    private readonly ushort _entryHeight;
    private readonly ushort _offsetGumpId;
    private readonly ushort _headerGumpId;
    private readonly ushort _entryGumpId;
    private readonly ushort _backGumpId;
    private readonly ushort _textHue;
    private readonly ushort _textOffsetX;

    private Span<byte> _backgroundSpan;
    private Span<byte> _offsetSpan;
    private int _currentX;
    private int _currentY;
    private int _backgroundWidth;
    private int _offsetWidth;
    private ushort _currentPage;

    public readonly int CurrentX => _currentX;
    public readonly int CurrentY => _currentY;
    public readonly ReadOnlySpan<byte> Layout => _builder.Layout;
    public readonly int LayoutSize => _builder.LayoutSize;
    internal readonly int Switches => _builder.Switches;
    internal readonly int TextEntries => _builder.TextEntries;

    [UnscopedRef]
    public ref T StringsWriter => ref _builder.StringsWriter;

    public GridGumpBuilder(GumpFlags flags = GumpFlags.None, ushort borderSize = 10, ushort offsetSize = 1,
        ushort entryHeight = 20, ushort offsetGumpId = 0x0A40, ushort headerGumpId = 0x0E14, ushort entryGumpId = 0x0BBC,
        ushort backGumpId = 0x13BE, ushort textHue = 0, ushort textOffsetX = 2)
    {
        _builder = new GumpBuilder<T>(flags);

        _borderSize = borderSize;
        _offsetSize = offsetSize;
        _entryHeight = entryHeight;
        _offsetGumpId = offsetGumpId;
        _headerGumpId = headerGumpId;
        _entryGumpId = entryGumpId;
        _backGumpId = backGumpId;
        _textHue = textHue;
        _textOffsetX = textOffsetX;
    }

    public void FinishPage()
    {
        if (_backgroundWidth == 0)
        {
            return;
        }

        int backgroundHeight = _currentY + _entryHeight + _offsetSize + _borderSize;
        int offsetHeight = _currentY + _entryHeight + _offsetSize - _borderSize;

        Span<char> buffer = stackalloc char[46];

        buffer.TryWrite($"{{ resizepic 0 0 {_backGumpId} {_backgroundWidth} {backgroundHeight} }}", out int charsWritten);
        OperationStatus result = Ascii.FromUtf16(buffer[..charsWritten], _backgroundSpan, out int bytesWritten);
        _backgroundSpan[bytesWritten..].Fill((byte)' ');

        Debug.Assert(result == OperationStatus.Done);

        buffer.TryWrite($"{{ gumppictiled {_borderSize} {_borderSize} {_offsetWidth} {offsetHeight} {_offsetGumpId} }}", out charsWritten);
        result = Ascii.FromUtf16(buffer[..charsWritten], _offsetSpan, out bytesWritten);
        _offsetSpan[bytesWritten..].Fill((byte)' ');

        Debug.Assert(result == OperationStatus.Done);

        _backgroundWidth = 100;
        _offsetWidth = 100;
    }

    public void AddNewPage()
    {
        FinishPage();

        _currentX = _borderSize + _offsetSize;
        _currentY = _borderSize + _offsetSize;

        _builder.AddPage(++_currentPage);

        _backgroundSpan = _builder.Reserve(35);
        _offsetSpan = _builder.Reserve(46);
    }

    public void AddNewLine()
    {
        _currentY += _entryHeight + _offsetSize;
        _currentX = _borderSize + _offsetSize;
    }

    public void AddEntryLabel(int width, string text)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddLabelCropped(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, _textHue, text);

        IncreaseX(width);
    }

    public void AddEntryHtml(int width, string? text)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, text);

        IncreaseX(width);
    }

    public void AddEntryHtml(int width, ref GumpInterpolatedStringHandler<T, None> handler)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, ref handler);

        IncreaseX(width);
    }

    public void AddEntryHtml(int width, int color, string? text)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, text);

        IncreaseX(width);
    }

    public void AddEntryHtml(int width, int color,
        [InterpolatedStringHandlerArgument(nameof(color))] scoped ref GumpInterpolatedStringHandler<T, Colored> handler)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, ref handler);

        IncreaseX(width);
    }

    public void AddEntryHtmlCentered(int width, string? text)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, text);

        IncreaseX(width);
    }

    public void AddEntryHtmlCentered(int width, ref GumpInterpolatedStringHandler<T, Centered> handler)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, ref handler);

        IncreaseX(width);
    }

    public void AddEntryHtmlCentered(int width, int color, string? text)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, text);

        IncreaseX(width);
    }

    public void AddEntryHtmlCentered(int width, int color,
        [InterpolatedStringHandlerArgument(nameof(color))] ref GumpInterpolatedStringHandler<T, Centered> handler)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, ref handler);

        IncreaseX(width);
    }

    public void AddEntryHeader(int width)
    {
        AddEntryHeader(width, 1);
    }

    public void AddEntryHeader(int width, int spannedEntries)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight * spannedEntries + _offsetSize * (spannedEntries - 1), _headerGumpId);
        IncreaseX(width);
    }

    public void AddBlankLine()
    {
        if (_offsetWidth != 0)
        {
            _builder.AddImageTiled(0, _currentY, _offsetWidth, _entryHeight, _backGumpId + 4);
        }

        AddNewLine();
    }

    public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight)
    {
        AddEntryButton(width, normalID, pressedID, buttonID, buttonWidth, buttonHeight, 1);
    }

    public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight, int spannedEntries)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight * spannedEntries + _offsetSize * (spannedEntries - 1), _headerGumpId);

        _builder.AddButton(_currentX + (width - buttonWidth) / 2, _currentY + (_entryHeight * spannedEntries +
            _offsetSize * (spannedEntries - 1) - buttonHeight) / 2, normalID, pressedID, buttonID);

        IncreaseX(width);
    }

    public void AddEntryPageButton(int width, int normalID, int pressedID, int page, int buttonWidth, int buttonHeight)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _headerGumpId);

        _builder.AddButton(_currentX + (width - buttonWidth) / 2, _currentY + (_entryHeight - buttonHeight) / 2, normalID,
            pressedID, 0, GumpButtonType.Page, page);

        IncreaseX(width);
    }

    public void AddEntryText(int width, int entryID, string initialText)
    {
        _builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
        _builder.AddTextEntry(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, _textHue, entryID, initialText);

        IncreaseX(width);
    }

    public void AddImageTiled(int x, int y, int width, int height, int gumpId)
    {
        _builder.AddImageTiled(x, y, width, height, gumpId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncreaseX(int width)
    {
        _currentX += width + _offsetSize;
        width = _currentX + _borderSize;

        if (width > _backgroundWidth)
        {
            _backgroundWidth = width;
        }

        width = _currentX - _borderSize;

        if (width > _offsetWidth)
        {
            _offsetWidth = width;
        }
    }

    internal void FinalizeLayout()
    {
        _builder.FinalizeLayout();
    }

    public readonly void Dispose()
    {
        _builder.Dispose();
    }
}
