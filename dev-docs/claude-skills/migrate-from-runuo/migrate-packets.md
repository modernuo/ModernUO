---
name: migrate-packets
description: >
  Trigger: when converting RunUO Packet subclasses, PacketWriter/PacketReader, or packet handler registration to ModernUO span-based packets.
  Covers: outgoing packet conversion, incoming handler conversion, SpanWriter/SpanReader.
---

# RunUO -> ModernUO Packet Migration

## When This Activates
- Converting `Packet` subclasses to static create methods
- Converting `PacketHandlers.Register()` to `IncomingPackets.Register()`
- Converting `PacketWriter`/`PacketReader` to `SpanWriter`/`SpanReader`

## Conversion Steps (Outgoing)
1. Create static class: `public static class OutgoingMyPackets`
2. Define length constant
3. Convert constructor to `static void CreateXxx(Span<byte> buffer, ...)`
4. Replace `m_Stream.Write(x)` with `writer.Write(x)`
5. Create `SendXxx(this NetState ns, ...)` extension method
6. Check `ns.CannotSendPackets()` at start
7. Use `stackalloc byte[length].InitializePacket()` for buffer
8. Replace `ns.Send(new Packet(...))` with `ns.SendXxx(...)`

## Conversion Steps (Incoming)
1. Change `PacketHandlers.Register(id, len, ingame, new OnPacketReceive(H))` to `IncomingPackets.Register(id, len, ingame, &H)` in `unsafe Configure()`
2. Change handler: `void H(NetState, PacketReader)` -> `void H(NetState, SpanReader)`
3. Replace read calls: `pvSrc.ReadString()` -> `reader.ReadAsciiSafe()`

## Quick Mapping
| RunUO | ModernUO |
|---|---|
| `class X : Packet` | Static `CreateX(Span<byte>)` method |
| `m_Stream.Write(val)` | `writer.Write(val)` (SpanWriter) |
| `PacketWriter` | `SpanWriter` |
| `PacketReader` / `pvSrc` | `SpanReader` / `reader` |
| `ns.Send(new X(...))` | `ns.SendX(...)` extension |
| `PacketHandlers.Register(...)` | `IncomingPackets.Register(... &handler)` |
| `new OnPacketReceive(H)` | `&H` (function pointer) |

## See Also
- `dev-docs/runuo-migration-docs/05-packets-networking.md` -- detailed migration reference
- `dev-docs/networking-packets.md` -- complete ModernUO networking system
- `dev-docs/claude-skills/modernuo-networking.md` -- ModernUO networking skill
