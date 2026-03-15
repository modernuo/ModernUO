# Gump Migration

## Overview

RunUO uses a `Gump` base class where layout is built in the constructor using `AddXxx()` entry methods. ModernUO provides `StaticGump<T>` (cached layout) and `DynamicGump` (rebuilt per-instance) with ref struct builders, and enforces the empty gump rule.

## RunUO Pattern

```csharp
using Server.Gumps;
using Server.Network;

namespace Server.Gumps
{
    public class ConfirmGump : Gump
    {
        private Mobile m_From;
        private string m_Message;

        public ConfirmGump(Mobile from, string message) : base(50, 50)
        {
            m_From = from;
            m_Message = message;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 400, 300, 5054);
            AddAlphaRegion(10, 10, 380, 280);

            AddHtml(20, 20, 360, 200, message, true, true);

            AddButton(100, 260, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(135, 262, 100, 20, 1011036, false, false); // OK

            AddButton(250, 260, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(285, 262, 100, 20, 1011012, false, false); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                m_From.SendMessage("Confirmed!");
            }
        }
    }
}

// Usage:
from.SendGump(new ConfirmGump(from, "Are you sure?"));
```

## ModernUO Equivalent (DynamicGump)

```csharp
using Server.Gumps;

namespace Server.Gumps;

public class ConfirmGump : DynamicGump
{
    private readonly Mobile _from;
    private readonly string _message;

    public override bool Singleton => true;

    private ConfirmGump(Mobile from, string message) : base(50, 50)
    {
        _from = from;
        _message = message;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        builder.AddAlphaRegion(10, 10, 380, 280);

        builder.AddHtml(20, 20, 360, 200, _message, background: true, scrollbar: true);

        builder.AddButton(100, 260, 4005, 4007, 1);
        builder.AddHtmlLocalized(135, 262, 100, 20, 1011036); // OK

        builder.AddButton(250, 260, 4017, 4019, 0);
        builder.AddHtmlLocalized(285, 262, 100, 20, 1011012); // Cancel
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            _from.SendMessage("Confirmed!");
        }
    }

    public static void DisplayTo(Mobile from, string message)
    {
        if (from?.NetState == null)
            return;

        from.SendGump(new ConfirmGump(from, message));
    }
}

// Usage:
ConfirmGump.DisplayTo(from, "Are you sure?");
```

## ModernUO Equivalent (StaticGump — for fixed layouts)

```csharp
using Server.Gumps;

namespace Server.Gumps;

public class ConfirmGump : StaticGump<ConfirmGump>
{
    private readonly Mobile _from;
    private readonly string _message;

    public override bool Singleton => true;

    private ConfirmGump(Mobile from, string message) : base(50, 50)
    {
        _from = from;
        _message = message;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 400, 300, 5054);
        builder.AddAlphaRegion(10, 10, 380, 280);

        builder.AddHtmlPlaceholder(20, 20, 360, 200, "message", true, true);

        builder.AddButton(100, 260, 4005, 4007, 1);
        builder.AddHtmlLocalized(135, 262, 100, 20, 1011036);

        builder.AddButton(250, 260, 4017, 4019, 0);
        builder.AddHtmlLocalized(285, 262, 100, 20, 1011012);
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetStringSlot("message", _message);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
            _from.SendMessage("Confirmed!");
    }

    public static void DisplayTo(Mobile from, string message)
    {
        if (from?.NetState == null)
            return;
        from.SendGump(new ConfirmGump(from, message));
    }
}
```

## Migration Mapping Table

| RunUO | ModernUO | Notes |
|---|---|---|
| `class Foo : Gump` | `class Foo : DynamicGump` or `class Foo : StaticGump<Foo>` | Choose based on layout |
| Layout in constructor | Layout in `BuildLayout(ref builder)` | Move all AddXxx calls |
| `AddPage(0)` | `builder.AddPage()` | 0 is default |
| `AddBackground(...)` | `builder.AddBackground(...)` | Same args, on builder |
| `AddButton(x, y, n, p, id, GumpButtonType.Reply, 0)` | `builder.AddButton(x, y, n, p, id)` | Simplified — Reply is default |
| `AddButton(x, y, n, p, id, GumpButtonType.Page, p)` | `builder.AddButton(x, y, n, p, id, GumpButtonType.Page, p)` | Same for page nav |
| `AddHtml(x, y, w, h, text, bg, scroll)` | `builder.AddHtml(x, y, w, h, text, background: bg, scrollbar: scroll)` | Named params |
| `AddHtmlLocalized(x, y, w, h, num, bg, scroll)` | `builder.AddHtmlLocalized(x, y, w, h, num)` | Simplified |
| `AddLabel(x, y, hue, text)` | `builder.AddLabel(x, y, hue, text)` | Same |
| `AddTextEntry(x, y, w, h, hue, id, text)` | `builder.AddTextEntry(x, y, w, h, hue, id, text)` | Same |
| `AddCheck(x, y, off, on, state, id)` | `builder.AddCheckbox(x, y, off, on, state, id)` | Renamed |
| `AddRadio(x, y, off, on, state, id)` | `builder.AddRadio(x, y, off, on, state, id)` | Same |
| `Closable = false` | `builder.SetNoClose()` | Property → method |
| `Dragable = false` | `builder.SetNoMove()` | Property → method (note: Dragable → NoMove) |
| `Resizable = false` | `builder.SetNoResize()` | Property → method |
| `Disposable = false` | `builder.SetNoDispose()` | Property → method |
| `OnResponse(NetState, RelayInfo)` | `OnResponse(NetState, in RelayInfo)` | `in` keyword added |
| `info.TextEntries[i].Text` | `info.GetTextEntry(id)` | Direct lookup by ID |
| `info.Switches` contains check | `info.IsSwitched(switchID)` | Direct boolean check |
| `from.SendGump(new MyGump(...))` | `MyGump.DisplayTo(from, ...)` | Static entry point |
| `from.CloseGump(typeof(MyGump))` | `from.CloseGump<MyGump>()` | Generic method |
| `from.HasGump(typeof(MyGump))` | `from.HasGump<MyGump>()` | Generic method |

