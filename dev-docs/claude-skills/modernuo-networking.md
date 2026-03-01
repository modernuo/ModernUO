---
name: modernuo-networking
description: >
  Trigger when creating or modifying packets, working with NetState, SpanWriter, SpanReader, or implementing network protocol handlers.
---

# ModernUO Networking & Packets

## When This Activates
- Creating new packets (outgoing or incoming)
- Modifying existing packet handlers
- Working with `NetState`, `SpanWriter`, `SpanReader`
- Implementing network protocol features

## Key Rules

1. **Outgoing packets**: Static `Create*` method fills `Span<byte>`, extension `Send*` method on `NetState`
2. **Incoming packets**: Register with function pointers in `Configure()`
3. **Use `stackalloc`** for small fixed-size outgoing packets
4. **Always check `ns.CannotSendPackets()`** before sending
5. **Big-endian by default** -- use `WriteLE()` for little-endian

## Outgoing Packet Pattern

### Step 1: Create Extension Method on NetState
```csharp
public static class OutgoingMyPackets
{
    public const int MyPacketLength = 12;

    public static void SendMyPacket(this NetState ns, Serial target, int value)
    {
        if (ns.CannotSendPackets())
            return;

        var buffer = stackalloc byte[MyPacketLength].InitializePacket();
        CreateMyPacket(buffer, target, value);
        ns.Send(buffer);
    }

    public static void CreateMyPacket(Span<byte> buffer, Serial target, int value)
    {
        if (buffer[0] != 0)  // Already initialized check
            return;

        var writer = new SpanWriter(buffer);
        writer.Write((byte)0xBF);       // Packet ID
        writer.Write((ushort)12);       // Length
        writer.Write((ushort)0x99);     // Sub-command
        writer.Write(target);           // Serial (4 bytes)
        writer.Write((short)value);     // Value (2 bytes)
    }
}
```

### Step 2: Call from Game Code
```csharp
mobile.NetState.SendMyPacket(target.Serial, 42);

// Or for all nearby players:
foreach (var ns in mobile.GetClientsInRange(18))
{
    ns.SendMyPacket(target.Serial, 42);
}
```

### Variable-Length Outgoing Packets
```csharp
public static void SendMyDynamicPacket(this NetState ns, string name)
{
    if (ns.CannotSendPackets())
        return;

    var writer = new SpanWriter(stackalloc byte[64]);  // Or use pooled buffer for large packets
    writer.Write((byte)0x99);          // Packet ID
    writer.Write((ushort)0);           // Placeholder for length
    writer.WriteBigUniNull(name);      // Unicode string with null terminator
    writer.WritePacketLength();        // Fill in actual length

    ns.Send(writer.Span);
}
```

## Incoming Packet Pattern

### Step 1: Register Handler in Configure()
```csharp
public static class IncomingMyPackets
{
    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x99, 12, true, &MyPacketHandler);
        //                       ^ID   ^len ^inGameOnly  ^handler
        // len=0 for variable-length packets
    }

    public static void MyPacketHandler(NetState state, SpanReader reader)
    {
        var from = state.Mobile;
        if (from == null)
            return;

        var targetSerial = (Serial)reader.ReadUInt32();
        var value = reader.ReadInt16();

        var target = World.FindMobile(targetSerial);
        if (target != null)
        {
            // Process packet
        }
    }
}
```

### Encoded Packet Registration
```csharp
public static unsafe void Configure()
{
    IncomingPackets.RegisterEncoded(0x28, true, &GuildGumpRequest);
    //                              ^subID  ^inGame  ^handler
}

public static void GuildGumpRequest(NetState state, IEntity target, EncodedReader reader)
{
    // Handle encoded packet
}
```

## SpanWriter Reference

