/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridEntryExtensions.cs                                          *
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
using Server.Buffers;

namespace Server.Gumps;

/// <summary>
/// Extension methods for DynamicGumpBuilder that provide BaseGridGump-style entry methods.
/// These combine a tiled background with content, matching the AddEntry* pattern.
/// </summary>
public static class GridEntryExtensions
{
    extension(ref DynamicGumpBuilder builder)
    {
        /// <summary>
        /// Adds an entry with label (EntryGumpID background + cropped label).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryLabel(
            in GridCell cell,
            in GridEntryStyle style,
            ReadOnlySpan<char> text
        )
        {
            builder.AddImageTiled(cell, style.EntryGumpID);
            builder.AddLabelCropped(cell, style.TextHue, text, style.TextOffsetX);
        }

        /// <summary>
        /// Adds an entry with label using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryLabel(
            in GridCell cell,
            in GridEntryStyle style,
            ref RawInterpolatedStringHandler handler
        )
        {
            builder.AddImageTiled(cell, style.EntryGumpID);
            builder.AddLabelCropped(cell, style.TextHue, ref handler, style.TextOffsetX);
        }

        /// <summary>
        /// Adds an entry with HTML (EntryGumpID background + HTML text).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryHtml(
            in GridCell cell,
            in GridEntryStyle style,
            ReadOnlySpan<char> text
        )
        {
            builder.AddImageTiled(cell, style.EntryGumpID);
            builder.AddHtml(cell, text, style.TextOffsetX, 0);
        }

        /// <summary>
        /// Adds an entry with HTML using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryHtml(
            in GridCell cell,
            in GridEntryStyle style,
            ref RawInterpolatedStringHandler handler
        )
        {
            builder.AddImageTiled(cell, style.EntryGumpID);
            builder.AddHtml(cell, ref handler, style.TextOffsetX, 0);
        }

        /// <summary>
        /// Adds a header entry (HeaderGumpID background only).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryHeader(
            in GridCell cell,
            in GridEntryStyle style
        )
        {
            builder.AddImageTiled(cell, style.HeaderGumpID);
        }

        /// <summary>
        /// Adds an entry with a centered button (HeaderGumpID background + centered button).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryButton(
            in GridCell cell,
            in GridEntryStyle style,
            int normalId,
            int pressedId,
            int buttonId,
            int buttonWidth,
            int buttonHeight
        )
        {
            builder.AddImageTiled(cell, style.HeaderGumpID);
            builder.AddButton(
                cell, normalId, pressedId, buttonId,
                offsetX: (cell.Width - buttonWidth) / 2,
                offsetY: (cell.Height - buttonHeight) / 2
            );
        }

        /// <summary>
        /// Adds an entry with arrow left button.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryArrowLeft(
            in GridCell cell,
            in GridEntryStyle style,
            int buttonId
        )
        {
            builder.AddEntryButton(
                cell, style,
                GridEntryStyle.ArrowLeftID1,
                GridEntryStyle.ArrowLeftID2,
                buttonId,
                GridEntryStyle.ArrowWidth,
                GridEntryStyle.ArrowHeight
            );
        }

        /// <summary>
        /// Adds an entry with arrow right button.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryArrowRight(
            in GridCell cell,
            in GridEntryStyle style,
            int buttonId
        )
        {
            builder.AddEntryButton(
                cell, style,
                GridEntryStyle.ArrowRightID1,
                GridEntryStyle.ArrowRightID2,
                buttonId,
                GridEntryStyle.ArrowWidth,
                GridEntryStyle.ArrowHeight
            );
        }

        /// <summary>
        /// Adds an entry with text input (EntryGumpID background + text entry).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryText(
            in GridCell cell,
            in GridEntryStyle style,
            int entryId,
            ReadOnlySpan<char> initialText = default
        )
        {
            builder.AddImageTiled(cell, style.EntryGumpID);
            builder.AddTextEntry(cell, style.TextHue, entryId, style.TextOffsetX, 0, initialText);
        }

        /// <summary>
        /// Adds an entry with text input using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryText(
            in GridCell cell,
            in GridEntryStyle style,
            int entryId,
            ref RawInterpolatedStringHandler handler
        )
        {
            builder.AddImageTiled(cell, style.EntryGumpID);
            builder.AddTextEntry(cell, style.TextHue, entryId, style.TextOffsetX, 0, ref handler);
        }

        /// <summary>
        /// Adds a blank line (BackGumpID + 4 background across full width).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddEntryBlank(
            in GridCell cell,
            in GridEntryStyle style
        )
        {
            builder.AddImageTiled(cell, style.BackGumpID + 4);
        }

        /// <summary>
        /// Adds the standard grid background (outer background + inner offset region).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGridBackground(
            int width,
            int height,
            in GridEntryStyle style
        )
        {
            builder.AddBackground(0, 0, width, height, style.BackGumpID);
            builder.AddImageTiled(
                style.BorderSize,
                style.BorderSize,
                width - style.BorderSize * 2,
                height - style.BorderSize * 2,
                style.OffsetGumpID
            );
        }
    }
}
