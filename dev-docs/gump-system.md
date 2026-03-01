# ModernUO Gump System

This document covers ModernUO's gump (UI dialog) system, including BaseGump, StaticGump, DynamicGump, builders, response handling, and best practices.

## Overview

Gumps are custom UI dialogs displayed to players. ModernUO provides a modern gump system with two main types:
- **StaticGump**: Layout is cached and reused across instances (better performance)
- **DynamicGump**: Layout is rebuilt each time (for variable content)

## Class Hierarchy

```
BaseGump (abstract)
├── StaticGump<TSelf>  -- Cached layout, for fixed-structure gumps
└── DynamicGump        -- Rebuilt layout, for dynamic-structure gumps
```

All gump classes are in `Projects/UOContent/Gumps/Base/`.

## Sending Gumps

```csharp
using Server.Gumps;  // REQUIRED for extension methods

// Send a gump
mobile.SendGump(new MyGump(mobile));

// Check if gump is open
if (mobile.HasGump<MyGump>()) { }

// Find an open gump
var gump = mobile.FindGump<MyGump>();

// Close a gump
mobile.CloseGump<MyGump>();
```

**Important**: `using Server.Gumps;` is required. Without it, `SendGump()`, `HasGump<T>()`, `FindGump<T>()`, and `CloseGump<T>()` extension methods won't resolve.

These methods are also available on `NetState`:
```csharp
mobile.NetState.SendGump(gump);
mobile.NetState.HasGump<MyGump>();
```

## StaticGump

Use for gumps where the layout structure is the same for all instances. The layout is compiled and cached on first use, then reused.

### Template
```csharp
using Server.Gumps;

namespace Server.Gumps;

public class MyStaticGump : StaticGump<MyStaticGump>
{
    private readonly Mobile _player;
    private readonly string _data;

    public override bool Singleton => true;  // Only one per player

    public MyStaticGump(Mobile player, string data) : base(50, 50)
    {
        _player = player;
        _data = data;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        builder.AddAlphaRegion(10, 10, 380, 280);

        // Static text (baked into cached layout)
        builder.AddHtmlLocalized(15, 15, 370, 20, 1060635, 0x7800);  // "Warning"

        // Dynamic text (placeholder filled per-instance via BuildStrings)
        builder.AddHtmlPlaceholder(15, 45, 370, 200, "content", false, true);

        // Buttons
        builder.AddButton(100, 265, 4005, 4007, 1);  // OK (buttonID=1)
        builder.AddHtmlLocalized(135, 267, 100, 20, 1011036);  // "OK"

        builder.AddButton(250, 265, 4017, 4019, 0);  // Cancel (buttonID=0 = close)
        builder.AddHtmlLocalized(285, 267, 100, 20, 1011012);  // "Cancel"
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetHtmlText("content", _data, "#FFC000", 4);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            _player.SendMessage("Confirmed!");
        }
    }
}
```

### How Caching Works
1. First instance calls `BuildLayout()` -- the layout bytes are compiled and cached
2. Subsequent instances reuse the cached layout bytes
3. `BuildStrings()` is called per-instance to fill dynamic text placeholders
4. Use `AddLabelPlaceholder`/`AddHtmlPlaceholder` for text that changes per instance
5. Use `AddLabel`/`AddHtml`/`AddHtmlLocalized` for text baked into the cache

### Placeholders
```csharp
// In BuildLayout:
builder.AddLabelPlaceholder(x, y, hue, "slotKey");
builder.AddHtmlPlaceholder(x, y, w, h, "slotKey", background, scrollbar);
builder.AddTextEntryPlaceholder(x, y, w, h, hue, entryId, "slotKey");

// In BuildStrings:
builder.SetStringSlot("slotKey", "text value");
builder.SetHtmlText("slotKey", "html content", "#color", fontSize);
```

## DynamicGump

Use for gumps where the layout structure varies per instance (e.g., lists of items, search results).

