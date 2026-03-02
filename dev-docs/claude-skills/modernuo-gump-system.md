---
name: modernuo-gump-system
description: >
  Trigger when creating or modifying gumps (UI dialogs). When working with BaseGump, StaticGump, DynamicGump, or GumpSystem.
---

# ModernUO Gump System

## When This Activates
- Creating new gumps (UI dialogs/windows)
- Modifying existing gump layouts
- Handling gump button responses
- Using `mobile.SendGump()`, `HasGump<T>()`, `CloseGump<T>()`

## Key Rules

1. **Use `StaticGump<TSelf>`** when layout is fixed (cached, better performance)
2. **Use `DynamicGump`** when layout depends on instance data (rebuilt each time)
3. **Use `mobile.SendGump(gump)`** to send -- requires `using Server.Gumps;`
4. **Override `Singleton => true`** if only one instance should be open per player
5. **Never forget `using Server.Gumps;`** -- extension methods won't resolve without it

## Hierarchy

```
BaseGump (abstract)
├── StaticGump<TSelf> -- Cached layout, use for menus/dialogs
└── DynamicGump      -- Rebuilt layout, use for dynamic content
```

## StaticGump Pattern (Preferred for Fixed Layouts)

```csharp
using Server.Gumps;

namespace Server.Gumps;

public class MyGump : StaticGump<MyGump>
{
    private readonly Mobile _player;
    private readonly string _message;

    public override bool Singleton => true;  // One per player

    public MyGump(Mobile player, string message) : base(50, 50)
    {
        _player = player;
        _message = message;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);

        // Static text (cached)
        builder.AddHtmlLocalized(10, 10, 380, 20, 1060635, 0x7800);  // "Warning"

        // Dynamic text placeholder (filled per-instance)
        builder.AddHtmlPlaceholder(10, 40, 380, 220, "content", false, true);

        // Buttons
        builder.AddButton(150, 265, 4005, 4007, 1);  // OK button (buttonID=1)
        builder.AddHtmlLocalized(185, 267, 100, 20, 1011036);  // "OK"

        builder.AddButton(250, 265, 4017, 4019, 0);  // Cancel (buttonID=0 = close)
        builder.AddHtmlLocalized(285, 267, 100, 20, 1011012);  // "Cancel"
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        // Fill dynamic placeholders
        builder.SetHtmlText("content", _message, "#FFC000");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            _player.SendMessage("You clicked OK!");
        }
    }
}
```

## DynamicGump Pattern (For Variable Layouts)

```csharp
using Server.Gumps;

namespace Server.Gumps;

public class InventoryGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly List<Item> _items;

    public override bool Singleton => true;

    public InventoryGump(Mobile player, List<Item> items) : base(50, 50)
    {
        _player = player;
        _items = items;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 40 + _items.Count * 30, 5054);
        builder.AddHtml(10, 10, 380, 20, "Inventory");

        for (var i = 0; i < _items.Count; i++)
        {
            var y = 40 + i * 30;
            builder.AddLabel(10, y, 0, _items[i].Name ?? "Unknown");
            builder.AddButton(350, y, 4005, 4007, i + 1);  // Button per item
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID > 0 && info.ButtonID <= _items.Count)
        {
            var item = _items[info.ButtonID - 1];
            _player.SendMessage($"Selected: {item.Name}");
        }
    }
}
```

## Sending and Managing Gumps

```csharp
using Server.Gumps;

// Send gump
mobile.SendGump(new MyGump(mobile, "Hello!"));

// Check if gump is open
if (mobile.HasGump<MyGump>())
{
    // Already open
}

// Find existing gump
var gump = mobile.FindGump<MyGump>();

// Close gump
mobile.CloseGump<MyGump>();
```

## Builder Methods Reference

