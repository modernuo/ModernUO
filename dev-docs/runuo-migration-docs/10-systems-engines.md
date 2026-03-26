# Systems & Engines Migration

## Overview

RunUO community scripts often include multi-file systems: custom crafting extensions, spawner systems, economy engines, housing addons, quest systems. Migrating these requires applying all prior changes (serialization, timers, events, persistence) systematically across multiple interdependent files.

## Approach

### 1. Map the System
Before changing code, understand the system's structure:
- List all files in the system
- Identify entry points (Configure/Initialize methods)
- Map class dependencies (what references what)
- Identify serialized types (anything with Serialize/Deserialize)
- Identify persistence (EventSink.WorldSave handlers)
- Identify timers (Timer subclasses, DelayCall)
- Identify gumps (Gump subclasses)
- Identify packets (Packet subclasses)

### 2. Conversion Order
Convert files in dependency order:
1. **Data types / enums** — No serialization changes needed, just naming/namespace
2. **Persistence classes** — Convert EventSink.WorldSave to GenericPersistence
3. **Core entities (Items/Mobiles)** — Serialization, timers, properties
4. **Gumps** — Convert to DynamicGump/StaticGump
5. **Commands** — Usually minimal changes
6. **Packets** — Convert to SpanWriter if custom packets exist
7. **Entry point / registration** — Update Configure/Initialize

### 3. File Organization
RunUO scripts are in `Scripts/` with arbitrary structure. ModernUO expects files in `Projects/UOContent/`:

| RunUO Location | ModernUO Location |
|---|---|
| `Scripts/Custom/MySystem/` | `Projects/UOContent/Engines/MySystem/` or `Projects/UOContent/Systems/MySystem/` |
| `Scripts/Items/MyItem.cs` | `Projects/UOContent/Items/{Category}/MyItem.cs` |
| `Scripts/Mobiles/MyMobile.cs` | `Projects/UOContent/Mobiles/{Category}/MyMobile.cs` |
| `Scripts/Gumps/MyGump.cs` | `Projects/UOContent/Gumps/MyGump.cs` |
| `Scripts/Commands/MyCommand.cs` | Keep with parent system or `Projects/UOContent/Commands/` |

## Pattern: Multi-File System Migration

### Example: A Custom Bounty System

**RunUO structure:**
```
Scripts/Custom/BountySystem/
├── BountySystem.cs          (EventSink.WorldSave/Load, core logic)
├── BountyBoard.cs           (Item - the board in town)
├── BountyBoardGump.cs       (Gump - UI)
├── BountyEntry.cs           (Data class - not serialized as entity)
├── BountyCommands.cs        (Commands)
└── BountyTimer.cs           (Timer for bounty expiry)
```

**Migration steps:**

#### File 1: BountyEntry.cs (Data class — simplest)
```csharp
// RunUO
namespace Server.Custom.BountySystem
{
    public class BountyEntry
    {
        private Mobile m_Target;
        private Mobile m_Poster;
        private int m_Amount;
        private DateTime m_Expiry;

        public Mobile Target { get { return m_Target; } }
        public Mobile Poster { get { return m_Poster; } }
        public int Amount { get { return m_Amount; } set { m_Amount = value; } }
        public DateTime Expiry { get { return m_Expiry; } }

        public BountyEntry(Mobile target, Mobile poster, int amount, DateTime expiry)
        {
            m_Target = target;
            m_Poster = poster;
            m_Amount = amount;
            m_Expiry = expiry;
        }
    }
}

// ModernUO
namespace Server.Custom.BountySystem;

public class BountyEntry
{
    public Mobile Target { get; }
    public Mobile Poster { get; }
    public int Amount { get; set; }
    public DateTime Expiry { get; }

    public BountyEntry(Mobile target, Mobile poster, int amount, DateTime expiry)
    {
        Target = target;
        Poster = poster;
        Amount = amount;
        Expiry = expiry;
    }
}
```

#### File 2: BountySystem.cs (Persistence — convert to GenericPersistence)
```csharp
// ModernUO
namespace Server.Custom.BountySystem;

public class BountySystem : GenericPersistence
{
    private static BountySystem _instance;
    private static readonly List<BountyEntry> _bounties = new();

    public static IReadOnlyList<BountyEntry> Bounties => _bounties;

    public static void Configure()
    {
        _instance = new BountySystem();
    }

    public BountySystem() : base("BountySystem", 10) { }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0);
        writer.WriteEncodedInt(_bounties.Count);
        foreach (var b in _bounties)
        {
            writer.Write(b.Target);
            writer.Write(b.Poster);
            writer.Write(b.Amount);
            writer.Write(b.Expiry);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();
        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var target = reader.ReadEntity<Mobile>();
            var poster = reader.ReadEntity<Mobile>();
            var amount = reader.ReadInt();
            var expiry = reader.ReadDateTime();
            if (target != null && poster != null)
                _bounties.Add(new BountyEntry(target, poster, amount, expiry));
        }
    }

    public static void AddBounty(BountyEntry entry)
    {
        _bounties.Add(entry);
        _instance.MarkDirty();
    }

    public static void RemoveBounty(BountyEntry entry)
    {
        _bounties.Remove(entry);
        _instance.MarkDirty();
    }
}
```

