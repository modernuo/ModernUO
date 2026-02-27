/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridBuilderExtensions.cs                                        *
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
/// Extension methods for DynamicGumpBuilder that accept GridCell for positioning.
/// Provides ergonomic grid-based gump building with zero allocations.
/// </summary>
public static class GridBuilderExtensions
{
    extension(ref DynamicGumpBuilder builder)
    {
        /// <summary>
        /// Adds a background element filling the entire cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBackground(in GridCell cell, int gumpId) =>
            builder.AddBackground(cell.X, cell.Y, cell.Width, cell.Height, gumpId);

        /// <summary>
        /// Adds an alpha region filling the entire cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddAlphaRegion(in GridCell cell) =>
            builder.AddAlphaRegion(cell.X, cell.Y, cell.Width, cell.Height);

        /// <summary>
        /// Adds a tiled image filling the entire cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddImageTiled(in GridCell cell, int gumpId) =>
            builder.AddImageTiled(cell.X, cell.Y, cell.Width, cell.Height, gumpId);

        /// <summary>
        /// Adds an image at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddImage(
            in GridCell cell,
            int gumpId,
            int hue = 0,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddImage(cell.X + offsetX, cell.Y + offsetY, gumpId, hue);

        /// <summary>
        /// Adds an item display at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddItem(
            in GridCell cell,
            int itemId,
            int hue = 0,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddItem(cell.X + offsetX, cell.Y + offsetY, itemId, hue);

        /// <summary>
        /// Adds a label at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLabel(
            in GridCell cell,
            int hue,
            ReadOnlySpan<char> text,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddLabel(cell.X + offsetX, cell.Y + offsetY, hue, text);

        /// <summary>
        /// Adds a label at the cell position with optional offset using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLabel(
            in GridCell cell,
            int hue,
            ref RawInterpolatedStringHandler handler,
            int offsetX = 0,
            int offsetY = 0
        )
        {
            builder.AddLabel(cell.X + offsetX, cell.Y + offsetY, hue, handler.Text);
            handler.Clear();
        }

        /// <summary>
        /// Adds a cropped label filling the cell with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLabelCropped(
            in GridCell cell,
            int hue,
            ReadOnlySpan<char> text,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddLabelCropped(
            cell.X + offsetX,
            cell.Y + offsetY,
            cell.Width - offsetX,
            cell.Height - offsetY,
            hue,
            text
        );

