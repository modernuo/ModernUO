/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PropsLayoutExtensions.cs                                        *
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

using static Server.Gumps.PropsConfig;

namespace Server.Gumps;

/// <summary>
/// Shared helper for advancing to the next row in PropsConfig-style layouts.
/// Resets x to content origin and advances y by one row height.
/// </summary>
public static class PropsLayout
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextRow(ref int x, ref int y, int entryHeight = EntryHeight)
    {
        x = BorderSize + OffsetSize;
        y += entryHeight + OffsetSize;
    }
}

/// <summary>
/// Extension methods for legacy Gump that provide PropsConfig-style layout building blocks.
/// Each method encapsulates a common row pattern used across staff gumps.
/// </summary>
public static class PropsLayoutExtensions
{
    /// <summary>
    /// Adds the standard PropsConfig frame: outer background + inner offset region.
    /// Outputs the content origin (x, y) for the first row.
    /// Does NOT call AddPage — callers handle paging strategy.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsFrame(
        this Gump gump,
        int totalWidth,
        int rowCount,
        out int x,
        out int y,
        int entryHeight = EntryHeight
    )
    {
        var totalHeight = OffsetSize + (entryHeight + OffsetSize) * rowCount;

        gump.AddBackground(0, 0, BorderSize + totalWidth + BorderSize, BorderSize + totalHeight + BorderSize, BackGumpID);
        gump.AddImageTiled(BorderSize, BorderSize, totalWidth, totalHeight, OffsetGumpID);

        x = BorderSize + OffsetSize;
        y = BorderSize + OffsetSize;
    }

    /// <summary>
    /// Adds a 3-column navigation header: [Prev] [Title] [Next].
    /// All three columns use HeaderGumpID backgrounds.
    /// </summary>
    public static void AddPropsHeader(
        this Gump gump,
        int totalWidth,
        ref int x,
        ref int y,
        string title,
        bool hasPrev,
        int prevButtonId,
        bool hasNext,
        int nextButtonId,
        GumpButtonType prevType = GumpButtonType.Reply,
        int prevParam = 0,
        GumpButtonType nextType = GumpButtonType.Reply,
        int nextParam = 0,
        int entryHeight = EntryHeight
    )
    {
        var emptyWidth = totalWidth - PrevWidth - NextWidth - OffsetSize * 4;

        gump.AddImageTiled(x, y, PrevWidth, entryHeight, HeaderGumpID);

        if (hasPrev)
        {
            gump.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, prevButtonId, prevType, prevParam);
        }

        x += PrevWidth + OffsetSize;

        gump.AddImageTiled(x, y, emptyWidth, entryHeight, HeaderGumpID);

        if (title != null)
        {
            gump.AddHtml(x + TextOffsetX, y, emptyWidth - TextOffsetX, entryHeight, $"<center>{title}</center>");
        }

        x += emptyWidth + OffsetSize;

        gump.AddImageTiled(x, y, NextWidth, entryHeight, HeaderGumpID);

        if (hasNext)
        {
            gump.AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, nextButtonId, nextType, nextParam);
        }
    }

    /// <summary>
    /// Adds a 4-column navigation header: [Back] [Title] [Prev] [Next].
    /// Back/Prev/Next use HeaderGumpID, Title uses EntryGumpID.
    /// </summary>
    public static void AddPropsHeaderWithBack(
        this Gump gump,
        int totalWidth,
        ref int x,
        ref int y,
        string title,
        bool hasBack,
        int backButtonId,
        bool hasPrev,
        int prevButtonId,
        bool hasNext,
        int nextButtonId,
        GumpButtonType nextType = GumpButtonType.Reply,
        int nextParam = 0,
        int entryHeight = EntryHeight
    )
    {
        var emptyWidth = totalWidth - PrevWidth * 2 - NextWidth - OffsetSize * 5;

        gump.AddImageTiled(x, y, PrevWidth, entryHeight, HeaderGumpID);

        if (hasBack)
        {
            gump.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, backButtonId);
        }

        x += PrevWidth + OffsetSize;

        gump.AddImageTiled(x, y, emptyWidth, entryHeight, EntryGumpID);

        if (title != null)
        {
            gump.AddHtml(
                x + TextOffsetX,
                y + (entryHeight - EntryHeight) / 2,
                emptyWidth - TextOffsetX,
                entryHeight,
                $"<center>{title}</center>"
            );
        }

        x += emptyWidth + OffsetSize;

        gump.AddImageTiled(x, y, PrevWidth, entryHeight, HeaderGumpID);

        if (hasPrev)
        {
            gump.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, prevButtonId);
        }

        x += PrevWidth + OffsetSize;

        gump.AddImageTiled(x, y, NextWidth, entryHeight, HeaderGumpID);

        if (hasNext)
        {
            gump.AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, nextButtonId, nextType, nextParam);
        }
    }

    /// <summary>
    /// Adds an entry row with a cropped label and optional action button.
    /// Layout: [Label:EntryGumpID] [Button:SetGumpID]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsEntryButton(
        this Gump gump,
        ref int x,
        ref int y,
        int entryWidth,
        string label,
        bool hasButton,
        int buttonId,
        int textHue = TextHue,
        int entryHeight = EntryHeight
    )
    {
        gump.AddImageTiled(x, y, entryWidth, entryHeight, EntryGumpID);
        gump.AddLabelCropped(x + TextOffsetX, y, entryWidth - TextOffsetX, entryHeight, textHue, label);
        x += entryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            gump.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
        }

        if (hasButton)
        {
            gump.AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, buttonId);
        }
    }

    /// <summary>
    /// Adds a two-column entry row with name, value, and optional action button.
    /// Layout: [Name:EntryGumpID] [Value:EntryGumpID] [Button:SetGumpID]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsEntryNameValue(
        this Gump gump,
        ref int x,
        ref int y,
        int nameWidth,
        int valueWidth,
        string name,
        string value,
        bool hasButton,
        int buttonId,
        int entryHeight = EntryHeight
    )
    {
        gump.AddImageTiled(x, y, nameWidth, entryHeight, EntryGumpID);
        gump.AddLabelCropped(x + TextOffsetX, y, nameWidth - TextOffsetX, entryHeight, TextHue, name);
        x += nameWidth + OffsetSize;

        gump.AddImageTiled(x, y, valueWidth, entryHeight, EntryGumpID);
        gump.AddLabelCropped(x + TextOffsetX, y, valueWidth - TextOffsetX, entryHeight, TextHue, value);
        x += valueWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            gump.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
        }

        if (hasButton)
        {
            gump.AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, buttonId);
        }
    }

    /// <summary>
    /// Adds an entry row with a text input field and optional action button.
    /// Layout: [TextEntry:EntryGumpID] [Button:SetGumpID]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsEntryTextInput(
        this Gump gump,
        ref int x,
        ref int y,
        int entryWidth,
        int entryId,
        string initialText,
        bool hasButton,
        int buttonId,
        int entryHeight = EntryHeight
    )
    {
        gump.AddImageTiled(x, y, entryWidth, entryHeight, EntryGumpID);
        gump.AddTextEntry(x + TextOffsetX, y, entryWidth - TextOffsetX, entryHeight, TextHue, entryId, initialText);
        x += entryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            gump.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
        }

        if (hasButton)
        {
            gump.AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, buttonId);
        }
    }

    /// <summary>
    /// Adds an entry row with a cropped label and no action button.
    /// Layout: [Label:EntryGumpID] [Empty:SetGumpID]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsEntryLabel(
        this Gump gump,
        ref int x,
        ref int y,
        int entryWidth,
        string label,
        int entryHeight = EntryHeight
    )
    {
        gump.AddImageTiled(x, y, entryWidth, entryHeight, EntryGumpID);
        gump.AddLabelCropped(x + TextOffsetX, y, entryWidth - TextOffsetX, entryHeight, TextHue, label);
        x += entryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            gump.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
        }
    }

    /// <summary>
    /// Adds a full-width type label row (used for PropsGump type group headers).
    /// Layout: [TypeName:EntryGumpID spanning typeWidth] [Empty:SetGumpID]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsEntryType(
        this Gump gump,
        ref int x,
        ref int y,
        int typeWidth,
        string typeName,
        int entryHeight = EntryHeight
    )
    {
        gump.AddImageTiled(x, y, typeWidth, entryHeight, EntryGumpID);
        gump.AddLabelCropped(x + TextOffsetX, y, typeWidth - TextOffsetX, entryHeight, TextHue, typeName);
        x += typeWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            gump.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
        }
    }

    /// <summary>
    /// Adds a blank separator row spanning the full width.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddPropsEntryBlank(
        this Gump gump,
        int x,
        int y,
        int totalWidth,
        int entryHeight = EntryHeight
    )
    {
        gump.AddImageTiled(x - OffsetSize, y, totalWidth, entryHeight, BackGumpID + 4);
    }
}