### Template
```csharp
using Server.Gumps;

namespace Server.Gumps;

public class MyDynamicGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly List<Item> _items;

    public override bool Singleton => true;

    public MyDynamicGump(Mobile player, List<Item> items) : base(50, 50)
    {
        _player = player;
        _items = items;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var height = 60 + _items.Count * 30;
        builder.AddPage();
        builder.AddBackground(0, 0, 400, height, 5054);
        builder.AddAlphaRegion(10, 10, 380, height - 20);
        builder.AddHtml(15, 15, 370, 20, "Select an item:");

        for (var i = 0; i < _items.Count; i++)
        {
            var y = 45 + i * 30;
            var item = _items[i];
            builder.AddLabel(20, y, 0x480, item.Name ?? "Unknown");
            builder.AddButton(350, y, 4005, 4007, i + 1);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID > 0 && info.ButtonID <= _items.Count)
        {
            var selected = _items[info.ButtonID - 1];
            _player.SendMessage($"You selected: {selected.Name}");
        }
    }
}
```

## Builder Methods Reference

### Layout Structure
```csharp
builder.AddPage(int page = 0);           // Add page (0 = all pages)
builder.AddBackground(x, y, w, h, gumpID);
builder.AddAlphaRegion(x, y, w, h);       // Transparent background
builder.AddImageTiled(x, y, w, h, gumpID); // Tiled background image
builder.AddGroup(int groupId);             // Radio button group
```

### Images
```csharp
builder.AddImage(x, y, gumpID, hue);     // Gump art image
builder.AddItem(x, y, itemID, hue);       // Item graphic
builder.AddImageTiledButton(x, y, normalID, pressedID, buttonID, type, param, itemID, hue, w, h);
```

### Text
```csharp
builder.AddLabel(x, y, hue, text);                    // Single-line text
builder.AddLabelCropped(x, y, w, h, hue, text);       // Cropped text
builder.AddHtml(x, y, w, h, text, bg, scrollbar);     // HTML text
builder.AddHtml(x, y, w, h, text, color, size, fontStyle, align, bg, scrollbar);
builder.AddHtmlLocalized(x, y, w, h, clilocNumber);   // Localized text
builder.AddHtmlLocalized(x, y, w, h, clilocNumber, color);
```

### Interactive Elements
```csharp
builder.AddButton(x, y, normalID, pressedID, buttonID);
builder.AddButton(x, y, normalID, pressedID, buttonID, GumpButtonType.Page, pageNum);
builder.AddCheckbox(x, y, inactiveID, activeID, selected, switchID);
builder.AddRadio(x, y, inactiveID, activeID, selected, switchID);
builder.AddTextEntry(x, y, w, h, hue, entryID, initialText);
builder.AddTextEntryLimited(x, y, w, h, hue, entryID, initialText, maxLength);
```

### Modifiers
```csharp
builder.SetNoClose();      // Disable right-click close
builder.SetNoMove();       // Disable dragging
builder.SetNoResize();     // Disable resizing
builder.SetNoDispose();    // Disable dispose
builder.AddTooltip(num);   // Tooltip on hover
builder.AddItemProperty(serial); // Item property tooltip
```

## Response Handling

```csharp
public override void OnResponse(NetState sender, in RelayInfo info)
{
    var mobile = sender.Mobile;

    // Button ID (0 = close/cancel, 1+ = custom buttons)
    switch (info.ButtonID)
    {
        case 0: return;  // Closed
        case 1:
            // Handle button 1
            break;
    }

    // Check checkbox/radio state
    bool isChecked = info.IsSwitched(switchID);

    // Get text entry value
    string text = info.GetTextEntry(entryID);
}
```