#### File 3: BountyBoard.cs (Item — serialization + timer)
```csharp
// ModernUO
using ModernUO.Serialization;
using Server.Gumps;

namespace Server.Custom.BountySystem;

[SerializationGenerator(0)]
public partial class BountyBoard : Item
{
    [Constructible]
    public BountyBoard() : base(0x1E5E)
    {
        Movable = false;
    }

    public override string DefaultName => "a bounty board";

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendLocalizedMessage(500446);
            return;
        }

        BountyBoardGump.DisplayTo(from);
    }
}
```

#### File 4: BountyBoardGump.cs (Gump — convert to DynamicGump)
```csharp
// ModernUO
using Server.Gumps;

namespace Server.Custom.BountySystem;

public class BountyBoardGump : DynamicGump
{
    private readonly Mobile _from;

    public override bool Singleton => true;

    private BountyBoardGump(Mobile from) : base(50, 50)
    {
        _from = from;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var bounties = BountySystem.Bounties;
        var height = 80 + bounties.Count * 30;

        builder.AddPage();
        builder.AddBackground(0, 0, 400, height, 5054);
        builder.AddAlphaRegion(10, 10, 380, height - 20);
        builder.AddLabel(20, 15, 0x480, "Active Bounties");

        for (var i = 0; i < bounties.Count; i++)
        {
            var b = bounties[i];
            var y = 45 + i * 30;
            builder.AddLabel(20, y, 0x480, b.Target.Name ?? "Unknown");
            builder.AddLabel(200, y, 0x480, $"{b.Amount} gold");
            builder.AddButton(350, y, 4005, 4007, i + 1);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID > 0)
        {
            var index = info.ButtonID - 1;
            var bounties = BountySystem.Bounties;
            if (index < bounties.Count)
                _from.SendMessage($"Bounty on {bounties[index].Target.Name}: {bounties[index].Amount} gold");
        }
    }

    public static void DisplayTo(Mobile from)
    {
        if (from?.NetState == null)
            return;
        from.SendGump(new BountyBoardGump(from));
    }
}
```

#### File 5: BountyCommands.cs (Commands — minimal changes)
```csharp
// ModernUO
using Server.Commands;

namespace Server.Custom.BountySystem;

public static class BountyCommands
{
    public static void Configure()
    {
        CommandSystem.Register("Bounty", AccessLevel.Player, Bounty_OnCommand);
    }

    [Usage("Bounty <amount>")]
    [Description("Place a bounty on a targeted player")]
    public static void Bounty_OnCommand(CommandEventArgs e)
    {
        if (e.Length < 1)
        {
            e.Mobile.SendMessage("Usage: [Bounty <amount>");
            return;
        }

        var amount = e.GetInt32(0);
        e.Mobile.SendMessage("Target the player to place a bounty on.");
        e.Mobile.Target = new BountyTarget(amount);
    }
}
```

## Cross-Reference Handling

When files reference each other:
1. Convert shared data types first
2. Convert the persistence/core system next
3. Convert consumers (gumps, commands) last
4. Keep all files in the same namespace

## Common Multi-File Patterns

### Crafting Extension
| RunUO File | ModernUO Conversion |
|---|---|
| CraftItem subclass | `[SerializationGenerator]`, `partial` |
| CraftSystem registration | `Configure()` method |
| Resource definition | Data class, auto-properties |
| Custom tools (Item) | Standard item migration |

### Custom Spawner
| RunUO File | ModernUO Conversion |
|---|---|
| Spawner Item | `[SerializationGenerator]`, `[AfterDeserialization]` for timers |
| Spawn timer | `TimerExecutionToken`, fire-and-forget |
| Config file | `JsonConfig` or `ServerConfiguration` |
| Admin gump | `DynamicGump` conversion |

### Economy/Banking System
| RunUO File | ModernUO Conversion |
|---|---|
| Data persistence | `GenericPersistence` |
| Account data | Serialized fields or `GenericPersistence` |
| Transaction log | `GenericPersistence` |
| Player-facing gump | `DynamicGump` or `StaticGump` |

## Configuration Migration

RunUO systems often use XML or custom config files:
```csharp
// RunUO
XmlDocument doc = new XmlDocument();
doc.Load("Data/MyConfig.xml");

// ModernUO — use ServerConfiguration for simple settings
var enabled = ServerConfiguration.GetOrUpdateSetting("mySystem.enabled", true);
var maxItems = ServerConfiguration.GetOrUpdateSetting("mySystem.maxItems", 100);

// ModernUO — use JsonConfig for complex settings
var config = JsonConfig.Deserialize<MyConfig>(configPath);
```

## Testing the Migration

After converting all files:
1. Compile the project: `dotnet build`
2. Fix any compilation errors
3. Start the server and check for runtime errors
4. Test each feature:
   - Items can be `[add`ed
   - Gumps display correctly
   - Commands work
   - Data persists across saves/restarts
   - Timers fire correctly

## See Also

- `dev-docs/content-patterns.md` — File organization patterns
- `dev-docs/configuration.md` — Configuration system reference
- All prior migration docs (01 through 09)
