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

namespace Server.Gumps;

/// <summary>
/// Extension methods for DynamicGumpBuilder that accept GridCell for positioning.
/// Provides ergonomic grid-based gump building with zero allocations.
/// </summary>
public static class GridBuilderExtensions
{
    /// <summary>
    /// Adds a background element filling the entire cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddBackground(ref this DynamicGumpBuilder builder, in GridCell cell, int gumpId)
    {
        builder.AddBackground(cell.X, cell.Y, cell.Width, cell.Height, gumpId);
    }

    /// <summary>
    /// Adds an alpha region filling the entire cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddAlphaRegion(ref this DynamicGumpBuilder builder, in GridCell cell)
    {
        builder.AddAlphaRegion(cell.X, cell.Y, cell.Width, cell.Height);
    }

    /// <summary>
    /// Adds a tiled image filling the entire cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddImageTiled(ref this DynamicGumpBuilder builder, in GridCell cell, int gumpId)
    {
        builder.AddImageTiled(cell.X, cell.Y, cell.Width, cell.Height, gumpId);
    }

    /// <summary>
    /// Adds an image at the cell position with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddImage(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int gumpId,
        int hue = 0,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddImage(cell.X + offsetX, cell.Y + offsetY, gumpId, hue);
    }

    /// <summary>
    /// Adds an item display at the cell position with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddItem(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int itemId,
        int hue = 0,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddItem(cell.X + offsetX, cell.Y + offsetY, itemId, hue);
    }

    /// <summary>
    /// Adds a label at the cell position with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddLabel(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int hue,
        ReadOnlySpan<char> text,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddLabel(cell.X + offsetX, cell.Y + offsetY, hue, text);
    }

    /// <summary>
    /// Adds a cropped label filling the cell with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddLabelCropped(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int hue,
        ReadOnlySpan<char> text,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddLabelCropped(
            cell.X + offsetX,
            cell.Y + offsetY,
            cell.Width - offsetX,
            cell.Height - offsetY,
            hue,
            text);
    }

    /// <summary>
    /// Adds HTML text filling the entire cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddHtml(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        ReadOnlySpan<char> text,
        ReadOnlySpan<char> color = default,
        int size = -1,
        byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left,
        bool background = false,
        bool scrollbar = false)
    {
        builder.AddHtml(cell.X, cell.Y, cell.Width, cell.Height, text, color, size, fontStyle, align, background, scrollbar);
    }

    /// <summary>
    /// Adds HTML text filling the cell with offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddHtml(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        ReadOnlySpan<char> text,
        int offsetX,
        int offsetY,
        ReadOnlySpan<char> color = default,
        int size = -1,
        byte fontStyle = 0,
        TextAlignment align = TextAlignment.Left,
        bool background = false,
        bool scrollbar = false)
    {
        builder.AddHtml(
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
            scrollbar);
    }

    /// <summary>
    /// Adds a localized HTML element filling the cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddHtmlLocalized(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int number,
        bool background = false,
        bool scrollbar = false)
    {
        builder.AddHtmlLocalized(cell.X, cell.Y, cell.Width, cell.Height, number, background, scrollbar);
    }

    /// <summary>
    /// Adds a localized HTML element with color.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddHtmlLocalized(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int number,
        int color,
        bool background = false,
        bool scrollbar = false)
    {
        builder.AddHtmlLocalized(cell.X, cell.Y, cell.Width, cell.Height, number, color, background, scrollbar);
    }

    /// <summary>
    /// Adds a button at the cell position with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddButton(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int normalId,
        int pressedId,
        int buttonId,
        GumpButtonType type = GumpButtonType.Reply,
        int param = 0,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddButton(cell.X + offsetX, cell.Y + offsetY, normalId, pressedId, buttonId, type, param);
    }

    /// <summary>
    /// Adds a checkbox at the cell position with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddCheckbox(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int inactiveId,
        int activeId,
        bool selected,
        int switchId,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddCheckbox(cell.X + offsetX, cell.Y + offsetY, inactiveId, activeId, selected, switchId);
    }

    /// <summary>
    /// Adds a radio button at the cell position with optional offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddRadio(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int inactiveId,
        int activeId,
        bool selected,
        int switchId,
        int offsetX = 0,
        int offsetY = 0)
    {
        builder.AddRadio(cell.X + offsetX, cell.Y + offsetY, inactiveId, activeId, selected, switchId);
    }

    /// <summary>
    /// Adds a text entry filling the cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddTextEntry(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int hue,
        int entryId,
        ReadOnlySpan<char> initialText = default)
    {
        builder.AddTextEntry(cell.X, cell.Y, cell.Width, cell.Height, hue, entryId, initialText);
    }

    /// <summary>
    /// Adds a text entry with offset.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddTextEntry(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int hue,
        int entryId,
        int offsetX,
        int offsetY,
        ReadOnlySpan<char> initialText = default)
    {
        builder.AddTextEntry(
            cell.X + offsetX,
            cell.Y + offsetY,
            cell.Width - offsetX,
            cell.Height - offsetY,
            hue,
            entryId,
            initialText);
    }

    /// <summary>
    /// Adds a limited text entry filling the cell.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddTextEntryLimited(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int hue,
        int entryId,
        int size,
        ReadOnlySpan<char> initialText = default)
    {
        builder.AddTextEntryLimited(cell.X, cell.Y, cell.Width, cell.Height, hue, entryId, initialText, size);
    }

    /// <summary>
    /// Adds an image-tiled button at the cell position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddImageTiledButton(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        int normalId,
        int pressedId,
        int buttonId,
        GumpButtonType type,
        int param,
        int itemId,
        int hue,
        int localizedTooltip = -1)
    {
        builder.AddImageTiledButton(
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
            localizedTooltip);
    }
}
