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
using Server.Network;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using static Server.Gumps.Components.Interpolation.GumpInterpolatedStringHandler;

namespace Server.Gumps.Components
{
    public ref struct GridGumpBuilder<T>
        where T : struct, IStringsHandler
    {
        internal GumpBuilder<T> builder;

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

        public GridGumpBuilder(GumpFlags flags = GumpFlags.None, ushort borderSize = 10, ushort offsetSize = 1,
            ushort entryHeight = 20, ushort offsetGumpId = 0x0A40, ushort headerGumpId = 0x0E14, ushort entryGumpId = 0x0BBC,
            ushort backGumpId = 0x13BE, ushort textHue = 0, ushort textOffsetX = 2)
        {
            builder = new(flags);

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

            Span<char> buffer = stackalloc char[46 * 2];

            MemoryExtensions.TryWrite(buffer, $"{{ resizepic 0 0 {_backGumpId} {_backgroundWidth} {backgroundHeight} }}", out int charsWritten);
            OperationStatus result = Ascii.FromUtf16(buffer[..charsWritten], _backgroundSpan, out int bytesWritten);
            _backgroundSpan[bytesWritten..].Fill((byte)' ');

            Debug.Assert(result == OperationStatus.Done);

            MemoryExtensions.TryWrite(buffer, $"{{ gumppictiled {_borderSize} {_borderSize} {_offsetWidth} {offsetHeight} {_offsetGumpId} }}", out charsWritten);
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

            builder.AddPage(++_currentPage);

            _backgroundSpan = builder.Reserve(35);
            _offsetSpan = builder.Reserve(46);
        }

        public void AddNewLine()
        {
            _currentY += _entryHeight + _offsetSize;
            _currentX = _borderSize + _offsetSize;
        }

        public void AddEntryLabel(int width, string text)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddLabelCropped(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, _textHue, text);

            IncreaseX(width);
        }

        public void AddEntryHtml(int width, string? text)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, text);

            IncreaseX(width);
        }

        public void AddEntryHtml(int width, ref GumpInterpolatedStringHandler<T, None> handler)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, ref handler);

            IncreaseX(width);
        }

        public void AddEntryHtml(int width, int color, string? text)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, text);

            IncreaseX(width);
        }

        public void AddEntryHtml(int width, int color,
            [InterpolatedStringHandlerArgument(nameof(color))] scoped ref GumpInterpolatedStringHandler<T, Colored> handler)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtml(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, ref handler);

            IncreaseX(width);
        }

        public void AddEntryHtmlCentered(int width, string? text)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, text);

            IncreaseX(width);
        }

        public void AddEntryHtmlCentered(int width, ref GumpInterpolatedStringHandler<T, Centered> handler)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, ref handler);

            IncreaseX(width);
        }

        public void AddEntryHtmlCentered(int width, int color, string? text)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, text);

            IncreaseX(width);
        }

        public void AddEntryHtmlCentered(int width, int color,
            [InterpolatedStringHandlerArgument(nameof(color))] ref GumpInterpolatedStringHandler<T, Centered> handler)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddHtmlCentered(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, color, ref handler);

            IncreaseX(width);
        }

        public void AddEntryHeader(int width)
        {
            AddEntryHeader(width, 1);
        }

        public void AddEntryHeader(int width, int spannedEntries)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight * spannedEntries + _offsetSize * (spannedEntries - 1), _headerGumpId);
            IncreaseX(width);
        }

        public void AddBlankLine()
        {
            if (_offsetWidth != 0)
            {
                builder.AddImageTiled(0, _currentY, _offsetWidth, _entryHeight, _backGumpId + 4);
            }

            AddNewLine();
        }

        public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight)
        {
            AddEntryButton(width, normalID, pressedID, buttonID, buttonWidth, buttonHeight, 1);
        }

        public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight, int spannedEntries)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight * spannedEntries + _offsetSize * (spannedEntries - 1), _headerGumpId);

            builder.AddButton(_currentX + (width - buttonWidth) / 2, _currentY + (_entryHeight * spannedEntries +
                _offsetSize * (spannedEntries - 1) - buttonHeight) / 2, normalID, pressedID, buttonID);

            IncreaseX(width);
        }

        public void AddEntryPageButton(int width, int normalID, int pressedID, int page, int buttonWidth, int buttonHeight)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _headerGumpId);

            builder.AddButton(_currentX + (width - buttonWidth) / 2, _currentY + (_entryHeight - buttonHeight) / 2, normalID,
                pressedID, 0, GumpButtonType.Page, page);

            IncreaseX(width);
        }

        public void AddEntryText(int width, int entryID, string initialText)
        {
            builder.AddImageTiled(_currentX, _currentY, width, _entryHeight, _entryGumpId);
            builder.AddTextEntry(_currentX + _textOffsetX, _currentY, width - _textOffsetX, _entryHeight, _textHue, entryID, initialText);

            IncreaseX(width);
        }

        public void AddImageTiled(int x, int y, int width, int height, int gumpId)
        {
            builder.AddImageTiled(x, y, width, height, gumpId);
        }

        public readonly void Send(NetState ns, Serial serial, int typeId, int x, int y, out int switches, out int textEntries)
        {
            builder.Send(ns, serial, typeId, x, y, out switches, out textEntries);
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

        public readonly void Dispose()
        {
            builder.Dispose();
        }
    }

    public static class GridGumpBuilderExtensions
    {
        public static void CompileCompressed(this in GridGumpBuilder<DynamicStringsHandler> builder, out LayoutEntry layout, out DynamicStringsEntry strings)
        {
            builder.builder.CompileCompressed(out layout, out strings);
        }

        public static void CompileCompressed(this in GridGumpBuilder<StaticStringsHandler> builder, out LayoutEntry layout, out StringsEntry strings)
        {
            builder.builder.CompileCompressed(out layout, out strings);
        }

        public static void Compile(this in GridGumpBuilder<StaticStringsHandler> builder, out LayoutEntry layout, out StringsEntry strings)
        {
            builder.builder.Compile(out layout, out strings);
        }
    }
}
