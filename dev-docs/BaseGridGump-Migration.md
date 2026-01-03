# BaseGridGump to DynamicGump Migration

## Overview

`BaseGridGump` is a legacy gump class that provides a cursor-based grid layout system. This document analyzes the feasibility of migrating its consumers to `DynamicGump` using the new grid layout system.

## BaseGridGump Design

### Cursor-Based Layout Model
- Tracks `CurrentX` and `CurrentY` as a moving cursor
- `IncreaseX(width)` advances horizontally
- `AddNewLine()` moves to the next row
- Background auto-sizes as content is added via `FinishPage()`

### Virtual Style Properties
```csharp
public virtual int BorderSize => 10;
public virtual int OffsetSize => 1;
public virtual int EntryHeight => 20;
public virtual int OffsetGumpID => 0x0A40;
public virtual int HeaderGumpID => 0x0E14;
public virtual int EntryGumpID => 0x0BBC;
public virtual int BackGumpID => 0x13BE;
public virtual int TextHue => 0;
public virtual int TextOffsetX => 2;
```

### Entry Methods
| Method | Behavior |
|--------|----------|
| `AddEntryLabel(w, text)` | EntryGumpID background + cropped label |
| `AddEntryHtml(w, text)` | EntryGumpID background + HTML |
| `AddEntryHeader(w)` | HeaderGumpID background only |
| `AddEntryHeader(w, rows)` | HeaderGumpID spanning multiple rows |
| `AddEntryButton(w, ...)` | HeaderGumpID + centered button |
| `AddEntryText(w, id, text)` | EntryGumpID + text entry |
| `AddBlankLine()` | BackGumpID full-width separator |

## Consumers

| Gump | File | Sizing | Convertible |
|------|------|--------|-------------|
| `CommandListGump` | HelpInfo.cs | Fixed width, variable height (max 16 rows) | Yes |
| `BatchGump` | Batch.cs | Variable (custom drawing) | Needs analysis |
| `BatchScopeGump` | Batch.cs | Variable row count | Yes |
| `InterfaceGump` | Interface.cs | Variable columns and rows | Complex |
| `InterfaceItemGump` | Interface.cs | Fixed layout | Yes |
| `InterfaceMobileGump` | Interface.cs | Variable rows (conditional) | Yes |

## CommandListGump Analysis

### Layout Structure
```
Row 0 (Header):  [Button/Header(20)] [Html(320)] [Button/Header(20)]
Row 1-15 (Data): [Html(341)]                     [Button(20)]
                 -- or --
                 [Html(341)] [Header(20)]  (access level separator)
```

### Dimensions
- **Width:** Fixed at `360 + BorderSize*2 + OffsetSize*2` = 382 pixels
- **Height:** Variable, max 16 rows = `BorderSize*2 + OffsetSize*2 + 16*EntryHeight + 15*OffsetSize` = 357 pixels

### Why It Works With Fixed Dimensions
1. Column widths are constant (20, 320, 20)
2. Row count is capped at 16 (1 header + 15 from loop)
3. Auto-sizing only affects unused bottom space

### Proposed Conversion
```csharp
public class CommandListGump : DynamicGump
{
    private static readonly GridEntryStyle Style = GridEntryStyle.Default;

    // Fixed dimensions
    private const int GumpWidth = 382;  // 360 + borders
    private const int GumpHeight = 357; // 16 rows max + borders
    private const int ContentWidth = 360;
    private const int EntriesPerPage = 15;

    // Column spec: button(20) content(320) button(20)
    private const string ColumnSpec = "20 320 20";

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        Span<int> colPos = stackalloc int[3];
        Span<int> colWidths = stackalloc int[3];

        var layout = ListViewLayout.Create(
            Style.BorderSize + Style.OffsetSize,
            Style.BorderSize + Style.OffsetSize,
            ContentWidth,
            GumpHeight - Style.BorderSize * 2,
            _list.Count,
            _page,
            Style.EntryHeight,
            Style.EntryHeight, // header same height as rows
            ColumnSpec,
            colPos, colWidths);

        // Background
        builder.AddBackground(0, 0, GumpWidth, GumpHeight, Style.BackGumpID);
        builder.AddImageTiled(Style.BorderSize, Style.BorderSize,
            ContentWidth, GumpHeight - Style.BorderSize * 2, Style.OffsetGumpID);

        // Header row
        var headerY = layout.GetRowY(-1); // or use GetHeaderCell
        // ... build header ...

        // Data rows
        for (var i = 0; i < layout.VisibleCount; i++)
        {
            var dataIndex = layout.GetDataIndex(i);
            var rowY = layout.GetRowY(i);
            // ... build row ...
        }
    }
}
```