### Layout Elements
```csharp
builder.AddPage(int page = 0);
builder.AddBackground(int x, int y, int width, int height, int gumpID);
builder.AddAlphaRegion(int x, int y, int width, int height);
builder.AddImageTiled(int x, int y, int width, int height, int gumpID);
builder.AddImage(int x, int y, int gumpID, int hue = 0);
builder.AddItem(int x, int y, int itemID, int hue = 0);
```

### Text
```csharp
builder.AddLabel(int x, int y, int hue, ReadOnlySpan<char> text);
builder.AddHtml(int x, int y, int w, int h, ReadOnlySpan<char> text, ...);
builder.AddHtmlLocalized(int x, int y, int w, int h, int number, ...);
builder.AddLabelPlaceholder(int x, int y, int hue, ReadOnlySpan<char> slotKey);
builder.AddHtmlPlaceholder(int x, int y, int w, int h, ReadOnlySpan<char> slotKey, ...);
```

### Interactive
```csharp
builder.AddButton(int x, int y, int normalID, int pressedID, int buttonID, ...);
builder.AddCheckbox(int x, int y, int inactiveID, int activeID, bool selected, int switchID);
builder.AddRadio(int x, int y, int inactiveID, int activeID, bool selected, int switchID);
builder.AddTextEntry(int x, int y, int w, int h, int hue, int entryID, ...);
```

### Modifiers
```csharp
builder.SetNoClose();    // Cannot close with right-click
builder.SetNoMove();     // Cannot move the gump
builder.SetNoResize();   // Cannot resize
builder.SetNoDispose();  // Cannot dispose
builder.AddTooltip(int number);  // Hover tooltip
```

## Response Handling

```csharp
public override void OnResponse(NetState sender, in RelayInfo info)
{
    var buttonID = info.ButtonID;       // 0 = close, 1+ = button clicks
    var isChecked = info.IsSwitched(0); // Checkbox/radio state by switchID
    var text = info.GetTextEntry(0);    // Text entry value by entryID
}
```

## Static vs Dynamic: When to Use Which

| Use StaticGump | Use DynamicGump |
|---|---|
| Confirmation dialogs | Lists of variable length |
| Static menus | Player-specific content |
| Warning prompts | Crafting interfaces |
| Settings panels | Search results |
| Help pages | Dynamic data display |

## Important Properties

### Singleton
```csharp
public override bool Singleton => true;
```
Automatically closes any existing instance of this gump type for the player before sending a new one. **Always set this for gumps that shouldn't stack.** Without it, repeated sends create duplicates the player must close individually.

### Cached (StaticGump only)
```csharp
protected virtual bool Cached => true;  // default
```
Controls whether `StaticGump<T>` caches its compiled layout bytes. Override to `false` **during development only** to force layout rebuild each send — useful for iterating on layout without restarting the server:
```csharp
protected override bool Cached => false;  // TEMPORARY — remove before commit
```

## Empty Gump Rule (CRITICAL AUDIT RULE)

**NEVER send a gump with no visual components.** An empty gump cannot be closed by the client — no close button, no right-click dismiss. This causes a **gump leak** on both client and server.

Empty gumps happen when short-circuiting inside a constructor or `BuildLayout`:
```csharp
// BAD — early return in constructor leaves gump empty but it still gets sent
public MyGump(Mobile from) : base(50, 50)
{
    if (!from.Alive) return;  // Empty gump — LEAK!
    // ...layout...
}
```

### Fix: Static DisplayTo Pattern
Validate prerequisites **before** constructing the gump. Make the constructor `private`:
```csharp
public class MyGump : DynamicGump
{
    public override bool Singleton => true;

    private MyGump(Mobile from, SomeData data) : base(50, 50) { /* store fields */ }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        // Always has visual content — DisplayTo guarantees valid state
    }

    public static void DisplayTo(Mobile from)
    {
        if (!from.Alive || from.NetState == null) return;  // No gump created
        var data = GetData(from);
        if (data == null) return;  // No gump created
        from.SendGump(new MyGump(from, data));
    }
}
```
Reference: `Projects/UOContent/Gumps/Go/GoGump.cs`

