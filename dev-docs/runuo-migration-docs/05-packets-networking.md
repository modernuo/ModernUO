# Packets & Networking Migration

## Overview

RunUO uses a `Packet` class hierarchy where outgoing packets are objects with `PacketWriter`, and incoming packets use `PacketHandler` with `PacketReader`. ModernUO completely removes the `Packet` class hierarchy. Outgoing packets are static methods using `SpanWriter` with stack-allocated buffers. Incoming packets are registered function pointers using `SpanReader`.

## RunUO Outgoing Packet Pattern

```csharp
// RunUO — Packet subclass
public sealed class MyPacket : Packet
{
    public MyPacket(Serial target, int value) : base(0xBF, 12)
    {
        m_Stream.Write((ushort)12);
        m_Stream.Write((ushort)0x99);
        m_Stream.Write((int)target);
        m_Stream.Write((short)value);
    }
}

// Usage:
ns.Send(new MyPacket(target.Serial, 42));
```

## ModernUO Outgoing Packet Pattern

```csharp
// ModernUO — Static create method + extension method
public static class OutgoingMyPackets
{
    public const int MyPacketLength = 12;

    public static void CreateMyPacket(Span<byte> buffer, Serial target, int value)
    {
        if (buffer[0] != 0)
            return;

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xBF);
        writer.Write((ushort)12);
        writer.Write((ushort)0x99);
        writer.Write(target);
        writer.Write((short)value);
    }
}

public static class MyPacketExtensions
{
    public static void SendMyPacket(this NetState ns, Serial target, int value)
    {
        if (ns.CannotSendPackets())
            return;

        var buffer = stackalloc byte[OutgoingMyPackets.MyPacketLength].InitializePacket();
        OutgoingMyPackets.CreateMyPacket(buffer, target, value);
        ns.Send(buffer);
    }
}

// Usage:
mobile.NetState.SendMyPacket(target.Serial, 42);
```

## RunUO Incoming Packet Pattern

```csharp
// RunUO — PacketHandler registration
public class MyPacketHandlers
{
    public static void Initialize()
    {
        PacketHandlers.Register(0x99, 12, true, new OnPacketReceive(MyHandler));
    }

    public static void MyHandler(NetState state, PacketReader pvSrc)
    {
        Serial target = pvSrc.ReadInt32();
        int value = pvSrc.ReadInt16();
        // Process...
    }
}
```

## ModernUO Incoming Packet Pattern

```csharp
// ModernUO — Function pointer registration
public static class IncomingMyPackets
{
    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x99, 12, true, &MyHandler);
    }

    public static void MyHandler(NetState state, SpanReader reader)
    {
        var target = (Serial)reader.ReadUInt32();
        var value = reader.ReadInt16();
        // Process...
    }
}
```

## Migration Mapping Table

| RunUO | ModernUO | Notes |
|---|---|---|
| `class MyPacket : Packet` | Static `CreateXxx(Span<byte>)` method | No class hierarchy |
| `Packet(packetId, length)` constructor | `new SpanWriter(buffer)` | Stack-allocated buffer |
| `m_Stream.Write(value)` | `writer.Write(value)` | Same method names |
| `PacketWriter` | `SpanWriter` | Ref struct, stack-allocated |
| `PacketReader` | `SpanReader` | Ref struct |
| `pvSrc.ReadInt32()` | `reader.ReadInt32()` | Same names |
| `pvSrc.ReadString()` | `reader.ReadAsciiSafe()` or `reader.ReadBigUniSafe()` | Explicit encoding |
| `pvSrc.ReadUnicodeStringSafe()` | `reader.ReadBigUniSafe()` | Explicit name |
| `ns.Send(new MyPacket(...))` | `ns.SendMyPacket(...)` | Extension method |
| `PacketHandlers.Register(id, len, ingame, handler)` | `IncomingPackets.Register(id, len, ingame, &handler)` | Function pointer |
| `new OnPacketReceive(handler)` | `&handler` | Function pointer, no delegate |
| `Packet.Compile()` / `Packet.SetStatic()` | Not needed | Buffer is stack-allocated |
| `Packet.Acquire()` / `Packet.Release()` | Not needed | No pooling, stack memory |
| Variable-length: `this.EnsureCapacity(len)` | `writer.WritePacketLength()` at end | Fill length at position 1-2 |

## Step-by-Step Conversion

### Outgoing Packets

#### Step 1: Create Static Class
```csharp
public static class OutgoingMySystemPackets
{
    // Constants and Create methods go here
}
```

#### Step 2: Convert Packet Constructor to Create Method
```csharp
// Determine packet length from RunUO constructor: base(0xBF, 12)
public const int MyPacketLength = 12;

public static void CreateMyPacket(Span<byte> buffer, Serial target, int value)
{
    if (buffer[0] != 0) // Already initialized guard
        return;

    var writer = new SpanWriter(buffer);
    writer.Write((byte)0xBF);       // Packet ID
    writer.Write((ushort)12);       // Length
    writer.Write((ushort)0x99);     // Sub-command
    writer.Write(target);           // Serial
    writer.Write((short)value);     // Value
}
```

