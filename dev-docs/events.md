# ModernUO Event System

This document covers ModernUO's event system, including EventSink static events and the CodeGeneratedEvents system for custom entity events.

## Overview

ModernUO provides two event mechanisms:
1. **EventSink**: Static events for core game lifecycle (login, logout, death, speech, etc.)
2. **CodeGeneratedEvents**: Attribute-based events on game entities (player login, creature death, etc.)

## EventSink

### Architecture

`EventSink` is a `static partial class` spread across multiple files in `Projects/Server/Events/`. Each event is defined as a `public static event Action<T>` with a corresponding `InvokeXxx()` method.

### Subscribing to Events

Subscribe in your `Configure()` static method:

```csharp
public static class MySystem
{
    public static void Configure()
    {
        EventSink.Connected += OnPlayerConnected;
        EventSink.Disconnected += OnPlayerDisconnected;
        EventSink.Speech += OnSpeech;
        EventSink.ServerStarted += OnServerStarted;
    }

    private static void OnPlayerConnected(Mobile m)
    {
        if (m is PlayerMobile pm)
            pm.SendMessage("Welcome to the server!");
    }

    private static void OnPlayerDisconnected(Mobile m)
    {
        // Cleanup player state
    }

    private static void OnSpeech(SpeechEventArgs e)
    {
        if (e.Speech.InsensitiveContains("help"))
        {
            e.Mobile.SendMessage("Type [help for commands.");
            e.Handled = true;
        }
    }

    private static void OnServerStarted()
    {
        // Initialize after all systems loaded
    }
}
```

### Available Events

#### Server Lifecycle
| Event | Signature | When |
|---|---|---|
| `ServerStarted` | `Action` | Server fully initialized |
| `Shutdown` | `Action` | Server shutting down |
| `WorldLoad` | `Action` | World loaded from saves |
| `WorldSave` | `Action` | World save triggered |
| `WorldSavePostSnapshot` | `Action<WorldSavePostSnapshotEventArgs>` | After save snapshot |
| `ServerCrashed` | `Action<ServerCrashedEventArgs>` | Unhandled exception |

#### Player Connection
| Event | Signature | When |
|---|---|---|
| `Connected` | `Action<Mobile>` | Player connected to server |
| `BeforeDisconnected` | `Action<Mobile>` | About to disconnect |
| `Disconnected` | `Action<Mobile>` | Player disconnected |
| `Logout` | `Action<Mobile>` | Player logged out |

#### Account
| Event | Signature | When |
|---|---|---|
| `AccountLogin` | `Action<AccountLoginEventArgs>` | Account login attempt |

#### Communication
| Event | Signature | When |
|---|---|---|
| `Speech` | `Action<SpeechEventArgs>` | Player speaks |
| `PaperdollRequest` | `Action<Mobile, Mobile>` | Paperdoll opened (beholder, beheld) |

#### Combat
| Event | Signature | When |
|---|---|---|
| `AggressiveAction` | `Action<AggressiveActionEventArgs>` | Aggressive action taken |

#### Movement
| Event | Signature | When |
|---|---|---|
| `Movement` | `Action<MovementEventArgs>` | Player moves |

#### Network
| Event | Signature | When |
|---|---|---|
| `SocketConnect` | `Action<SocketConnectEventArgs>` | New socket connection |

### EventArgs Classes

#### SpeechEventArgs
```csharp
public class SpeechEventArgs
{
    public Mobile Mobile { get; }
    public string Speech { get; set; }     // Can modify speech text
    public MessageType Type { get; }
    public int Hue { get; }
    public int[] Keywords { get; }
    public bool Handled { get; set; }      // Set true to consume
    public bool Blocked { get; set; }      // Set true to block
    public bool HasKeyword(int keyword);
}
```

#### AccountLoginEventArgs
```csharp
public class AccountLoginEventArgs
{
    public NetState State { get; }
    public string Username { get; }
    public string Password { get; }
    public bool Accepted { get; set; }           // Set false to reject
    public ALRReason RejectReason { get; set; }  // Reason for rejection
}
```