/// <summary>
/// Extension methods for DynamicGumpBuilder that provide PropsConfig-style layout building blocks.
/// Mirrors PropsLayoutExtensions but uses ReadOnlySpan&lt;char&gt; for text parameters.
/// </summary>
public static class PropsLayoutBuilderExtensions
{
    extension(ref DynamicGumpBuilder builder)
    {
        /// <summary>
        /// Adds the standard PropsConfig frame: outer background + inner offset region.
        /// Outputs the content origin (x, y) for the first row.
        /// Does NOT call AddPage — callers handle paging strategy.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsFrame(
            int totalWidth,
            int rowCount,
            out int x,
            out int y,
            int entryHeight = EntryHeight
        )
        {
            var totalHeight = OffsetSize + (entryHeight + OffsetSize) * rowCount;

            builder.AddBackground(0, 0, BorderSize + totalWidth + BorderSize, BorderSize + totalHeight + BorderSize, BackGumpID);
            builder.AddImageTiled(BorderSize, BorderSize, totalWidth, totalHeight, OffsetGumpID);

            x = BorderSize + OffsetSize;
            y = BorderSize + OffsetSize;
        }

        /// <summary>
        /// Adds a 3-column navigation header: [Prev] [Title] [Next].
        /// All three columns use HeaderGumpID backgrounds.
        /// </summary>
        public void AddPropsHeader(
            int totalWidth,
            ref int x,
            ref int y,
            ReadOnlySpan<char> title,
            bool hasPrev,
            int prevButtonId,
            bool hasNext,
            int nextButtonId,
            GumpButtonType prevType = GumpButtonType.Reply,
            int prevParam = 0,
            GumpButtonType nextType = GumpButtonType.Reply,
            int nextParam = 0,
            int entryHeight = EntryHeight
        )
        {
            var emptyWidth = totalWidth - PrevWidth - NextWidth - OffsetSize * 4;

            builder.AddImageTiled(x, y, PrevWidth, entryHeight, HeaderGumpID);

            if (hasPrev)
            {
                builder.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, prevButtonId, prevType, prevParam);
            }