#### Step 3: Create Send Extension Method
```csharp
public static void SendMyPacket(this NetState ns, Serial target, int value)
{
    if (ns.CannotSendPackets())
        return;

    var buffer = stackalloc byte[MyPacketLength].InitializePacket();
    CreateMyPacket(buffer, target, value);
    ns.Send(buffer);
}
```

#### Step 4: For Variable-Length Packets
```csharp
public static void SendDynamicPacket(this NetState ns, string name, int[] values)
{
    if (ns.CannotSendPackets())
        return;

    var length = 7 + name.Length * 2 + values.Length * 4;
    var writer = new SpanWriter(stackalloc byte[length]);

    writer.Write((byte)0x99);
    writer.Write((ushort)0);           // Length placeholder
    writer.WriteBigUniNull(name);
    writer.Write((ushort)values.Length);

    foreach (var val in values)
        writer.Write(val);

    writer.WritePacketLength();        // Fill in actual length
    ns.Send(writer.Span);
}
```

### Incoming Packets

#### Step 1: Change Registration
```csharp
// RunUO (Initialize)
PacketHandlers.Register(0x99, 12, true, new OnPacketReceive(MyHandler));

// ModernUO (Configure, unsafe)
public static unsafe void Configure()
{
    IncomingPackets.Register(0x99, 12, true, &MyHandler);
}
```

#### Step 2: Update Handler Signature
```csharp
// RunUO
public static void MyHandler(NetState state, PacketReader pvSrc)

// ModernUO
public static void MyHandler(NetState state, SpanReader reader)
```

#### Step 3: Update Read Methods
```csharp
// RunUO → ModernUO
pvSrc.ReadInt32()          → reader.ReadInt32()
pvSrc.ReadInt16()          → reader.ReadInt16()
pvSrc.ReadByte()           → reader.ReadByte()
pvSrc.ReadBoolean()        → reader.ReadBoolean()
pvSrc.ReadString()         → reader.ReadAsciiSafe()
pvSrc.ReadUnicodeStringSafe() → reader.ReadBigUniSafe()
pvSrc.ReadUnicodeString()  → reader.ReadBigUni()
pvSrc.Seek(offset, origin) → reader.Seek(offset, origin)
```

## SpanWriter Quick Reference

```csharp
writer.Write(bool);           // 1 byte
writer.Write(byte);           // 1 byte
writer.Write(short);          // 2 bytes big-endian
writer.Write(ushort);         // 2 bytes big-endian
writer.Write(int);            // 4 bytes big-endian
writer.Write(uint);           // 4 bytes big-endian
writer.Write(Serial);         // 4 bytes
writer.WriteAsciiNull(str);   // ASCII null-terminated
writer.WriteBigUniNull(str);  // UTF-16 BE null-terminated
writer.WriteLE(int);          // 4 bytes little-endian
writer.WritePacketLength();   // Fill length at pos 1-2
```

## SpanReader Quick Reference

```csharp
reader.ReadByte();            // 1 byte
reader.ReadBoolean();         // 1 byte
reader.ReadInt16();           // 2 bytes big-endian
reader.ReadUInt16();          // 2 bytes big-endian
reader.ReadInt32();           // 4 bytes big-endian
reader.ReadUInt32();          // 4 bytes big-endian
reader.ReadAsciiSafe();       // ASCII, filtered
reader.ReadBigUniSafe();      // UTF-16 BE, filtered
reader.Seek(offset, origin);  // Position
reader.Remaining;             // Bytes left
```

## Shared Buffer Pattern

When sending the same packet to multiple players:
```csharp
public static void SendToNearby(Mobile source, int effectId)
{
    Span<byte> buffer = stackalloc byte[EffectPacketLength];
    buffer.InitializePacket();

    foreach (var ns in source.GetClientsInRange(18))
    {
        CreateEffectPacket(buffer, source.Serial, effectId);
        ns.Send(buffer);
    }
}
```

The `buffer[0] != 0` guard in `Create` methods prevents re-initialization, so the buffer is built once and reused.

## Edge Cases & Gotchas

### 1. Always Check CannotSendPackets()
```csharp
if (ns.CannotSendPackets())
    return;
```

### 2. Use InitializePacket() on stackalloc
```csharp
var buffer = stackalloc byte[length].InitializePacket();
```

### 3. Big-Endian by Default
UO protocol is big-endian. Only use `WriteLE`/`ReadLE` when specifically needed.

### 4. Use Safe String Reads
Always use `ReadAsciiSafe()`/`ReadBigUniSafe()` for incoming strings to filter control characters.

### 5. Function Pointers Require unsafe
The `Configure()` method must be marked `unsafe` for function pointer syntax `&handler`.

### 6. Many Common Packets Already Exist
Before writing custom packet code, check if ModernUO already has a `Send*` extension method in:
- `OutgoingMobilePackets` — Mobile status, animation, movement
- `OutgoingItemPackets` — Item display, updates
- `OutgoingEffectPackets` — Effects, sounds
- `OutgoingContainerPackets` — Container contents

## See Also

- `dev-docs/networking-packets.md` — Complete ModernUO networking reference
- `01-foundation-changes.md` — Foundation changes
