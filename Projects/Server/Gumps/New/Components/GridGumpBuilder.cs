/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridGumpBuilder.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Enums;
using Server.Gumps.Interfaces;
using Server.Network;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Gumps.Components
{
    public ref struct GridGumpBuilder<T>
        where T : struct, IStringsHandler
    {
        internal GumpBuilder<T> builder;

        private readonly ushort borderSize;
        private readonly ushort offsetSize;
        private readonly ushort entryHeight;
        private readonly ushort offsetGumpId;
        private readonly ushort headerGumpId;
        private readonly ushort entryGumpId;
        private readonly ushort backGumpId;
        private readonly ushort textHue;
        private readonly ushort textOffsetX;

        private Span<byte> backgroundSpan;
        private Span<byte> offsetSpan;
        private int currentX;
        private int currentY;
        private int backgroundWidth;
        private int offsetWidth;
        private ushort currentPage;

        public readonly int CurrentX => currentX;
        public readonly int CurrentY => currentY;

        public GridGumpBuilder(GumpFlags flags = GumpFlags.None, ushort borderSize = 10, ushort offsetSize = 1,
            ushort entryHeight = 20, ushort offsetGumpId = 0x0A40, ushort headerGumpId = 0x0E14, ushort entryGumpId = 0x0BBC,
            ushort backGumpId = 0x13BE, ushort textHue = 0, ushort textOffsetX = 2)
        {
            builder = new(flags);

            this.borderSize = borderSize;
            this.offsetSize = offsetSize;
            this.entryHeight = entryHeight;
            this.offsetGumpId = offsetGumpId;
            this.headerGumpId = headerGumpId;
            this.entryGumpId = entryGumpId;
            this.backGumpId = backGumpId;
            this.textHue = textHue;
            this.textOffsetX = textOffsetX;
        }

        public void FinishPage()
        {
            if (backgroundWidth == 0)
            {
                return;
            }

            int backgroundHeight = currentY + entryHeight + offsetSize + borderSize;
            int offsetHeight = currentY + entryHeight + offsetSize - borderSize;

            Span<char> buffer = stackalloc char[46 * 2];

            MemoryExtensions.TryWrite(buffer, $"{{ resizepic 0 0 {backGumpId} {backgroundWidth} {backgroundHeight} }}", out int charsWritten);
            OperationStatus result = Ascii.FromUtf16(buffer[..charsWritten], backgroundSpan, out int bytesWritten);
            backgroundSpan[bytesWritten..].Fill((byte)' ');

            Debug.Assert(result == OperationStatus.Done);

            MemoryExtensions.TryWrite(buffer, $"{{ gumppictiled {borderSize} {borderSize} {offsetWidth} {offsetHeight} {offsetGumpId} }}", out charsWritten);
            result = Ascii.FromUtf16(buffer[..charsWritten], offsetSpan, out bytesWritten);
            offsetSpan[bytesWritten..].Fill((byte)' ');

            Debug.Assert(result == OperationStatus.Done);

            backgroundWidth = 100;
            offsetWidth = 100;
        }

        public void AddNewPage()
        {
            FinishPage();

            currentX = borderSize + offsetSize;
            currentY = borderSize + offsetSize;

            builder.AddPage(++currentPage);

            backgroundSpan = builder.Reserve(35);
            offsetSpan = builder.Reserve(46);
        }

        public void AddNewLine()
        {
            currentY += entryHeight + offsetSize;
            currentX = borderSize + offsetSize;
        }

        public void AddEntryLabel(int width, string text)
        {
            builder.AddImageTiled(currentX, currentY, width, entryHeight, entryGumpId);
            builder.AddLabelCropped(currentX + textOffsetX, currentY, width - textOffsetX, entryHeight, textHue, text);

            IncreaseX(width);
        }

        public void AddEntryHtml(int width, string? text)
        {
            builder.AddImageTiled(currentX, currentY, width, entryHeight, entryGumpId);
            builder.AddHtml(currentX + textOffsetX, currentY, width - textOffsetX, entryHeight, text);

            IncreaseX(width);
        }

        public void AddEntryHeader(int width)
        {
            AddEntryHeader(width, 1);
        }

        public void AddEntryHeader(int width, int spannedEntries)
        {
            builder.AddImageTiled(currentX, currentY, width, entryHeight * spannedEntries + offsetSize * (spannedEntries - 1), headerGumpId);
            IncreaseX(width);
        }

        public void AddBlankLine()
        {
            if (offsetWidth != 0)
            {
                builder.AddImageTiled(0, currentY, offsetWidth, entryHeight, backGumpId + 4);
            }

            AddNewLine();
        }

        public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight)
        {
            AddEntryButton(width, normalID, pressedID, buttonID, buttonWidth, buttonHeight, 1);
        }

        public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight, int spannedEntries)
        {
            builder.AddImageTiled(currentX, currentY, width, entryHeight * spannedEntries + offsetSize * (spannedEntries - 1), headerGumpId);

            builder.AddButton(currentX + (width - buttonWidth) / 2, currentY + (entryHeight * spannedEntries +
                offsetSize * (spannedEntries - 1) - buttonHeight) / 2, normalID, pressedID, buttonID);

            IncreaseX(width);
        }

        public void AddEntryPageButton(int width, int normalID, int pressedID, int page, int buttonWidth, int buttonHeight)
        {
            builder.AddImageTiled(currentX, currentY, width, entryHeight, headerGumpId);

            builder.AddButton(currentX + (width - buttonWidth) / 2, currentY + (entryHeight - buttonHeight) / 2, normalID,
                pressedID, 0, GumpButtonType.Page, page);

            IncreaseX(width);
        }

        public void AddEntryText(int width, int entryID, string initialText)
        {
            builder.AddImageTiled(currentX, currentY, width, entryHeight, entryGumpId);
            builder.AddTextEntry(currentX + textOffsetX, currentY, width - textOffsetX, entryHeight, textHue, entryID, initialText);

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
            currentX += width + offsetSize;
            width = currentX + borderSize;

            if (width > backgroundWidth)
            {
                backgroundWidth = width;
            }

            width = currentX - borderSize;

            if (width > offsetWidth)
            {
                offsetWidth = width;
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
