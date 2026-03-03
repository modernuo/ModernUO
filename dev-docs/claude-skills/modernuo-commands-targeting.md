---
name: modernuo-commands-targeting
description: >
  Trigger when creating in-game commands, targeting mechanics, or working with CommandSystem/Target. When implementing [commands or player interactions.
---

# ModernUO Commands & Targeting

## When This Activates
- Creating new in-game `[` commands
- Implementing targeting mechanics
- Working with `CommandSystem.Register()` or `Target` class
- Adding GM/admin tools

## Key Rules

1. **Register commands in `Configure()`** static method (called at startup)
2. **Use `[Usage]` and `[Description]`** attributes on handler methods
3. **Prefix is `[`** by default (e.g., `[mycommand`)
4. **Target inherits from `Target`** class, override `OnTarget()`
5. **Always validate inputs** in command handlers

## Command Registration

```csharp
using Server.Commands;

namespace Server.Custom;

public static class MyCommands
{
    public static void Configure()
    {
        CommandSystem.Register("MyCommand", AccessLevel.GameMaster, MyCommand_OnCommand);
        CommandSystem.Register("MyOtherCmd", AccessLevel.Player, MyOtherCmd_OnCommand);
    }

    [Usage("MyCommand <name> [count]")]
    [Description("Does something with a name and optional count")]
    public static void MyCommand_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Length < 1)
        {
            from.SendMessage("Usage: [MyCommand <name> [count]");
            return;
        }

        var name = e.GetString(0);
        var count = e.Length > 1 ? e.GetInt32(1) : 1;

        from.SendMessage($"Processing {name} x{count}");
    }
}
```

## CommandEventArgs

```csharp
e.Mobile        // Mobile who issued the command
e.Command       // Command name string
e.ArgString     // Raw argument string
e.Arguments     // string[] split arguments
e.Length         // Number of arguments

// Typed argument accessors:
e.GetString(0)  // Get string at index (empty string if missing)
e.GetInt32(1)   // Get int at index (0 if missing/invalid)
e.GetUInt32(0)  // Get uint at index
e.GetBoolean(0) // Get bool at index
e.GetDouble(0)  // Get double at index
e.GetTimeSpan(0) // Get TimeSpan at index
```

## Access Levels

```csharp
AccessLevel.Player        // Regular players
AccessLevel.Counselor     // Support staff
AccessLevel.GameMaster    // GMs
AccessLevel.Seer          // Event coordinators
AccessLevel.Administrator // Server admins
AccessLevel.Developer     // Developers
AccessLevel.Owner         // Server owner
```

## Target System

### Basic Target
```csharp
using Server.Targeting;

private class MyTarget : Target
{
    public MyTarget() : base(
        12,                    // Range (-1 for unlimited)
        false,                 // AllowGround
        TargetFlags.None       // None, Harmful, or Beneficial
    )
    {
        // Optional settings:
        // CheckLOS = false;      // Skip line-of-sight check
        // DisallowMultis = true; // Don't allow targeting multi objects
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is Mobile m)
        {
            from.SendMessage($"You targeted {m.Name}");
        }
        else if (targeted is Item item)
        {
            from.SendMessage($"You targeted item {item.Name}");
        }
        else if (targeted is LandTarget land)
        {
            from.SendMessage($"You targeted land at {land.Location}");
        }
        else if (targeted is StaticTarget st)
        {
            from.SendMessage($"You targeted static {st.ItemID}");
        }
    }

    protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
    {
        from.SendMessage("Targeting cancelled.");
    }

    protected override void OnTargetFinish(Mobile from)
    {
        // Always called after targeting completes (success or cancel)
    }
}

// Usage:
from.Target = new MyTarget();
```

### TargetFlags
```csharp
TargetFlags.None       // Neutral targeting
TargetFlags.Harmful    // Criminal check, combat targeting
TargetFlags.Beneficial // Healing, buffing
```

### Target Object Types
- `Mobile` -- a player or creature
- `Item` -- an item in the world or container
- `LandTarget` -- ground tile (`Location`, `TileID`, `Name`)
- `StaticTarget` -- static map object (`Location`, `ItemID`, `Name`, `Hue`)

### Command + Target Pattern
```csharp
public static void Configure()
{
    CommandSystem.Register("Tame", AccessLevel.GameMaster, Tame_OnCommand);
}

[Usage("Tame")]
[Description("Force-tames a creature")]
public static void Tame_OnCommand(CommandEventArgs e)
{
    e.Mobile.SendMessage("Select a creature to tame.");
    e.Mobile.Target = new TameTarget();
}

private class TameTarget : Target
{
    public TameTarget() : base(-1, false, TargetFlags.None) { }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is BaseCreature { Tamable: true } creature)
        {
            creature.SetControlMaster(from);
            from.SendMessage($"You have tamed {creature.Name}.");
        }
        else
        {
            from.SendMessage("That cannot be tamed.");
        }
    }
}
```

### Target Validation Overrides
```csharp
// Override these for custom validation:
protected override bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map)
protected override bool CanTarget(Mobile from, Item item, ref Point3D loc, ref Map map)
protected override bool CanTarget(Mobile from, LandTarget land, ref Point3D loc, ref Map map)
protected override bool CanTarget(Mobile from, StaticTarget st, ref Point3D loc, ref Map map)

// Error handlers:
protected override void OnTargetOutOfRange(Mobile from, object targeted)
protected override void OnTargetOutOfLOS(Mobile from, object targeted)
protected override void OnTargetNotAccessible(Mobile from, object targeted)
protected override void OnTargetDeleted(Mobile from, object targeted)
protected override void OnTargetUntargetable(Mobile from, object targeted)
```

## Anti-Patterns

- **Registering commands outside `Configure()`**: Won't be called during startup
- **Missing access level validation**: Always set appropriate `AccessLevel`
- **Not checking `e.Length`**: Accessing missing arguments returns defaults silently
- **Hardcoded range in targeting**: Use -1 for unlimited, appropriate range for game mechanics

## Real Examples
- Command registration: `Projects/UOContent/Commands/StaffAccess.cs`
- Target from item: `Projects/UOContent/Items/Misc/InteriorDecorator.cs`
- Spell targeting: `Projects/UOContent/Spells/First/MagicArrow.cs`
- Command system: `Projects/Server/Commands.cs`
- Target base: `Projects/Server/Targeting/Target.cs`
- Attributes: `Projects/Server/Attributes.cs` (Usage, Description, Aliases)

## See Also
- `dev-docs/commands-targeting.md` - Complete documentation
- `dev-docs/claude-skills/modernuo-gump-system.md` - Commands that open gumps
- `dev-docs/claude-skills/modernuo-content-patterns.md` - Content creation
