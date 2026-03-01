# ModernUO Commands & Targeting

This document covers ModernUO's command system for in-game `[` commands and the targeting system for player interactions.

## Command System

### Overview

Commands are prefixed with `[` by default (e.g., `[MyCommand`). They are registered in static `Configure()` methods and associated with an access level.

### Registration

```csharp
using Server.Commands;

namespace Server.Custom;

public static class MyCommands
{
    public static void Configure()
    {
        CommandSystem.Register("MyCommand", AccessLevel.GameMaster, MyCommand_OnCommand);
    }

    [Usage("MyCommand <name> [count]")]
    [Description("Does something with a name and optional count")]
    [Aliases("mc", "mycmd")]
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

### CommandSystem API

```csharp
public static class CommandSystem
{
    public static string Prefix { get; set; } = "[";
    public static Dictionary<string, CommandEntry> Entries { get; }

    public static void Register(string command, AccessLevel access, CommandEventHandler handler);
    public static bool Handle(Mobile from, string text, MessageType type = MessageType.Regular);
    public static string[] Split(string value);
}
```

### CommandEventArgs

```csharp
public class CommandEventArgs
{
    public Mobile Mobile { get; }        // Who issued the command
    public string Command { get; }       // Command name
    public string ArgString { get; }     // Raw argument string
    public string[] Arguments { get; }   // Split arguments
    public int Length { get; }           // Argument count

    // Typed accessors (return default if index out of range)
    public string GetString(int index);     // "" if missing
    public int GetInt32(int index);         // 0 if missing
    public uint GetUInt32(int index);       // 0 if missing
    public bool GetBoolean(int index);      // false if missing
    public double GetDouble(int index);     // 0.0 if missing
    public TimeSpan GetTimeSpan(int index); // TimeSpan.Zero if missing
}
```

### Access Levels

```csharp
public enum AccessLevel
{
    Player,        // 0 - Regular players
    Counselor,     // 1 - Support staff
    GameMaster,    // 2 - Game Masters
    Seer,          // 3 - Event coordinators
    Administrator, // 4 - Server administrators
    Developer,     // 5 - Developers
    Owner          // 6 - Server owner
}
```

### Command Attributes

```csharp
[Usage("CommandName <required> [optional]")]
// Documents command syntax. Displayed in help listings.

[Description("What this command does")]
// Documents command purpose. Displayed in help listings.

[Aliases("alias1", "alias2")]
// Alternative names for the command.
```

Defined in `Projects/Server/Attributes.cs`.

### Command + Targeting Pattern

A common pattern: command starts targeting, target handler performs the action.

```csharp
public static class HealCommand
{
    public static void Configure()
    {
        CommandSystem.Register("Heal", AccessLevel.GameMaster, Heal_OnCommand);
    }

    [Usage("Heal")]
    [Description("Fully heals a targeted mobile")]
    public static void Heal_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendMessage("Select a mobile to heal.");
        e.Mobile.Target = new HealTarget();
    }

    private class HealTarget : Target
    {
        public HealTarget() : base(-1, false, TargetFlags.Beneficial) { }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile m)
            {
                m.Hits = m.HitsMax;
                m.Mana = m.ManaMax;
                m.Stam = m.StamMax;
                m.Poison = null;
                from.SendMessage($"You have healed {m.Name}.");
            }
            else
            {
                from.SendMessage("That is not a mobile.");
            }
        }
    }
}
```

---

## Targeting System

### Overview

The targeting system allows players to select objects in the game world. When a target is set on a mobile, the client shows a targeting cursor. The player clicks on something, and the server processes the selection.

### Target Base Class

```csharp
public abstract class Target
{
    // Constructor
    protected Target(
        int range,           // Max range (-1 for unlimited)
        bool allowGround,    // Can target ground tiles
        TargetFlags flags    // None, Harmful, Beneficial
    );

    // Properties
    public int Range { get; set; }
    public bool AllowGround { get; set; }
    public TargetFlags Flags { get; set; }
    public bool CheckLOS { get; set; }          // Default: true
    public bool DisallowMultis { get; set; }    // Default: false
    public bool AllowNonlocal { get; set; }     // Default: false
    public int TargetID { get; }

    // Override for main handling
    protected virtual void OnTarget(Mobile from, object targeted);

    // Override for error handling
    protected virtual void OnTargetCancel(Mobile from, TargetCancelType cancelType);
    protected virtual void OnTargetFinish(Mobile from);
    protected virtual void OnTargetOutOfRange(Mobile from, object targeted);
    protected virtual void OnTargetOutOfLOS(Mobile from, object targeted);
    protected virtual void OnTargetNotAccessible(Mobile from, object targeted);
    protected virtual void OnTargetDeleted(Mobile from, object targeted);
    protected virtual void OnTargetUntargetable(Mobile from, object targeted);
    protected virtual void OnNonlocalTarget(Mobile from, object targeted);
    protected virtual void OnCantSeeTarget(Mobile from, object targeted);
    protected virtual void OnTargetInSecureTrade(Mobile from, object targeted);