## Step-by-Step Conversion

### Step 1: Choose Target Type
| If the layout... | Convert to |
|---|---|
| Is the same structure every time | `StaticGump<T>` (best performance) |
| Changes based on data (loops, conditionals) | `DynamicGump` |
| You're unsure | `DynamicGump` (simpler, still much better than legacy `Gump`) |

### Step 2: Change Class Declaration
```csharp
// RunUO
public class MyGump : Gump

// ModernUO
public class MyGump : DynamicGump
// OR
public class MyGump : StaticGump<MyGump>
```

### Step 3: Add Singleton If Needed
```csharp
public override bool Singleton => true; // Only one instance per player
```

### Step 4: Make Constructor Private, Add DisplayTo
```csharp
private MyGump(Mobile from) : base(50, 50) { _from = from; }

public static void DisplayTo(Mobile from)
{
    if (from?.NetState == null)
        return;
    from.SendGump(new MyGump(from));
}
```

### Step 5: Move Layout to BuildLayout
Move all `AddXxx()` calls from the constructor to `BuildLayout(ref DynamicGumpBuilder builder)`, prefixing each with `builder.`.

### Step 6: Convert Properties to Builder Methods
```csharp
// RunUO properties in constructor
Closable = false;
Dragable = false;

// ModernUO methods in BuildLayout
builder.SetNoClose();
builder.SetNoMove();
```

### Step 7: Update OnResponse Signature
```csharp
// RunUO
public override void OnResponse(NetState sender, RelayInfo info)

// ModernUO
public override void OnResponse(NetState sender, in RelayInfo info)
```

### Step 8: Update Text Entry and Switch Access
```csharp
// RunUO
TextRelay relay = info.GetTextEntry(0);
string text = relay != null ? relay.Text : "";

// ModernUO
string text = info.GetTextEntry(0);
```

```csharp
// RunUO
bool isChecked = info.IsSwitched(switchID);
// ModernUO — same
bool isChecked = info.IsSwitched(switchID);
```

### Step 9: For StaticGump — Extract Dynamic Text
Replace variable text with placeholders:
```csharp
// In BuildLayout:
builder.AddLabelPlaceholder(x, y, hue, "playerName");
builder.AddHtmlPlaceholder(x, y, w, h, "description", false, true);

// In BuildStrings:
protected override void BuildStrings(ref GumpStringsBuilder builder)
{
    builder.SetStringSlot("playerName", _from.Name);
    builder.SetHtmlText("description", _text, "#FFC000", 4);
}
```

### Step 10: Update Callers
```csharp
// RunUO
from.SendGump(new MyGump(from));
from.CloseGump(typeof(MyGump));

// ModernUO
MyGump.DisplayTo(from);
from.CloseGump<MyGump>();
```

## Empty Gump Rule (CRITICAL)

**NEVER send a gump with no visual components.** An empty gump has no close button and cannot be dismissed — it leaks on both client and server.

This typically happens when constructor logic short-circuits:
```csharp
// BAD — gump is created empty, then sent
public MyGump(Mobile from) : base(50, 50)
{
    if (!from.Alive)
        return;  // Empty gump created!
    AddPage(0);
    // ...
}
```

**Fix**: Use the static `DisplayTo()` pattern. Validate before constructing:
```csharp
private MyGump(Mobile from) : base(50, 50) { /* always builds layout */ }

public static void DisplayTo(Mobile from)
{
    if (!from.Alive || from.NetState == null)
        return;  // No gump created at all
    from.SendGump(new MyGump(from));
}
```

## Edge Cases & Gotchas

### 1. using Server.Gumps Is Required
The extension methods `SendGump()`, `HasGump<T>()`, `FindGump<T>()`, `CloseGump<T>()` are in the `Server.Gumps` namespace. Without the using, they won't resolve.

### 2. StaticGump Caching
StaticGump caches layout bytes on first use. During development, override `Cached => false` to rebuild every time, but remove before committing.

### 3. Button ID 0 = Close
Button ID 0 is reserved for close/cancel. Use 1+ for action buttons.

### 4. GumpButtonType.Reply Is Default
In ModernUO, `AddButton(x, y, n, p, id)` defaults to Reply type. Only specify `GumpButtonType.Page` for page navigation buttons.

## See Also

- `dev-docs/gump-system.md` — Complete ModernUO gump reference
- `01-foundation-changes.md` — Foundation changes
