# Commands & Events Migration

## Overview

Commands are largely similar between RunUO and ModernUO — the `CommandSystem.Register()` API is the same. The main changes are handler attribute conventions and that events now use `Action<T>` delegates instead of custom delegate types.

## Command Changes

### Registration (Same Pattern)
```csharp
// Both RunUO and ModernUO
public static void Configure()  // ModernUO uses Configure(), RunUO may use Initialize()
{
    CommandSystem.Register("MyCommand", AccessLevel.GameMaster, MyCommand_OnCommand);
}
```

**Key difference**: ModernUO prefers `Configure()` for registration. RunUO often uses `Initialize()`. Both work, but `Configure()` runs earlier in the startup sequence and is the convention.

### Handler Attributes
```csharp
// RunUO
[Usage("MyCommand <arg>")]
[Description("Does something")]

// ModernUO — same attributes, plus optional Aliases
[Usage("MyCommand <arg>")]
[Description("Does something")]
[Aliases("mc", "mycmd")]
```

### CommandEventArgs (Same)
```csharp
public static void MyCommand_OnCommand(CommandEventArgs e)
{
    var from = e.Mobile;
    var name = e.GetString(0);
    var count = e.Length > 1 ? e.GetInt32(1) : 1;
}
```

No changes needed for command handlers themselves.

## Event System Migration

### RunUO EventSink Pattern
```csharp
// RunUO — custom delegate types
public static void Initialize()
{
    EventSink.WorldSave += new WorldSaveEventHandler(OnWorldSave);
    EventSink.WorldLoad += new WorldLoadEventHandler(OnWorldLoad);
    EventSink.Login += new LoginEventHandler(OnLogin);
    EventSink.Logout += new LogoutEventHandler(OnLogout);
    EventSink.Speech += new SpeechEventHandler(OnSpeech);
    EventSink.Movement += new MovementEventHandler(OnMovement);
    EventSink.ServerStarted += new ServerStartedEventHandler(OnServerStarted);
    EventSink.Crashed += new CrashedEventHandler(OnCrashed);
}

private static void OnWorldSave(WorldSaveEventArgs e)
{
    // Save data to file
}

private static void OnLogin(LoginEventArgs e)
{
    Mobile m = e.Mobile;
    m.SendMessage("Welcome!");
}
```

### ModernUO EventSink Pattern
```csharp
// ModernUO — Action<T> delegates, changed event names
public static void Configure()
{
    EventSink.WorldSave += OnWorldSave;       // Action (no args)
    EventSink.WorldLoad += OnWorldLoad;       // Action (no args)
    EventSink.Connected += OnConnected;        // Action<Mobile> — was Login
    EventSink.Disconnected += OnDisconnected;  // Action<Mobile> — was Logout
    EventSink.Speech += OnSpeech;             // Action<SpeechEventArgs>
    EventSink.Movement += OnMovement;         // Action<MovementEventArgs>
    EventSink.ServerStarted += OnServerStarted; // Action (no args)
    EventSink.ServerCrashed += OnCrashed;     // Action<ServerCrashedEventArgs>
}

private static void OnWorldSave()
{
    // For persistence, use GenericPersistence instead (see 08-persistence.md)
}

private static void OnConnected(Mobile m)
{
    m.SendMessage("Welcome!");
}
```

## Event Migration Mapping