    // Validation overrides
    protected virtual bool CanTarget(Mobile from, Mobile mobile, ref Point3D loc, ref Map map);
    protected virtual bool CanTarget(Mobile from, Item item, ref Point3D loc, ref Map map);
    protected virtual bool CanTarget(Mobile from, LandTarget land, ref Point3D loc, ref Map map);
    protected virtual bool CanTarget(Mobile from, StaticTarget st, ref Point3D loc, ref Map map);

    // Timeout
    public void BeginTimeout(Mobile from, long delay);
    public void CancelTimeout();
}
```

### TargetFlags

```csharp
[Flags]
public enum TargetFlags : byte
{
    None       = 0x00,  // Neutral targeting
    Harmful    = 0x01,  // Triggers criminal check, PvP flag
    Beneficial = 0x02   // Healing, buffing
}
```

### TargetCancelType

```csharp
public enum TargetCancelType
{
    Overridden,    // New target replaced this one
    Canceled,      // Player pressed Escape
    Disconnected,  // Player disconnected
    Timeout        // Target timed out
}
```

### Target Object Types

When `OnTarget` is called, the `targeted` parameter can be:

| Type | Description | Key Properties |
|---|---|---|
| `Mobile` | A player or creature | `.Name`, `.Hits`, `.Location` |
| `Item` | An item | `.Name`, `.ItemID`, `.Location` |
| `LandTarget` | Ground tile | `.Location`, `.TileID`, `.Name` |
| `StaticTarget` | Static map object | `.Location`, `.ItemID`, `.Hue` |

### Setting a Target

```csharp
// Set target on mobile (shows targeting cursor)
mobile.Target = new MyTarget();

// Cancel current target
Target.Cancel(mobile);
```

### Basic Target Implementation

```csharp
private class IdentifyTarget : Target
{
    public IdentifyTarget() : base(12, false, TargetFlags.None)
    {
        // CheckLOS = false;  // Uncomment to skip line-of-sight
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        switch (targeted)
        {
            case Mobile m:
                from.SendMessage($"Mobile: {m.Name}, Hits: {m.Hits}/{m.HitsMax}");
                break;
            case Item item:
                from.SendMessage($"Item: {item.Name ?? item.DefaultName}, ID: 0x{item.ItemID:X}");
                break;
            case LandTarget land:
                from.SendMessage($"Land at {land.Location}, Tile: {land.TileID}");
                break;
            case StaticTarget st:
                from.SendMessage($"Static at {st.Location}, ID: 0x{st.ItemID:X}");
                break;
        }
    }

    protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
    {
        if (cancelType == TargetCancelType.Canceled)
            from.SendMessage("Targeting cancelled.");
    }

    protected override void OnTargetFinish(Mobile from)
    {
        // Always called after success or cancel
    }
}
```

### Target Validation Flow

1. Client sends target response
2. Server validates: same map, within range, line of sight
3. Calls `CanTarget()` override for type-specific validation
4. If valid: calls `OnTarget()`
5. If invalid: calls appropriate error handler
6. Always calls `OnTargetFinish()` at the end

### SpellTarget

For spells, use the built-in `SpellTarget<T>`:

```csharp
public override void OnCast()
{
    Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
}

// The spell's Target(Mobile m) method is called when the player targets
```

---

## Best Practices

1. **Register commands in `Configure()`** -- it's called automatically during startup
2. **Validate argument count** before accessing -- `GetInt32()` returns 0 for missing args (not an error)
3. **Use appropriate access levels** -- don't give players GM commands
4. **Use `TargetFlags.Harmful`** for offensive actions (triggers criminal flagging)
5. **Use `TargetFlags.Beneficial`** for healing/buffing
6. **Handle `OnTargetCancel`** to provide feedback when player cancels
7. **Clean up in `OnTargetFinish`** if you have state to release

## Key File References

| File | Description |
|---|---|
| `Projects/Server/Commands.cs` | CommandSystem, CommandEventArgs |
| `Projects/Server/Attributes.cs` | Usage, Description, Aliases |
| `Projects/Server/Targeting/Target.cs` | Target base class |
| `Projects/Server/Targeting/TargetFlags.cs` | TargetFlags enum |
| `Projects/Server/Targeting/TargetCancelType.cs` | Cancel types |
| `Projects/Server/Targeting/LandTarget.cs` | Land target |
| `Projects/Server/Targeting/StaticTarget.cs` | Static target |