## Converting Legacy Gump to DynamicGump / StaticGump

### Choose target type
- Layout is fixed structure → `StaticGump<T>` (cached, best performance)
- Layout varies per instance (loops, conditionals) → `DynamicGump`
- When in doubt → `DynamicGump` (simpler, still much better than legacy)

### Conversion checklist
1. Change base class: `Gump` → `DynamicGump` or `StaticGump<MyGump>`
2. Move layout code from constructor into `BuildLayout(ref DynamicGumpBuilder builder)` or `BuildLayout(ref StaticGumpBuilder builder)`
3. Store any state the constructor used as fields (constructor now just stores state, doesn't build layout)
4. Replace property flags with builder methods:
   - `Closable = false` → `builder.SetNoClose()`
   - `Draggable = false` → `builder.SetNoMove()`
   - `Resizable = false` → `builder.SetNoResize()`
   - `Disposable = false` → `builder.SetNoDispose()`
5. `AddPage(0)` → `builder.AddPage()` (0 is default)
6. For `StaticGump<T>`: extract dynamic text into placeholders (`AddLabelPlaceholder` / `AddHtmlPlaceholder`) and fill in `BuildStrings(ref GumpStringsBuilder builder)`
7. Update `OnResponse` signature: `RelayInfo info` → `in RelayInfo info`
8. Add `public override bool Singleton => true;` if appropriate
9. Make constructor `private`, add `public static void DisplayTo(Mobile from)` method
10. Move all validation/short-circuit logic into `DisplayTo` (never leave `BuildLayout` empty)

### Example: legacy → modern
```csharp
// BEFORE (legacy Gump)
public class OldGump : Gump
{
    public OldGump(Mobile from) : base(50, 50)
    {
        Closable = false;
        AddPage(0);
        AddBackground(0, 0, 400, 300, 5054);
        AddLabel(20, 20, 0x480, from.Name);
        AddButton(20, 260, 4005, 4007, 1);
    }
    public override void OnResponse(NetState sender, RelayInfo info) { }
}

// AFTER (DynamicGump)
public class NewGump : DynamicGump
{
    private readonly Mobile _from;
    public override bool Singleton => true;

    private NewGump(Mobile from) : base(50, 50) => _from = from;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.SetNoClose();
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        builder.AddLabel(20, 20, 0x480, _from.Name);
        builder.AddButton(20, 260, 4005, 4007, 1);
    }

    public override void OnResponse(NetState sender, in RelayInfo info) { }

    public static void DisplayTo(Mobile from) => from.SendGump(new NewGump(from));
}
```

## Anti-Patterns

- **Missing `using Server.Gumps;`**: `SendGump()`, `HasGump<T>()` won't resolve
- **Creating DynamicGump for static layouts**: Wastes CPU rebuilding every time
- **Not setting `Singleton`**: Multiple copies of same gump stack up
- **ButtonID 0 for actions**: 0 means "close" -- use 1+ for action buttons
- **Empty gumps**: Short-circuiting in constructor/BuildLayout causes gump leaks — use `DisplayTo` pattern
- **Leaving `Cached => false` in production**: Defeats the purpose of StaticGump — only disable during development

## Real Examples
- StaticGump warning: `Projects/UOContent/Gumps/StaticWarningGump.cs`
- DynamicGump craft: `Projects/UOContent/Engines/Craft/Core/CraftGump.cs`
- GumpSystem extensions: `Projects/UOContent/Gumps/Base/GumpSystem.cs`
- StaticGumpBuilder: `Projects/UOContent/Gumps/Base/StaticGumpBuilder.cs`
- DynamicGumpBuilder: `Projects/UOContent/Gumps/Base/DynamicGumpBuilder.cs`
- BaseGump: `Projects/UOContent/Gumps/Base/BaseGump.cs`

## See Also
- `dev-docs/gump-system.md` - Complete gump documentation
- `dev-docs/claude-skills/modernuo-commands-targeting.md` - Commands that open gumps