        /// <summary>
        /// Adds a cropped label filling the cell with optional offset using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLabelCropped(
            in GridCell cell,
            int hue,
            ref RawInterpolatedStringHandler handler,
            int offsetX = 0,
            int offsetY = 0
        )
        {
            builder.AddLabelCropped(
                cell.X + offsetX,
                cell.Y + offsetY,
                cell.Width - offsetX,
                cell.Height - offsetY,
                hue,
                handler.Text
            );
            handler.Clear();
        }

        /// <summary>
        /// Adds HTML text filling the entire cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtml(
            in GridCell cell,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> color = default,
            int size = -1,
            byte fontStyle = 0,
            TextAlignment align = TextAlignment.Left,
            bool background = false,
            bool scrollbar = false
        ) => builder.AddHtml(
            cell.X,
            cell.Y,
            cell.Width,
            cell.Height,
            text,
            color,
            size,
            fontStyle,
            align,
            background,
            scrollbar
        );

        /// <summary>
        /// Adds HTML text filling the entire cell using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtml(
            in GridCell cell,
            ref RawInterpolatedStringHandler handler,
            ReadOnlySpan<char> color = default,
            int size = -1,
            byte fontStyle = 0,
            TextAlignment align = TextAlignment.Left,
            bool background = false,
            bool scrollbar = false
        )
        {
            builder.AddHtml(
                cell.X,
                cell.Y,
                cell.Width,
                cell.Height,
                handler.Text,
                color,
                size,
                fontStyle,
                align,
                background,
                scrollbar
            );
            handler.Clear();
        }

        /// <summary>
        /// Adds HTML text filling the cell with offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtml(
            in GridCell cell,
            ReadOnlySpan<char> text,
            int offsetX,
            int offsetY,
            ReadOnlySpan<char> color = default,
            int size = -1,
            byte fontStyle = 0,
            TextAlignment align = TextAlignment.Left,
            bool background = false,
            bool scrollbar = false
        ) => builder.AddHtml(
            cell.X + offsetX,
            cell.Y + offsetY,
            cell.Width - offsetX,
            cell.Height - offsetY,
            text,
            color,
            size,
            fontStyle,
            align,
            background,
            scrollbar
        );

        /// <summary>
        /// Adds HTML text filling the cell with offset using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtml(
            in GridCell cell,
            ref RawInterpolatedStringHandler handler,
            int offsetX,
            int offsetY,
            ReadOnlySpan<char> color = default,
            int size = -1,
            byte fontStyle = 0,
            TextAlignment align = TextAlignment.Left,
            bool background = false,
            bool scrollbar = false
        )
        {
            builder.AddHtml(
                cell.X + offsetX,
                cell.Y + offsetY,
                cell.Width - offsetX,
                cell.Height - offsetY,
                handler.Text,
                color,
                size,
                fontStyle,
                align,
                background,
                scrollbar
            );
            handler.Clear();
        }

        /// <summary>
        /// Adds a localized HTML element filling the cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtmlLocalized(
            in GridCell cell,
            int number,
            bool background = false,
            bool scrollbar = false
        ) => builder.AddHtmlLocalized(cell.X, cell.Y, cell.Width, cell.Height, number, background, scrollbar);

        /// <summary>
        /// Adds a localized HTML element with color.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtmlLocalized(
            in GridCell cell,
            int number,
            int color,
            bool background = false,
            bool scrollbar = false
        ) => builder.AddHtmlLocalized(cell.X, cell.Y, cell.Width, cell.Height, number, color, background, scrollbar);

        /// <summary>
        /// Adds a localized HTML element with arguments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtmlLocalized(
            in GridCell cell,
            int number,
            ReadOnlySpan<char> args,
            int color,
            bool background = false,
            bool scrollbar = false
        ) => builder.AddHtmlLocalized(cell.X, cell.Y, cell.Width, cell.Height, number, args, color, background, scrollbar);

        /// <summary>
        /// Adds a localized HTML element with arguments using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddHtmlLocalized(
            in GridCell cell,
            int number,
            ref RawInterpolatedStringHandler handler,
            int color,
            bool background = false,
            bool scrollbar = false
        ) => builder.AddHtmlLocalized(cell.X, cell.Y, cell.Width, cell.Height, number, ref handler, color, background, scrollbar);

        /// <summary>
        /// Adds a button at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddButton(
            in GridCell cell,
            int normalId,
            int pressedId,
            int buttonId,
            GumpButtonType type = GumpButtonType.Reply,
            int param = 0,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddButton(cell.X + offsetX, cell.Y + offsetY, normalId, pressedId, buttonId, type, param);

        /// <summary>
        /// Adds a checkbox at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCheckbox(
            in GridCell cell,
            int inactiveId,
            int activeId,
            bool selected,
            int switchId,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddCheckbox(cell.X + offsetX, cell.Y + offsetY, inactiveId, activeId, selected, switchId);

        /// <summary>
        /// Adds a radio button at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRadio(
            in GridCell cell,
            int inactiveId,
            int activeId,
            bool selected,
            int switchId,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddRadio(cell.X + offsetX, cell.Y + offsetY, inactiveId, activeId, selected, switchId);

        /// <summary>
        /// Adds a text entry filling the cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTextEntry(
            in GridCell cell,
            int hue,
            int entryId,
            ReadOnlySpan<char> initialText = default
        ) => builder.AddTextEntry(cell.X, cell.Y, cell.Width, cell.Height, hue, entryId, initialText);

        /// <summary>
        /// Adds a text entry filling the cell using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTextEntry(
            in GridCell cell,
            int hue,
            int entryId,
            ref RawInterpolatedStringHandler handler
        )
        {
            builder.AddTextEntry(cell.X, cell.Y, cell.Width, cell.Height, hue, entryId, handler.Text);
            handler.Clear();
        }

        /// <summary>
        /// Adds a text entry with offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTextEntry(
            in GridCell cell,
            int hue,
            int entryId,
            int offsetX,
            int offsetY,
            ReadOnlySpan<char> initialText = default
        ) => builder.AddTextEntry(
            cell.X + offsetX,
            cell.Y + offsetY,
            cell.Width - offsetX,
            cell.Height - offsetY,
            hue,
            entryId,
            initialText
        );

        /// <summary>
        /// Adds a text entry with offset using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTextEntry(
            in GridCell cell,
            int hue,
            int entryId,
            int offsetX,
            int offsetY,
            ref RawInterpolatedStringHandler handler
        )
        {
            builder.AddTextEntry(
                cell.X + offsetX,
                cell.Y + offsetY,
                cell.Width - offsetX,
                cell.Height - offsetY,
                hue,
                entryId,
                handler.Text
            );
            handler.Clear();
        }

        /// <summary>
        /// Adds a limited text entry filling the cell.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTextEntryLimited(
            in GridCell cell,
            int hue,
            int entryId,
            int size,
            ReadOnlySpan<char> initialText = default
        ) => builder.AddTextEntryLimited(cell.X, cell.Y, cell.Width, cell.Height, hue, entryId, initialText, size);

        /// <summary>
        /// Adds a limited text entry filling the cell using interpolated string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTextEntryLimited(
            in GridCell cell,
            int hue,
            int entryId,
            int size,
            ref RawInterpolatedStringHandler handler
        )
        {
            builder.AddTextEntryLimited(cell.X, cell.Y, cell.Width, cell.Height, hue, entryId, handler.Text, size);
            handler.Clear();
        }

        /// <summary>
        /// Adds an image-tiled button at the cell position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddImageTiledButton(
            in GridCell cell,
            int normalId,
            int pressedId,
            int buttonId,
            GumpButtonType type,
            int param,
            int itemId,
            int hue,
            int localizedTooltip = -1
        ) => builder.AddImageTiledButton(
            cell.X,
            cell.Y,
            normalId,
            pressedId,
            buttonId,
            type,
            param,
            itemId,
            hue,
            cell.Width,
            cell.Height,
            localizedTooltip
        );

        /// <summary>
        /// Adds a sprite image using the cell dimensions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSpriteImage(
            in GridCell cell,
            int gumpId,
            int sx,
            int sy
        ) => builder.AddSpriteImage(cell.X, cell.Y, gumpId, cell.Width, cell.Height, sx, sy);

        /// <summary>
        /// Adds a sprite image at the cell position with optional offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSpriteImage(
            in GridCell cell,
            int gumpId,
            int width,
            int height,
            int sx,
            int sy,
            int offsetX = 0,
            int offsetY = 0
        ) => builder.AddSpriteImage(cell.X + offsetX, cell.Y + offsetY, gumpId, width, height, sx, sy);
    }
}