            x += PrevWidth + OffsetSize;

            builder.AddImageTiled(x, y, emptyWidth, entryHeight, HeaderGumpID);

            if (title.Length > 0)
            {
                builder.AddHtml(x + TextOffsetX, y, emptyWidth - TextOffsetX, entryHeight, title, align: TextAlignment.Center);
            }

            x += emptyWidth + OffsetSize;

            builder.AddImageTiled(x, y, NextWidth, entryHeight, HeaderGumpID);

            if (hasNext)
            {
                builder.AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, nextButtonId, nextType, nextParam);
            }
        }

        /// <summary>
        /// Adds a 4-column navigation header: [Back] [Title] [Prev] [Next].
        /// Back/Prev/Next use HeaderGumpID, Title uses EntryGumpID.
        /// </summary>
        public void AddPropsHeaderWithBack(
            int totalWidth,
            ref int x,
            ref int y,
            ReadOnlySpan<char> title,
            bool hasBack,
            int backButtonId,
            bool hasPrev,
            int prevButtonId,
            bool hasNext,
            int nextButtonId,
            GumpButtonType nextType = GumpButtonType.Reply,
            int nextParam = 0,
            int entryHeight = EntryHeight
        )
        {
            var emptyWidth = totalWidth - PrevWidth * 2 - NextWidth - OffsetSize * 5;

            builder.AddImageTiled(x, y, PrevWidth, entryHeight, HeaderGumpID);

            if (hasBack)
            {
                builder.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, backButtonId);
            }

            x += PrevWidth + OffsetSize;

            builder.AddImageTiled(x, y, emptyWidth, entryHeight, EntryGumpID);

            if (title.Length > 0)
            {
                builder.AddHtml(
                    x + TextOffsetX,
                    y + (entryHeight - EntryHeight) / 2,
                    emptyWidth - TextOffsetX,
                    entryHeight,
                    title,
                    align: TextAlignment.Center
                );
            }

            x += emptyWidth + OffsetSize;

            builder.AddImageTiled(x, y, PrevWidth, entryHeight, HeaderGumpID);

            if (hasPrev)
            {
                builder.AddButton(x + PrevOffsetX, y + PrevOffsetY, PrevButtonID1, PrevButtonID2, prevButtonId);
            }

            x += PrevWidth + OffsetSize;

            builder.AddImageTiled(x, y, NextWidth, entryHeight, HeaderGumpID);

            if (hasNext)
            {
                builder.AddButton(x + NextOffsetX, y + NextOffsetY, NextButtonID1, NextButtonID2, nextButtonId, nextType, nextParam);
            }
        }

        /// <summary>
        /// Adds an entry row with a cropped label and optional action button.
        /// Layout: [Label:EntryGumpID] [Button:SetGumpID]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsEntryButton(
            ref int x,
            ref int y,
            int entryWidth,
            ReadOnlySpan<char> label,
            bool hasButton,
            int buttonId,
            int textHue = TextHue,
            int entryHeight = EntryHeight
        )
        {
            builder.AddImageTiled(x, y, entryWidth, entryHeight, EntryGumpID);
            builder.AddLabelCropped(x + TextOffsetX, y, entryWidth - TextOffsetX, entryHeight, textHue, label);
            x += entryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                builder.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
            }

            if (hasButton)
            {
                builder.AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, buttonId);
            }
        }

        /// <summary>
        /// Adds a two-column entry row with name, value, and optional action button.
        /// Layout: [Name:EntryGumpID] [Value:EntryGumpID] [Button:SetGumpID]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsEntryNameValue(
            ref int x,
            ref int y,
            int nameWidth,
            int valueWidth,
            ReadOnlySpan<char> name,
            ReadOnlySpan<char> value,
            bool hasButton,
            int buttonId,
            int entryHeight = EntryHeight
        )
        {
            builder.AddImageTiled(x, y, nameWidth, entryHeight, EntryGumpID);
            builder.AddLabelCropped(x + TextOffsetX, y, nameWidth - TextOffsetX, entryHeight, TextHue, name);
            x += nameWidth + OffsetSize;

            builder.AddImageTiled(x, y, valueWidth, entryHeight, EntryGumpID);
            builder.AddLabelCropped(x + TextOffsetX, y, valueWidth - TextOffsetX, entryHeight, TextHue, value);
            x += valueWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                builder.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
            }

            if (hasButton)
            {
                builder.AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, buttonId);
            }
        }

        /// <summary>
        /// Adds an entry row with a text input field and optional action button.
        /// Layout: [TextEntry:EntryGumpID] [Button:SetGumpID]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsEntryTextInput(
            ref int x,
            ref int y,
            int entryWidth,
            int entryId,
            ReadOnlySpan<char> initialText,
            bool hasButton,
            int buttonId,
            int entryHeight = EntryHeight
        )
        {
            builder.AddImageTiled(x, y, entryWidth, entryHeight, EntryGumpID);
            builder.AddTextEntry(x + TextOffsetX, y, entryWidth - TextOffsetX, entryHeight, TextHue, entryId, initialText);
            x += entryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                builder.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
            }

            if (hasButton)
            {
                builder.AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, buttonId);
            }
        }

        /// <summary>
        /// Adds an entry row with a cropped label and no action button.
        /// Layout: [Label:EntryGumpID] [Empty:SetGumpID]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsEntryLabel(
            ref int x,
            ref int y,
            int entryWidth,
            ReadOnlySpan<char> label,
            int entryHeight = EntryHeight
        )
        {
            builder.AddImageTiled(x, y, entryWidth, entryHeight, EntryGumpID);
            builder.AddLabelCropped(x + TextOffsetX, y, entryWidth - TextOffsetX, entryHeight, TextHue, label);
            x += entryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                builder.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
            }
        }

        /// <summary>
        /// Adds a full-width type label row (used for PropsGump type group headers).
        /// Layout: [TypeName:EntryGumpID spanning typeWidth] [Empty:SetGumpID]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsEntryType(
            ref int x,
            ref int y,
            int typeWidth,
            ReadOnlySpan<char> typeName,
            int entryHeight = EntryHeight
        )
        {
            builder.AddImageTiled(x, y, typeWidth, entryHeight, EntryGumpID);
            builder.AddLabelCropped(x + TextOffsetX, y, typeWidth - TextOffsetX, entryHeight, TextHue, typeName);
            x += typeWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                builder.AddImageTiled(x, y, SetWidth, entryHeight, SetGumpID);
            }
        }

        /// <summary>
        /// Adds a blank separator row spanning the full width.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddPropsEntryBlank(
            int x,
            int y,
            int totalWidth,
            int entryHeight = EntryHeight
        )
        {
            builder.AddImageTiled(x - OffsetSize, y, totalWidth, entryHeight, BackGumpID + 4);
        }
    }
}