```csharp
// Constructors
var writer = new SpanWriter(Span<byte> buffer);
var writer = new SpanWriter(stackalloc byte[64]);
var writer = new SpanWriter(int capacity, bool resize = false);

// Integer writes (big-endian by default)
writer.Write(bool value);
writer.Write(byte value);
writer.Write(sbyte value);
writer.Write(short value);      // Big-endian
writer.Write(ushort value);     // Big-endian
writer.Write(int value);        // Big-endian
writer.Write(uint value);       // Big-endian
writer.Write(long value);
writer.Write(Serial serial);    // 4 bytes, big-endian

// Little-endian variants
writer.WriteLE(short value);
writer.WriteLE(ushort value);
writer.WriteLE(int value);
writer.WriteLE(uint value);

// String writes
writer.WriteAscii(string value);
writer.WriteAsciiNull(string value);      // Null-terminated
writer.WriteAscii(string value, int fixedLength);
writer.WriteLatin1(string value);
writer.WriteLatin1Null(string value);
writer.WriteBigUni(string value);         // UTF-16 big-endian
writer.WriteBigUniNull(string value);
writer.WriteLittleUni(string value);      // UTF-16 little-endian
writer.WriteLittleUniNull(string value);
writer.WriteUTF8(string value);
writer.WriteUTF8Null(string value);

// Utilities
writer.Write(ReadOnlySpan<byte> data);
writer.Clear(int count);                   // Write zeros
writer.Seek(int offset, SeekOrigin origin);
writer.WritePacketLength();                // Fill in length at position 1-2
writer.EnsureCapacity(int capacity);
writer.Dispose();                          // Return pooled buffer if any

// Properties
writer.Position;    // Current write position
writer.Capacity;    // Buffer size
writer.Span;        // ReadOnlySpan<byte> of written data
```

## SpanReader Reference

```csharp
// Constructor
var reader = new SpanReader(ReadOnlySpan<byte> data);

// Integer reads (big-endian by default)
reader.ReadByte();
reader.ReadBoolean();     // byte > 0
reader.ReadSByte();
reader.ReadInt16();       // Big-endian
reader.ReadUInt16();      // Big-endian
reader.ReadInt32();       // Big-endian
reader.ReadUInt32();      // Big-endian
reader.ReadInt64();
reader.ReadUInt64();

// Little-endian variants
reader.ReadInt16LE();
reader.ReadUInt16LE();
reader.ReadUInt32LE();

// String reads
reader.ReadAscii(int fixedLength = -1);
reader.ReadAsciiSafe(int fixedLength = -1);  // Filters control chars
reader.ReadLatin1(int fixedLength = -1);
reader.ReadLatin1Safe(int fixedLength = -1);
reader.ReadBigUni(int fixedLength = -1);
reader.ReadBigUniSafe(int fixedLength = -1);
reader.ReadLittleUni(int fixedLength = -1);
reader.ReadUTF8(int fixedLength = -1);

// Utilities
reader.Seek(int offset, SeekOrigin origin);
reader.Read(Span<byte> destination);

// Properties
reader.Position;     // Current read position
reader.Length;        // Total data length
reader.Remaining;    // Bytes remaining
```

## Common Packet Patterns

### Sound Effect
```csharp
ns.SendSoundEffect(0x1E5, target);
```

### Mobile Animation
```csharp
ns.SendMobileAnimation(mobile.Serial, action, frameCount, repeatCount, forward, repeat, delay);
```

### Damage
```csharp
ns.SendDamage(target.Serial, amount);
```

## Anti-Patterns

- **Not checking `CannotSendPackets()`**: Player may have disconnected
- **Using `new byte[]` for packets**: Use `stackalloc` for fixed-size, `SpanWriter` for variable
- **Wrong endianness**: UO protocol is big-endian; only use `WriteLE` when spec requires it
- **Not calling `InitializePacket()`**: Required for `Create*` pattern with static buffer reuse

## Real Examples
- Mobile packets: `Projects/Server/Network/Packets/OutgoingMobilePackets.cs`
- Item packets: `Projects/Server/Network/Packets/OutgoingItemPackets.cs`
- Damage packets: `Projects/Server/Network/Packets/OutgoingDamagePackets.cs`
- Effect packets: `Projects/Server/Network/Packets/OutgoingEffectPackets.cs`
- Incoming registration: `Projects/UOContent/Network/Packets/IncomingPlayerPackets.cs`
- Movement handler: `Projects/UOContent/Network/Packets/IncomingMovementPackets.cs`
- SpanWriter: `Projects/Server/Buffers/SpanWriter.cs`
- SpanReader: `Projects/Server/Buffers/SpanReader.cs`

## See Also
- `dev-docs/networking-packets.md` - Complete networking documentation
- `dev-docs/claude-skills/modernuo-threading.md` - Network I/O threading context
