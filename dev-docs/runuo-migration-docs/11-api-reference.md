# API Reference — RunUO to ModernUO Mapping

## Quick Lookup

Alphabetical by RunUO API name. Use Ctrl+F / Cmd+F to search.

## Attributes

| RunUO | ModernUO | Notes |
|---|---|---|
| `[Constructable]` | `[Constructible]` | Spelling change |
| `[CommandProperty(AccessLevel)]` | `[SerializedCommandProperty(AccessLevel)]` | On `[SerializableField]` fields |
| `[CommandProperty(AccessLevel)]` | `[CommandProperty(AccessLevel)]` | On `[SerializableProperty]` properties (unchanged) |
| `[CorpseName("name")]` | `public override string CorpseName => "name";` | Attribute → property override |
| `[Constructable]` | `[Constructible]` | Must use `using ModernUO.Serialization;` |
| `[Usage("...")]` | `[Usage("...")]` | Unchanged |
| `[Description("...")]` | `[Description("...")]` | Unchanged |
| N/A | `[Aliases("a1", "a2")]` | New in ModernUO |
| N/A | `[SerializationGenerator(ver, enc)]` | New — replaces manual Serialize/Deserialize |
| N/A | `[SerializableField(idx)]` | New — auto-generates property + serialization |
| N/A | `[SerializableProperty(idx)]` | New — custom property serialization |
| N/A | `[InvalidateProperties]` | New — auto-calls InvalidateProperties on set |
| N/A | `[AfterDeserialization]` | New — post-deserialize hook |
| N/A | `[DeltaDateTime]` | New — relative DateTime storage |
| N/A | `[EncodedInt]` | New — variable-length int encoding |
| N/A | `[InternString]` | New — string interning on load |
| N/A | `[Tidy]` | New — auto-clean null entries in collections |
| N/A | `[CanBeNull]` | New — nullable reference field |
| N/A | `[TypeAlias("old.name")]` | New — backward-compatible type mapping |

## Classes

| RunUO | ModernUO | Notes |
|---|---|---|
| `BinaryFileWriter` | `IGenericWriter` (via GenericPersistence) | No manual file writing |
| `BinaryFileReader` | `IGenericReader` (via GenericPersistence) | No manual file reading |
| `GenericWriter` | `IGenericWriter` | Interface now |
| `GenericReader` | `IGenericReader` | Interface now |
| `Gump` | `DynamicGump` or `StaticGump<T>` | See 04-gumps.md |
| `ObjectPropertyList` (parameter) | `IPropertyList` | Interface for GetProperties |
| `Packet` (base class) | Removed — use static methods | See 05-packets.md |
| `PacketWriter` | `SpanWriter` | Ref struct |
| `PacketReader` | `SpanReader` | Ref struct |
| `Timer` (subclass pattern) | `Timer.StartTimer()` + `TimerExecutionToken` | See 03-timers.md |
| `TextRelay` | Removed — use `info.GetTextEntry(id)` | Returns string directly |
| `RelayInfo` | `RelayInfo` (passed with `in`) | `in RelayInfo` in OnResponse |

## Constructors

| RunUO | ModernUO | Notes |
|---|---|---|
| `MyItem(Serial serial) : base(serial)` | DELETE | Auto-generated |
| `BaseCreature(AI, Fight, 10, 1, 0.2, 0.4)` | `BaseCreature(AI, Fight)` | Extra params have defaults |

## Events (EventSink)