### Button ID Convention
- `0` = Close/Cancel (default when player closes gump)
- `1+` = Custom action buttons
- Use `GumpButtonType.Page` for page navigation buttons (don't trigger OnResponse)

## BaseGump Properties

```csharp
public int X { get; set; }           // Gump X position
public int Y { get; set; }           // Gump Y position
public virtual bool Singleton => false;  // Only one instance per player
public int TypeID { get; }           // Unique type identifier
public Serial Serial { get; }       // Gump serial
```

## Common Gump IDs (Background Art)

| ID | Description |
|---|---|
| 5054 | Dark stone background |
| 9200 | Scroll background |
| 9250 | Light parchment |
| 3600 | Brown wood panel |
| 5120 | Gray stone border |
| 2620 | Ornate gold frame |

## Common Button IDs (Art)

| Normal/Pressed | Description |
|---|---|
| 4005/4007 | Small right arrow (green) |
| 4017/4019 | Small X (red) |
| 4023/4025 | Small left arrow |
| 4020/4022 | Small checkmark |
| 4029/4031 | Large right arrow |
| 247/248 | Large green gem |
| 241/242 | Large red gem |

## Important Properties

### Singleton
```csharp
public override bool Singleton => true;
```
When `true`, the gump system automatically closes any existing instance of this gump type for the player before sending a new one. **Always set this for gumps that shouldn't stack.** Without it, repeated sends create duplicate gumps the player must close individually.

### Cached (StaticGump only)
```csharp
protected virtual bool Cached => true;  // default
```
Controls whether `StaticGump<T>` caches its compiled layout. The layout is compiled once on first send, then reused for all subsequent instances.

**Set to `false` during development** to force the layout to rebuild every send — useful for hot-reload iteration and debugging layout changes without restarting the server:
```csharp
// Temporary: disable caching while iterating on layout
protected override bool Cached => false;
```
**Remove the override (or set back to `true`) before committing.** Leaving it `false` in production wastes CPU recompiling identical layouts.

## Empty Gump Rule (CRITICAL)

**NEVER send a gump with no visual components.** An empty gump (no background, no buttons, no content) has no close button and no right-click dismiss — the client cannot close it. This causes a **gump leak** on both the client and server: the gump stays in the tracking list forever, the client renders an invisible undismissable element, and the slot is consumed until the player relogs.

Empty gumps typically happen when a developer short-circuits inside the constructor or `BuildLayout`:
```csharp
// BAD: Short-circuit in constructor creates an empty gump
public MyGump(Mobile from) : base(50, 50)
{
    if (!from.Alive)
        return;  // Gump is already constructed — it's empty but will still be sent!

    AddPage(0);
    AddBackground(0, 0, 400, 300, 5054);
    // ...
}
```

### The Fix: Static DisplayTo Pattern

Use a static entry-point method that validates prerequisites **before** constructing the gump. The constructor is private — the only way to create the gump is through `DisplayTo`, which guarantees the gump is never empty. See `GoGump.cs` for the canonical example:

```csharp
public class MyGump : DynamicGump  // or StaticGump<MyGump>, or Gump
{
    public override bool Singleton => true;

    // Private constructor — can only be called from DisplayTo
    private MyGump(Mobile from, SomeData data) : base(50, 50)
    {
        // Safe to build layout — prerequisites already validated
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        // Always produces visual content — DisplayTo guarantees valid state
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        // ...
    }

    // Static entry point — validates before constructing
    public static void DisplayTo(Mobile from)
    {
        if (!from.Alive || from.NetState == null)
            return;  // No gump created at all

        var data = GetData(from);
        if (data == null)
            return;  // No gump created at all

        from.SendGump(new MyGump(from, data));
    }
}
```

**Key points:**
- Constructor is `private` — enforces that `DisplayTo` is the only entry point
- All validation/short-circuiting happens in `DisplayTo` before `new MyGump(...)` is called
- If prerequisites fail, no gump is constructed or sent
- The constructor and `BuildLayout` can assume valid state and always produce visual output
- Reference implementation: `Projects/UOContent/Gumps/Go/GoGump.cs`

## Converting Legacy Gump to DynamicGump / StaticGump

The legacy `Gump` class (in `Gumps/Base/Legacy/Gump.cs`) builds layouts by appending `GumpEntry` objects to a list. The modern `DynamicGump` and `StaticGump<T>` use ref struct builders that write directly to buffers — fewer allocations, better performance.

### Step-by-Step Conversion

#### 1. Choose the target type

| If the layout... | Convert to |
|---|---|
| Is the same structure every time (fixed elements, maybe some dynamic text) | `StaticGump<T>` |
| Changes shape based on instance data (loops, conditionals that add/remove elements) | `DynamicGump` |

When in doubt, use `DynamicGump` — it's simpler and still much better than legacy `Gump`.

#### 2. Change the class declaration

```csharp
// Legacy
public class MyGump : Gump

// Modern — pick one:
public class MyGump : DynamicGump
public class MyGump : StaticGump<MyGump>
```

#### 3. Move layout code into BuildLayout

Legacy gumps build their layout in the constructor. Modern gumps build it in `BuildLayout`:

```csharp
// Legacy — layout in constructor
public class OldGump : Gump
{
    public OldGump(Mobile from) : base(50, 50)
    {
        AddPage(0);
        AddBackground(0, 0, 400, 300, 5054);
        AddLabel(20, 20, 0x480, "Hello");
        AddButton(20, 260, 4005, 4007, 1);
    }
}

// Modern DynamicGump — layout in BuildLayout
public class NewGump : DynamicGump
{
    private readonly Mobile _from;

    public override bool Singleton => true;

    private NewGump(Mobile from) : base(50, 50)
    {
        _from = from;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        builder.AddLabel(20, 20, 0x480, "Hello");
        builder.AddButton(20, 260, 4005, 4007, 1);
    }

    public static void DisplayTo(Mobile from)
    {
        from.SendGump(new NewGump(from));
    }
}
```

#### 4. Key API differences

| Legacy `Gump` | Modern builder |
|---|---|
| `AddPage(0)` | `builder.AddPage()` (0 is the default) |
| `AddHtml(x, y, w, h, text, bg, scroll)` | `builder.AddHtml(x, y, w, h, text, background: bg, scrollbar: scroll)` |
| `Closable = false` | `builder.SetNoClose()` |
| `Draggable = false` | `builder.SetNoMove()` |
| `Resizable = false` | `builder.SetNoResize()` |
| `Disposable = false` | `builder.SetNoDispose()` |
| `AddLabel(x, y, hue, string)` | `builder.AddLabel(x, y, hue, ReadOnlySpan<char>)` |
| `Intern(string)` / string list | Not needed — builder handles strings internally |

#### 5. For StaticGump: extract dynamic text into placeholders

If converting to `StaticGump<T>` and some text varies per instance, replace those `AddLabel`/`AddHtml` calls with placeholder versions and fill them in `BuildStrings`:

```csharp
// In BuildLayout:
builder.AddLabelPlaceholder(20, 20, 0x480, "playerName");
builder.AddHtmlPlaceholder(20, 50, 360, 200, "description", false, true);

// In BuildStrings:
protected override void BuildStrings(ref GumpStringsBuilder builder)
{
    builder.SetStringSlot("playerName", _from.Name);
    builder.SetHtmlText("description", _description, "#FFC000", 4);
}
```

#### 6. Update OnResponse signature

```csharp
// Legacy
public override void OnResponse(NetState sender, RelayInfo info)

// Modern (RelayInfo is passed by ref)
public override void OnResponse(NetState sender, in RelayInfo info)
```

#### 7. Add DisplayTo and make constructor private

Always add a static `DisplayTo` method and make the constructor private to prevent empty gumps (see Empty Gump Rule above).

## When to Use Which

| Scenario | Type | Reason |
|---|---|---|
| Confirmation dialog | StaticGump | Fixed layout, shown frequently |
| Warning prompt | StaticGump | Fixed layout |
| Settings menu | StaticGump | Fixed structure |
| Item list (variable length) | DynamicGump | Layout depends on data |
| Craft menu | DynamicGump | Player-specific recipes |
| Search results | DynamicGump | Variable result count |
| Vendor inventory | DynamicGump | Different items per vendor |

## Key File References

| File | Description |
|---|---|
| `Projects/UOContent/Gumps/Base/BaseGump.cs` | Abstract base class |
| `Projects/UOContent/Gumps/Base/StaticGump.cs` | Cached static gump |
| `Projects/UOContent/Gumps/Base/DynamicGump.cs` | Dynamic gump |
| `Projects/UOContent/Gumps/Base/StaticGumpBuilder.cs` | Static layout builder |
| `Projects/UOContent/Gumps/Base/DynamicGumpBuilder.cs` | Dynamic layout builder |
| `Projects/UOContent/Gumps/Base/GumpStringsBuilder.cs` | String slot builder |
| `Projects/UOContent/Gumps/Base/GumpLayoutBuilder.cs` | Shared layout methods |
| `Projects/UOContent/Gumps/Base/GumpSystem.cs` | Extension methods |
| `Projects/UOContent/Gumps/StaticWarningGump.cs` | Example static gump |
