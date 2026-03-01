---
name: modernuo-events
description: >
  Trigger when subscribing to or creating game events, working with EventSink or generated events. When hooking into player login, death, speech, or other game events.
---

# ModernUO Events System

## When This Activates
- Subscribing to game events (login, logout, death, speech)
- Creating new events
- Working with `EventSink` static events
- Using `[GeneratedEvent]` / `[OnEvent]` attributes

## Key Rules

1. **Subscribe to events in `Configure()`** static method
2. **EventSink events are `static event Action<T>`** -- subscribe with `+=`
3. **Event handlers must match the delegate signature**
4. **Unsubscribe if your system can be disabled** (prevent leaks)

## EventSink Events

Subscribe in `Configure()`:
```csharp
public static void Configure()
{
    EventSink.Connected += OnPlayerConnected;
    EventSink.Disconnected += OnPlayerDisconnected;
    EventSink.Logout += OnLogout;
    EventSink.ServerStarted += OnServerStarted;
}
```

### Available Events

#### Core Lifecycle
```csharp
EventSink.ServerStarted      // Action              -- Server fully started
EventSink.Shutdown            // Action              -- Server shutting down
EventSink.WorldLoad           // Action              -- World loaded from saves
EventSink.WorldSave           // Action              -- World save triggered
EventSink.WorldSavePostSnapshot // Action<WorldSavePostSnapshotEventArgs>
```

#### Player Connection
```csharp
EventSink.Connected           // Action<Mobile>      -- Player connected
EventSink.BeforeDisconnected  // Action<Mobile>      -- About to disconnect
EventSink.Disconnected        // Action<Mobile>      -- Player disconnected
EventSink.Logout              // Action<Mobile>      -- Player logged out
```

#### Account
```csharp
EventSink.AccountLogin        // Action<AccountLoginEventArgs>
// AccountLoginEventArgs: .State (NetState), .Username, .Password, .Accepted (set), .RejectReason (set)
```

#### Communication
```csharp
EventSink.Speech              // Action<SpeechEventArgs>
// SpeechEventArgs: .Mobile, .Speech, .Type, .Hue, .Keywords, .Handled (set), .Blocked (set)
```

#### Combat
```csharp
EventSink.AggressiveAction    // Action<AggressiveActionEventArgs>
// AggressiveActionEventArgs: .Aggressed, .Aggressor, .Criminal
```

#### Movement
```csharp
EventSink.Movement            // Action<MovementEventArgs>
// MovementEventArgs: .Mobile, .Direction, .Blocked (set)
```

#### Network
```csharp
EventSink.SocketConnect       // Action<SocketConnectEventArgs>
// SocketConnectEventArgs: .Address, .AllowConnection (set)

EventSink.ServerCrashed       // Action<ServerCrashedEventArgs>
// ServerCrashedEventArgs: .Exception, .Close (set)
```

#### UI
```csharp
EventSink.PaperdollRequest    // Action<Mobile, Mobile>  -- (beholder, beheld)
```

## Event Handler Pattern

```csharp
public static class MyEventSystem
{
    public static void Configure()
    {
        EventSink.Connected += OnConnected;
        EventSink.Speech += OnSpeech;
    }

    private static void OnConnected(Mobile m)
    {
        if (m is PlayerMobile pm)
        {
            pm.SendMessage("Welcome back!");
        }
    }

    private static void OnSpeech(SpeechEventArgs e)
    {
        if (e.Speech.InsensitiveContains("help"))
        {
            e.Mobile.SendMessage("How can I help you?");
            e.Handled = true;  // Prevent further processing
        }
    }
}
```

## Generated Events (Code-Generated)

For custom events on game entities, use the CodeGeneratedEvents package:

### Defining Events
```csharp
// On the class that fires the event:
[GeneratedEvent(nameof(PlayerLoginEvent))]
public static partial void PlayerLoginEvent(PlayerMobile player);
```

### Subscribing to Events
```csharp
// On the handler class:
[OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
public static void HandleLogin(PlayerMobile player)
{
    // Handle the event
}
```

### Known Generated Events
- `PlayerMobile.PlayerLoginEvent`
- `PlayerMobile.PlayerDeathEvent`
- `BaseCreature.CreatureDeathEvent`

External reference: https://github.com/modernuo/CodeGeneratedEvents

## EventArgs Pool Pattern

Some EventArgs use object pooling to avoid allocation:
```csharp
// Movement uses pooling:
var args = MovementEventArgs.Create(mobile, dir);
EventSink.InvokeMovement(args);
args.Free();  // Return to pool

// AggressiveAction uses pooling:
var args = AggressiveActionEventArgs.Create(aggressed, aggressor, criminal);
EventSink.InvokeAggressiveAction(args);
args.Free();
```

## Anti-Patterns

- **Subscribing outside `Configure()`**: Won't be called during startup
- **Not checking player type**: `EventSink.Connected` fires for all mobiles, cast to `PlayerMobile` if needed
- **Blocking in event handlers**: Event handlers run on the game loop -- keep them fast
- **Not unsubscribing**: If system can be disabled, unsubscribe to prevent leaks

## Real Examples
- EventSink core: `Projects/Server/Events/EventSink.cs`
- Speech events: `Projects/Server/Events/SpeechEvent.cs`
- Movement events: `Projects/Server/Events/MovementEvent.cs`
- Account events: `Projects/Server/Events/AccountLoginEvent.cs`
- World events: `Projects/Server/Events/EventSink.cs` (WorldLoad, WorldSave, ServerStarted, Shutdown)
- Connection events: `Projects/Server/Events/SocketConnectionEvent.cs`
- GumpSystem subscription: `Projects/UOContent/Gumps/Base/GumpSystem.cs`

## See Also
- `dev-docs/events.md` - Complete events documentation
- `dev-docs/claude-skills/modernuo-content-patterns.md` - Content hooks
- `dev-docs/claude-skills/modernuo-configuration.md` - Configure() pattern