| RunUO | ModernUO | Signature |
|---|---|---|
| `EventSink.Login` | `EventSink.Connected` | `LoginEventArgs` → `Action<Mobile>` |
| `EventSink.Logout` | `EventSink.Disconnected` | `LogoutEventArgs` → `Action<Mobile>` |
| `EventSink.WorldSave` | `EventSink.WorldSave` | `WorldSaveEventArgs` → `Action` |
| `EventSink.WorldLoad` | `EventSink.WorldLoad` | `Action` |
| `EventSink.ServerStarted` | `EventSink.ServerStarted` | `Action` |
| `EventSink.Crashed` | `EventSink.ServerCrashed` | Renamed |
| `EventSink.Speech` | `EventSink.Speech` | `Action<SpeechEventArgs>` |
| `EventSink.Movement` | `EventSink.Movement` | `Action<MovementEventArgs>` |
| `EventSink.AggressiveAction` | `EventSink.AggressiveAction` | `Action<AggressiveActionEventArgs>` |
| `EventSink.AccountLogin` | `EventSink.AccountLogin` | `Action<AccountLoginEventArgs>` |
| `EventSink.SocketConnect` | `EventSink.SocketConnect` | `Action<SocketConnectEventArgs>` |
| `EventSink.Shutdown` | `EventSink.Shutdown` | `Action` |
| `EventSink.BeforeWorldSave` | Removed | Use `WorldSave` |
| `EventSink.PlayerDeath` | `PlayerMobile.PlayerDeathEvent` | CodeGeneratedEvent |
| `EventSink.CreatureDeath` | `BaseCreature.CreatureDeathEvent` | CodeGeneratedEvent |

## Event Delegates

| RunUO | ModernUO |
|---|---|
| `new WorldSaveEventHandler(Save)` | `Save` (direct method reference) |
| `new WorldLoadEventHandler(Load)` | `Load` |
| `new LoginEventHandler(OnLogin)` | `OnConnected` |
| `new LogoutEventHandler(OnLogout)` | `OnDisconnected` |
| `new SpeechEventHandler(OnSpeech)` | `OnSpeech` |
| `new TimerCallback(Method)` | `Method` |
| `new TimerStateCallback(Method)` | `Method` (typed overloads) |
| `new OnPacketReceive(Handler)` | `&Handler` (function pointer) |

## Gump Methods

| RunUO | ModernUO | Notes |
|---|---|---|
| `AddPage(0)` | `builder.AddPage()` | 0 is default |
| `AddBackground(...)` | `builder.AddBackground(...)` | On builder |
| `AddAlphaRegion(...)` | `builder.AddAlphaRegion(...)` | On builder |
| `AddLabel(x, y, hue, text)` | `builder.AddLabel(x, y, hue, text)` | On builder |
| `AddHtml(x, y, w, h, text, bg, scroll)` | `builder.AddHtml(x, y, w, h, text, background: bg, scrollbar: scroll)` | Named params |
| `AddHtmlLocalized(...)` | `builder.AddHtmlLocalized(...)` | On builder |
| `AddButton(x, y, n, p, id, Reply, 0)` | `builder.AddButton(x, y, n, p, id)` | Reply is default |
| `AddTextEntry(...)` | `builder.AddTextEntry(...)` | On builder |
| `AddCheck(x, y, off, on, state, id)` | `builder.AddCheckbox(x, y, off, on, state, id)` | Renamed |
| `Closable = false` | `builder.SetNoClose()` | Property → method |
| `Dragable = false` | `builder.SetNoMove()` | Renamed |
| `Resizable = false` | `builder.SetNoResize()` | Property → method |
| `Disposable = false` | `builder.SetNoDispose()` | Property → method |
| `from.SendGump(new G(...))` | `G.DisplayTo(from, ...)` | Static entry point |
| `from.CloseGump(typeof(G))` | `from.CloseGump<G>()` | Generic |
| `from.HasGump(typeof(G))` | `from.HasGump<G>()` | Generic |

## Gump Response

| RunUO | ModernUO |
|---|---|
| `OnResponse(NetState, RelayInfo)` | `OnResponse(NetState, in RelayInfo)` |
| `info.TextEntries[i].Text` | `info.GetTextEntry(id)` |
| `info.IsSwitched(id)` | `info.IsSwitched(id)` (same) |
| `info.ButtonID` | `info.ButtonID` (same) |

## Methods

| RunUO | ModernUO | Notes |
|---|---|---|
| `DateTime.UtcNow` | `Core.Now` | Server time source |
| `Console.WriteLine(...)` | `logger.Information(...)` | Structured logging |
| `World.FindMobile(serial)` | `reader.ReadEntity<Mobile>()` | In deserialization |
| `World.FindItem(serial)` | `reader.ReadEntity<Item>()` | In deserialization |
| `reader.ReadMobile()` | `reader.ReadEntity<Mobile>()` | Generic method |
| `reader.ReadItem()` | `reader.ReadEntity<Item>()` | Generic method |
| `reader.ReadInt()` | `reader.ReadInt()` | Same |
| `writer.Write((int)value)` | `writer.Write(value)` | No cast needed |
| `writer.WriteEncodedInt(value)` | `writer.WriteEncodedInt(value)` | Same |
| `InvalidateProperties()` | `InvalidateProperties()` | Same, or use `[InvalidateProperties]` |
| `this.MarkDirty()` | `this.MarkDirty()` | NEW — required in custom setters |

