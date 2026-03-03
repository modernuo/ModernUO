# ModernUO Networking & Packets

This document covers ModernUO's networking system, including outgoing and incoming packet patterns, SpanWriter/SpanReader, and NetState extensions.

## Overview

ModernUO uses a binary packet protocol for client-server communication. The system is built around:
- **Outgoing packets**: Static `Create*` methods + `Send*` extension methods on `NetState`
- **Incoming packets**: Function pointer handlers registered in `Configure()`
- **SpanWriter/SpanReader**: High-performance binary I/O using `Span<byte>`

## Outgoing Packet Pattern

### Step 1: Define Constants and Create Method

```csharp
public static class OutgoingMyPackets
{
    public const int MyPacketLength = 12;  // Fixed-size packet

    public static void CreateMyPacket(Span<byte> buffer, Serial target, int value)
    {
        if (buffer[0] != 0)  // Already initialized guard
            return;

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xBF);       // Packet ID
        writer.Write((ushort)12);       // Packet length
        writer.Write((ushort)0x99);     // Sub-command
        writer.Write(target);           // Serial (4 bytes)
        writer.Write((short)value);     // Value (2 bytes)
    }
}
```

### Step 2: Define Send Extension Method

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

### Step 3: Call from Game Code

```csharp
// Send to one player
mobile.NetState.SendMyPacket(target.Serial, 42);

// Send to nearby players
foreach (var ns in mobile.GetClientsInRange(18))
{
    ns.SendMyPacket(target.Serial, 42);
}
```

### Variable-Length Outgoing Packets

```csharp
public static void SendMyDynamicPacket(this NetState ns, string name, int[] values)
{
    if (ns.CannotSendPackets())
        return;

    var length = 7 + name.Length * 2 + values.Length * 4;
    var writer = new SpanWriter(stackalloc byte[length]);

    writer.Write((byte)0x99);          // Packet ID
    writer.Write((ushort)0);           // Length placeholder
    writer.WriteBigUniNull(name);      // Unicode string
    writer.Write((ushort)values.Length);

    foreach (var val in values)
        writer.Write(val);

    writer.WritePacketLength();        // Fill in actual length at position 1-2
    ns.Send(writer.Span);
}
```

### Shared Buffer Pattern (Multiple Recipients)

When sending the same packet to multiple players, create the buffer once:

```csharp
public static void SendToNearby(Mobile source, int effectId)
{
    Span<byte> buffer = stackalloc byte[EffectPacketLength];
    buffer.InitializePacket();

    foreach (var ns in source.GetClientsInRange(18))
    {
        // CreateXxx checks buffer[0] != 0 to avoid re-initializing
        CreateEffectPacket(buffer, source.Serial, effectId);
        ns.Send(buffer);
    }
}
```

---

## Incoming Packet Pattern

### Step 1: Register Handler in Configure()

```csharp
public static class IncomingMyPackets
{
    public static unsafe void Configure()
    {
        // Fixed-length packet (12 bytes, in-game only)
        IncomingPackets.Register(0x99, 12, true, &MyHandler);

        // Variable-length packet (0 = variable)
        IncomingPackets.Register(0x9A, 0, true, &MyDynamicHandler);

        // Out-of-game packet
        IncomingPackets.Register(0x9B, 10, false, &LoginHandler);

        // Encoded packet (sub-command)
        IncomingPackets.RegisterEncoded(0x28, true, &EncodedHandler);
    }
}
```

### Step 2: Implement Handler

```csharp
public static void MyHandler(NetState state, SpanReader reader)
{
    var from = state.Mobile;
    if (from == null)
        return;

    var targetSerial = (Serial)reader.ReadUInt32();
    var value = reader.ReadInt16();

    var target = World.FindMobile(targetSerial);
    if (target == null)
        return;

    // Process packet...
}

public static void MyDynamicHandler(NetState state, SpanReader reader)
{
    var from = state.Mobile;
    if (from == null)
        return;

    var name = reader.ReadBigUniSafe();
    var count = reader.ReadUInt16();

    for (var i = 0; i < count; i++)
    {
        var val = reader.ReadInt32();
        // Process each value...
    }
}
```

### Encoded Packet Handler

```csharp
public static void EncodedHandler(NetState state, IEntity target, EncodedReader reader)
{
    // Encoded packets have a different signature
    var from = state.Mobile;
    if (from == null)
        return;

    // Process...
}
```