#### MovementEventArgs (Pooled)
```csharp
public class MovementEventArgs
{
    public Mobile Mobile { get; }
    public Direction Direction { get; }
    public bool Blocked { get; set; }  // Set true to block movement

    // Object pooling
    public static MovementEventArgs Create(Mobile m, Direction dir);
    public void Free();  // Return to pool
}
```

#### AggressiveActionEventArgs (Pooled)
```csharp
public class AggressiveActionEventArgs
{
    public Mobile Aggressed { get; }
    public Mobile Aggressor { get; }
    public bool Criminal { get; }

    public static AggressiveActionEventArgs Create(Mobile aggressed, Mobile aggressor, bool criminal);
    public void Free();
}
```

#### WorldSavePostSnapshotEventArgs
```csharp
public class WorldSavePostSnapshotEventArgs
{
    public string OldSavePath { get; }
    public string NewSavePath { get; }
}
```

#### ServerCrashedEventArgs
```csharp
public class ServerCrashedEventArgs
{
    public Exception Exception { get; }
    public bool Close { get; set; }  // Set false to continue running
}
```

#### SocketConnectEventArgs
```csharp
public class SocketConnectEventArgs
{
    public IPAddress Address { get; }
    public bool AllowConnection { get; set; }  // Set false to reject
}
```

### Creating Custom EventSink Events

Add to EventSink as a partial class:

```csharp
// Projects/Server/Events/MyCustomEvent.cs
namespace Server;

public static partial class EventSink
{
    public static event Action<Mobile, Item> ItemCrafted;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeItemCrafted(Mobile crafter, Item item) =>
        ItemCrafted?.Invoke(crafter, item);
}
```

Then invoke from game code:
```csharp
EventSink.InvokeItemCrafted(crafter, craftedItem);
```

---

## CodeGeneratedEvents

For events on specific game entities, ModernUO uses source-generated events via the `CodeGeneratedEvents` package.

External reference: https://github.com/modernuo/CodeGeneratedEvents

### Defining Generated Events

On the class that fires the event:
```csharp
[GeneratedEvent(nameof(PlayerLoginEvent))]
public static partial void PlayerLoginEvent(PlayerMobile player);
```

### Subscribing to Generated Events

On any class that handles the event:
```csharp
[OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
public static void HandlePlayerLogin(PlayerMobile player)
{
    // Handle the event
}
```

### Known Generated Events
- `PlayerMobile.PlayerLoginEvent` -- Player logs in
- `PlayerMobile.PlayerDeathEvent` -- Player dies
- `BaseCreature.CreatureDeathEvent` -- Creature dies

---

## Event Args Pooling Pattern

Some EventArgs use object pooling to avoid allocation in hot paths:

```csharp
// System that fires the event:
var args = MovementEventArgs.Create(mobile, direction);
EventSink.InvokeMovement(args);
// Check args.Blocked after invocation
args.Free();  // Return to pool
```

This pattern is used for high-frequency events (movement, combat) to minimize GC pressure.

---

## Best Practices

1. **Subscribe in `Configure()`** -- called automatically during startup
2. **Check player type** -- `Connected` fires for all mobiles; cast to `PlayerMobile` if needed
3. **Keep handlers fast** -- they run on the game loop thread
4. **Use `Handled`/`Blocked`** -- on SpeechEventArgs to consume/block messages
5. **Unsubscribe on disable** -- if your system can be turned off, unsubscribe (`-=`) to prevent leaks
6. **Don't throw exceptions** -- unhandled exceptions in event handlers can crash the server

## Key File References

| File | Description |
|---|---|
| `Projects/Server/Events/EventSink.cs` | Core EventSink (partial) |
| `Projects/Server/Events/SpeechEvent.cs` | Speech event |
| `Projects/Server/Events/MovementEvent.cs` | Movement event (pooled) |
| `Projects/Server/Events/AggressiveActionEvent.cs` | Combat event (pooled) |
| `Projects/Server/Events/AccountLoginEvent.cs` | Account login |
| `Projects/Server/Events/EventSink.cs` | World save/load (WorldLoad, WorldSave, ServerStarted, Shutdown) |
| `Projects/Server/Events/SocketConnectionEvent.cs` | Socket connections |
| `Projects/Server/Events/ServerCrashedEvent.cs` | Crash handling |
