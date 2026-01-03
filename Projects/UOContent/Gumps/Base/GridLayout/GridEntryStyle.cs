/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GridEntryStyle.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Gumps;

/// <summary>
/// Defines the visual style for grid-based entry layouts.
/// Mirrors the virtual properties from BaseGridGump for easy migration.
/// </summary>
public readonly struct GridEntryStyle
{
    /// <summary>
    /// Default style matching BaseGridGump defaults.
    /// </summary>
    public static readonly GridEntryStyle Default = new(
        entryGumpID: 0x0BBC,
        headerGumpID: 0x0E14,
        offsetGumpID: 0x0A40,
        backGumpID: 0x13BE,
        textHue: 0,
        textOffsetX: 2,
        entryHeight: 20,
        borderSize: 10,
        offsetSize: 1
    );

    /// <summary>
    /// Background gump ID for data entry cells.
    /// </summary>
    public readonly int EntryGumpID;

    /// <summary>
    /// Background gump ID for header/button cells.
    /// </summary>
    public readonly int HeaderGumpID;

    /// <summary>
    /// Gump ID for the offset/separator region.
    /// </summary>
    public readonly int OffsetGumpID;

    /// <summary>
    /// Gump ID for the main background.
    /// </summary>
    public readonly int BackGumpID;

    /// <summary>
    /// Text hue for labels.
    /// </summary>
    public readonly int TextHue;

    /// <summary>
    /// Horizontal offset for text within cells.
    /// </summary>
    public readonly int TextOffsetX;

    /// <summary>
    /// Height of each entry row.
    /// </summary>
    public readonly int EntryHeight;

    /// <summary>
    /// Size of the outer border.
    /// </summary>
    public readonly int BorderSize;

    /// <summary>
    /// Size of the offset/gap between cells.
    /// </summary>
    public readonly int OffsetSize;

    // Arrow button constants (matching BaseGridGump)
    public const int ArrowLeftID1 = 0x15E3;
    public const int ArrowLeftID2 = 0x15E7;
    public const int ArrowRightID1 = 0x15E1;
    public const int ArrowRightID2 = 0x15E5;
    public const int ArrowWidth = 16;
    public const int ArrowHeight = 16;

    public GridEntryStyle(
        int entryGumpID,
        int headerGumpID,
        int offsetGumpID,
        int backGumpID,
        int textHue,
        int textOffsetX,
        int entryHeight,
        int borderSize,
        int offsetSize)
    {
        EntryGumpID = entryGumpID;
        HeaderGumpID = headerGumpID;
        OffsetGumpID = offsetGumpID;
        BackGumpID = backGumpID;
        TextHue = textHue;
        TextOffsetX = textOffsetX;
        EntryHeight = entryHeight;
        BorderSize = borderSize;
        OffsetSize = offsetSize;
    }

    /// <summary>
    /// Creates a copy with modified values.
    /// </summary>
    public GridEntryStyle With(
        int? entryGumpID = null,
        int? headerGumpID = null,
        int? offsetGumpID = null,
        int? backGumpID = null,
        int? textHue = null,
        int? textOffsetX = null,
        int? entryHeight = null,
        int? borderSize = null,
        int? offsetSize = null) => new(
        entryGumpID ?? EntryGumpID,
        headerGumpID ?? HeaderGumpID,
        offsetGumpID ?? OffsetGumpID,
        backGumpID ?? BackGumpID,
        textHue ?? TextHue,
        textOffsetX ?? TextOffsetX,
        entryHeight ?? EntryHeight,
        borderSize ?? BorderSize,
        offsetSize ?? OffsetSize
    );

    /// <summary>
    /// Calculates the content origin X (inside borders).
    /// </summary>
    public int ContentOriginX => BorderSize + OffsetSize;

    /// <summary>
    /// Calculates the content origin Y (inside borders).
    /// </summary>
    public int ContentOriginY => BorderSize + OffsetSize;

    /// <summary>
    /// Calculates total width given content width.
    /// </summary>
    public int GetTotalWidth(int contentWidth) => contentWidth + BorderSize * 2 + OffsetSize * 2;

    /// <summary>
    /// Calculates total height given number of rows.
    /// </summary>
    public int GetTotalHeight(int rowCount) =>
        BorderSize * 2 + OffsetSize * 2 + rowCount * EntryHeight + (rowCount - 1) * OffsetSize;

    /// <summary>
    /// Calculates content height given number of rows.
    /// </summary>
    public int GetContentHeight(int rowCount) =>
        rowCount * EntryHeight + (rowCount - 1) * OffsetSize;
}