### Registration Parameters

```csharp
IncomingPackets.Register(
    int packetID,       // Packet identifier (0x00-0xFF)
    int length,         // Fixed length, or 0 for variable-length
    bool inGameOnly,    // Requires authenticated player
    delegate*<NetState, SpanReader, void> handler  // Function pointer
);
```

---

## SpanWriter Reference

High-performance ref struct for writing binary data. Defined in `Projects/Server/Buffers/SpanWriter.cs`.

### Constructors

```csharp
var writer = new SpanWriter(Span<byte> buffer);              // Fixed buffer
var writer = new SpanWriter(stackalloc byte[64]);             // Stack buffer
var writer = new SpanWriter(int capacity, bool resize = false); // Pooled buffer
```

### Integer Writes (Big-Endian by Default)

```csharp
writer.Write(bool value);        // 1 byte
writer.Write(byte value);        // 1 byte
writer.Write(sbyte value);       // 1 byte
writer.Write(short value);       // 2 bytes, big-endian
writer.Write(ushort value);      // 2 bytes, big-endian
writer.Write(int value);         // 4 bytes, big-endian
writer.Write(uint value);        // 4 bytes, big-endian
writer.Write(long value);        // 8 bytes, big-endian
writer.Write(ulong value);       // 8 bytes, big-endian
writer.Write(Serial serial);     // 4 bytes (writes serial.Value)
```

### Little-Endian Variants

```csharp
writer.WriteLE(short value);
writer.WriteLE(ushort value);
writer.WriteLE(int value);
writer.WriteLE(uint value);
```

### String Writes

```csharp
// ASCII (1 byte per char)
writer.WriteAscii(string value);
writer.WriteAsciiNull(string value);       // Null-terminated
writer.WriteAscii(string value, int fixedLength);

// Latin-1 (1 byte per char, extended ASCII)
writer.WriteLatin1(string value);
writer.WriteLatin1Null(string value);
writer.WriteLatin1(string value, int fixedLength);

// UTF-16 Big-Endian (UO standard for Unicode)
writer.WriteBigUni(string value);
writer.WriteBigUniNull(string value);
writer.WriteBigUni(string value, int fixedLength);

// UTF-16 Little-Endian
writer.WriteLittleUni(string value);
writer.WriteLittleUniNull(string value);
writer.WriteLittleUni(string value, int fixedLength);

// UTF-8
writer.WriteUTF8(string value);
writer.WriteUTF8Null(string value);
```

### Utilities

```csharp
writer.Write(ReadOnlySpan<byte> data);    // Raw bytes
writer.Clear(int count);                   // Write zeros
writer.Seek(int offset, SeekOrigin origin); // Move position
writer.WritePacketLength();                // Fill length at position 1-2
writer.EnsureCapacity(int capacity);       // Grow buffer if needed
writer.Dispose();                          // Return pooled buffer

// Properties
writer.Position;     // Current write position
writer.Capacity;     // Buffer size
writer.Span;         // ReadOnlySpan<byte> of written data
writer.RawBuffer;    // Mutable Span<byte> of full buffer
```

---

## SpanReader Reference

High-performance ref struct for reading binary data. Defined in `Projects/Server/Buffers/SpanReader.cs`.

### Constructor

```csharp
var reader = new SpanReader(ReadOnlySpan<byte> data);
```

### Integer Reads (Big-Endian by Default)

```csharp
reader.ReadByte();        // 1 byte
reader.ReadBoolean();     // 1 byte (> 0 = true)
reader.ReadSByte();       // 1 byte signed
reader.ReadInt16();       // 2 bytes, big-endian
reader.ReadUInt16();      // 2 bytes, big-endian
reader.ReadInt32();       // 4 bytes, big-endian
reader.ReadUInt32();      // 4 bytes, big-endian
reader.ReadInt64();       // 8 bytes, big-endian
reader.ReadUInt64();      // 8 bytes, big-endian
```

### Little-Endian Variants

```csharp
reader.ReadInt16LE();
reader.ReadUInt16LE();
reader.ReadUInt32LE();
```

### String Reads