## New Grid Layout System Components

### Existing (from SpawnerControllerGump migration)
- `GridCell` - Value type for cell bounds
- `GridSizeSpec` - Parses sizing specs ("10*", "*", "100")
- `GridCalculator` - Computes track positions/sizes
- `ListViewLayout` - Pagination + column layout
- `GridBuilderExtensions` - Extension methods for DynamicGumpBuilder

### Needed for BaseGridGump Migration

#### GridEntryStyle
```csharp
public readonly struct GridEntryStyle
{
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

    public readonly int EntryGumpID;
    public readonly int HeaderGumpID;
    public readonly int OffsetGumpID;
    public readonly int BackGumpID;
    public readonly int TextHue;
    public readonly int TextOffsetX;
    public readonly int EntryHeight;
    public readonly int BorderSize;
    public readonly int OffsetSize;

    // Arrow button constants
    public const int ArrowLeftID1 = 0x15E3;
    public const int ArrowLeftID2 = 0x15E7;
    public const int ArrowRightID1 = 0x15E1;
    public const int ArrowRightID2 = 0x15E5;
    public const int ArrowWidth = 16;
    public const int ArrowHeight = 16;
}
```

#### Entry Extension Methods
```csharp
public static class GridEntryExtensions
{
    public static void AddEntryLabel(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        in GridEntryStyle style,
        ReadOnlySpan<char> text)
    {
        builder.AddImageTiled(cell, style.EntryGumpID);
        builder.AddLabelCropped(cell, style.TextHue, text, style.TextOffsetX);
    }

    public static void AddEntryHtml(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        in GridEntryStyle style,
        ReadOnlySpan<char> text)
    {
        builder.AddImageTiled(cell, style.EntryGumpID);
        builder.AddHtml(cell, text, offsetX: style.TextOffsetX);
    }

    public static void AddEntryHeader(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        in GridEntryStyle style)
    {
        builder.AddImageTiled(cell, style.HeaderGumpID);
    }

    public static void AddEntryButton(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        in GridEntryStyle style,
        int normalId,
        int pressedId,
        int buttonId,
        int buttonWidth,
        int buttonHeight)
    {
        builder.AddImageTiled(cell, style.HeaderGumpID);
        builder.AddButton(cell, normalId, pressedId, buttonId,
            offsetX: (cell.Width - buttonWidth) / 2,
            offsetY: (cell.Height - buttonHeight) / 2);
    }

    public static void AddEntryText(
        ref this DynamicGumpBuilder builder,
        in GridCell cell,
        in GridEntryStyle style,
        int entryId,
        ReadOnlySpan<char> initialText = default)
    {
        builder.AddImageTiled(cell, style.EntryGumpID);
        builder.AddTextEntry(cell, style.TextHue, entryId, style.TextOffsetX, 0, initialText);
    }
}
```

## Cursor Model vs Cell Model

| Aspect | Cursor (BaseGridGump) | Cell (New Grid Layout) |
|--------|----------------------|------------------------|
| Position tracking | Mutable CurrentX/Y | Computed from indices |
| Random access | No | Yes |
| Out-of-order building | No | Yes |
| Auto-sizing | Yes | No (pre-calculated) |
| Spanning | Manual | Via GridCell spanning |
| Flexibility | Limited | High |

**Conclusion:** The cell model is more flexible. Auto-sizing can be handled by:
1. Using fixed max dimensions (recommended for most cases)
2. Two-pass rendering (calculate then render)
3. Pre-computing content dimensions

## Migration Priority

1. **Easy (fixed layout):** `InterfaceItemGump`, `InterfaceMobileGump`, `CommandListGump`
2. **Medium (variable rows):** `BatchScopeGump`
3. **Complex (custom drawing):** `BatchGump`, `InterfaceGump`

## Implementation Steps

1. [ ] Create `GridEntryStyle` struct
2. [ ] Create `GridEntryExtensions` with `AddEntry*` methods
3. [ ] Migrate `CommandListGump` as proof of concept
4. [ ] Migrate remaining easy gumps
5. [ ] Analyze and migrate complex gumps
6. [ ] Deprecate/remove `BaseGridGump`