| RunUO Event | ModernUO Event | Signature Change |
|---|---|---|
| `EventSink.WorldSave` | `EventSink.WorldSave` | `WorldSaveEventArgs` → `Action` (no args) |
| `EventSink.WorldLoad` | `EventSink.WorldLoad` | `WorldLoadEventArgs` → `Action` (no args) |
| `EventSink.Login` | `EventSink.Connected` | `LoginEventArgs` → `Action<Mobile>` |
| `EventSink.Logout` | `EventSink.Disconnected` | `LogoutEventArgs` → `Action<Mobile>` |
| `EventSink.Speech` | `EventSink.Speech` | `SpeechEventArgs` (mostly same) |
| `EventSink.Movement` | `EventSink.Movement` | `MovementEventArgs` (mostly same) |
| `EventSink.ServerStarted` | `EventSink.ServerStarted` | `Action` (no args) |
| `EventSink.Crashed` | `EventSink.ServerCrashed` | Renamed |
| `EventSink.AggressiveAction` | `EventSink.AggressiveAction` | `AggressiveActionEventArgs` |
| `EventSink.AccountLogin` | `EventSink.AccountLogin` | `AccountLoginEventArgs` |
| `EventSink.SocketConnect` | `EventSink.SocketConnect` | `SocketConnectEventArgs` |
| `EventSink.BeforeWorldSave` | Removed | Use `WorldSave` event directly |
| `EventSink.Shutdown` | `EventSink.Shutdown` | `Action` |
| `EventSink.CharacterCreated` | Removed/Restructured | Check current source |
| `EventSink.OpenDoorMacroUsed` | Removed | Handle in movement/speech |
| `EventSink.PlayerDeath` | Use CodeGeneratedEvents | `PlayerMobile.PlayerDeathEvent` |
| `EventSink.CreatureDeath` | Use CodeGeneratedEvents | `BaseCreature.CreatureDeathEvent` |

## Step-by-Step Conversion

### Step 1: Change Initialize() to Configure()
```csharp
// RunUO
public static void Initialize()

// ModernUO
public static void Configure()
```

### Step 2: Remove Delegate Type Constructors
```csharp
// RunUO
EventSink.Login += new LoginEventHandler(OnLogin);

// ModernUO
EventSink.Connected += OnConnected;
```

### Step 3: Update Event Names
Rename `Login` to `Connected`, `Logout` to `Disconnected`, etc. (see mapping table).

### Step 4: Update Handler Signatures
```csharp
// RunUO
private static void OnLogin(LoginEventArgs e)
{
    Mobile m = e.Mobile;
}

// ModernUO
private static void OnConnected(Mobile m)
{
    // Mobile is passed directly
}
```

### Step 5: WorldSave/WorldLoad → GenericPersistence
If the event handler was saving/loading data to binary files, convert to `GenericPersistence` instead. See `08-persistence.md`.

```csharp
// RunUO — manual file persistence via EventSink
EventSink.WorldSave += new WorldSaveEventHandler(Save);
EventSink.WorldLoad += new WorldLoadEventHandler(Load);

// ModernUO — use GenericPersistence class (see 08-persistence.md)
// Don't use EventSink.WorldSave for custom persistence
```

## CodeGeneratedEvents

For entity-specific events, ModernUO uses source-generated events:

```csharp
// Subscribing to a generated event
[OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
public static void HandlePlayerLogin(PlayerMobile player)
{
    // Handle player login
}
```

Known generated events:
- `PlayerMobile.PlayerLoginEvent`
- `PlayerMobile.PlayerDeathEvent`
- `BaseCreature.CreatureDeathEvent`

## Edge Cases & Gotchas

### 1. WorldSave No Longer Has EventArgs
RunUO's `WorldSaveEventArgs` contained the save path. In ModernUO, use `WorldSavePostSnapshot` event if you need paths, or better yet, use `GenericPersistence`.

### 2. Login/Logout → Connected/Disconnected
The names changed AND the signatures changed. `LoginEventArgs.Mobile` → direct `Mobile` parameter.

### 3. Subscribe in Configure(), Not Initialize()
`Configure()` runs before `Initialize()`. Event subscriptions should happen early.

### 4. Pooled EventArgs
Some events (Movement, AggressiveAction) use pooled args. Don't store references to them — they get recycled.

### 5. Handled/Blocked Properties
`SpeechEventArgs.Handled` and `.Blocked` work the same. Set `Handled = true` to consume, `Blocked = true` to block.

## See Also

- `dev-docs/events.md` — Complete ModernUO event system reference
- `dev-docs/commands-targeting.md` — Complete command/targeting reference
- `08-persistence.md` — Converting WorldSave persistence
- `01-foundation-changes.md` — Foundation changes