## Networking

| RunUO | ModernUO | Notes |
|---|---|---|
| `ns.Send(new MyPacket(...))` | `ns.SendMyPacket(...)` | Extension method |
| `Packet.Compile()` | Not needed | Stack-allocated |
| `Packet.SetStatic()` | Not needed | Reuse via buffer[0] != 0 guard |
| `PacketHandlers.Register(...)` | `IncomingPackets.Register(...)` | Function pointers |
| `m_Stream.Write(value)` | `writer.Write(value)` | SpanWriter |
| `pvSrc.ReadInt32()` | `reader.ReadInt32()` | SpanReader |
| `pvSrc.ReadString()` | `reader.ReadAsciiSafe()` | Explicit encoding |
| `pvSrc.ReadUnicodeStringSafe()` | `reader.ReadBigUniSafe()` | Explicit name |

## Persistence

| RunUO | ModernUO | Notes |
|---|---|---|
| `EventSink.WorldSave += Save` | `class X : GenericPersistence` | Subclass |
| `EventSink.WorldLoad += Load` | `override Deserialize(IGenericReader)` | Method |
| `new BinaryFileWriter(path, true)` | Handled by framework | Automatic |
| `BinaryFileReader` / `FileStream` | Handled by framework | Automatic |
| `Directory.CreateDirectory(...)` | Handled by framework | Automatic |

## Properties

| RunUO | ModernUO | Notes |
|---|---|---|
| `Name = "text"` (in constructor) | `public override string DefaultName => "text";` | Property override |
| `Name` (custom text) | `DefaultName` (property override) | For fixed names |
| `int Prop { get { return m_X; } }` | `int Prop => _x;` | Expression-bodied |
| `int Prop { get { return m_X; } set { m_X = value; } }` | Auto-generated by `[SerializableField]` | Delete manual property |

## Timer

| RunUO | ModernUO | Notes |
|---|---|---|
| `new InternalTimer().Start()` | `Timer.StartTimer(..., out token)` | Fire-and-forget |
| `Timer.DelayCall(delay, callback)` | `Timer.StartTimer(delay, callback)` | Similar |
| `Timer.DelayCall(delay, stateCallback, state)` | `Timer.DelayCall(delay, callback, state)` | Typed state |
| `timer.Stop()` | `token.Cancel()` | Struct, safe to call multiple times |
| `timer.Running` | `token.Running` | Same concept |
| `TimerPriority.XXX` | Removed | Timer wheel auto-schedules |
| Timer in `Deserialize()` | `[AfterDeserialization]` | Post-load hook |
| `timer != null` check | `token.Running` check | Value type, always valid |

## Threading / Performance

| RunUO | ModernUO | Notes |
|---|---|---|
| `lock (_obj)` | Remove | Single-threaded |
| `volatile` | Remove keyword | Single-threaded |
| `ConcurrentDictionary` | `Dictionary` | Single-threaded |
| `Task.Run(...)` | Use `Timer.StartTimer()` | Game loop only |
| `new Thread(...)` | Forbidden | Game loop only |
| `ArrayPool<T>.Shared` | `STArrayPool<T>.Shared` | No-lock pool |
| `new List<T>()` (hot path) | `PooledRefList<T>.Create()` | Zero-alloc |
| `World.Mobiles.Values` iteration | `map.GetMobilesInRange<T>()` | Spatial query |
| `World.Items.Values` iteration | `map.GetItemsInRange<T>()` | Spatial query |

## Usings

| RunUO | ModernUO | Notes |
|---|---|---|
| (implicit) | `using ModernUO.Serialization;` | For serialization attributes |
| (implicit) | `using Server.Logging;` | For logging |
| (implicit) | `using Server.Gumps;` | For gump extension methods |
| (implicit) | `using Server.Collections;` | For PooledRefList |