```csharp
// Each has a "Safe" variant that filters control characters
reader.ReadAscii(int fixedLength = -1);
reader.ReadAsciiSafe(int fixedLength = -1);
reader.ReadLatin1(int fixedLength = -1);
reader.ReadLatin1Safe(int fixedLength = -1);
reader.ReadBigUni(int fixedLength = -1);
reader.ReadBigUniSafe(int fixedLength = -1);
reader.ReadLittleUni(int fixedLength = -1);
reader.ReadLittleUniSafe(int fixedLength = -1);
reader.ReadUTF8(int fixedLength = -1);
reader.ReadUTF8Safe(int fixedLength = -1);
```

### Utilities

```csharp
reader.Seek(int offset, SeekOrigin origin);
reader.Read(Span<byte> destination);

// Properties
reader.Position;    // Current read position
reader.Length;      // Total data length
reader.Remaining;   // Bytes remaining
reader.Buffer;      // ReadOnlySpan<byte> of full data
```

---

## Common Existing Send Methods

### Effects and Sounds
```csharp
ns.SendSoundEffect(int soundID, IPoint3D target);
ns.SendMobileAnimation(Serial mobile, int action, int frames, int repeat, bool forward, bool loop, int delay);
ns.SendNewMobileAnimation(Serial mobile, int action, int frames, int delay);
```

### Mobile Status
```csharp
ns.SendMobileHits(Mobile m, bool normalize = false);
ns.SendMobileMana(Mobile m, bool normalize = false);
ns.SendMobileStam(Mobile m, bool normalize = false);
ns.SendMobileAttributes(Mobile m, bool normalize = false);
ns.SendMobileStatus(Mobile m);
ns.SendMobileName(Mobile m);
ns.SendMobileMoving(Mobile source, Mobile target);
ns.SendBondedStatus(Serial serial, bool bonded);
ns.SendDeathAnimation(Serial killed, Serial corpse);
```

### Damage
```csharp
ns.SendDamage(Serial serial, int amount);
```

### Targeting
```csharp
ns.SendTargetReq(Target target);
ns.SendMovementRej(int sequence, Mobile m);
```

---

## Protocol Notes

- **Endianness**: UO protocol is big-endian by default
- **Packet ID**: First byte identifies the packet type (0x00-0xFF)
- **Length**: For variable-length packets, bytes 1-2 are the total length (big-endian ushort)
- **Serials**: 4-byte identifiers for items (0x40000000+) and mobiles (0x00000001+)
- **Clilocs**: 4-byte localized string IDs

## Best Practices

1. **Always check `ns.CannotSendPackets()`** before sending
2. **Use `stackalloc`** for fixed-size packets (avoids heap allocation)
3. **Use `InitializePacket()`** extension on stackalloc spans
4. **Use `ReadAsciiSafe`/`ReadBigUniSafe`** for incoming strings (filters control chars)
5. **Use `WritePacketLength()`** for variable-length packets
6. **Big-endian by default** -- only use `WriteLE`/`ReadLE` when the protocol requires it
7. **Function pointers** (`&Handler`) for incoming packet registration (no delegate allocation)

## Key File References

| File | Description |
|---|---|
| `Projects/Server/Buffers/SpanWriter.cs` | SpanWriter ref struct |
| `Projects/Server/Buffers/SpanReader.cs` | SpanReader ref struct |
| `Projects/Server/Network/Packets/IncomingPackets.cs` | Packet registration |
| `Projects/UOContent/Network/Packets/IncomingPlayerPackets.cs` | Player packet handlers |
| `Projects/UOContent/Network/Packets/IncomingMovementPackets.cs` | Movement handlers |
| `Projects/UOContent/Network/Packets/IncomingMessagePackets.cs` | Speech handlers |
| `Projects/UOContent/Network/Packets/IncomingItemPackets.cs` | Item handlers |
| `Projects/UOContent/Network/Packets/IncomingTargetingPackets.cs` | Targeting handlers |
| `Projects/Server/Network/Packets/OutgoingMobilePackets.cs` | Mobile packets |
| `Projects/Server/Network/Packets/OutgoingItemPackets.cs` | Item packets |
| `Projects/Server/Network/Packets/OutgoingDamagePackets.cs` | Damage packets |
| `Projects/Server/Network/Packets/OutgoingEffectPackets.cs` | Effect/sound packets |
| `Projects/Server/Network/Packets/OutgoingAccountPackets.cs` | Account packets |
| `Projects/Server/Network/Packets/OutgoingContainerPackets.cs` | Container packets |
| `Projects/Server/Network/PacketHandler.cs` | PacketHandler class |
