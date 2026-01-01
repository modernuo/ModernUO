# Packet Documentation Conventions

## String Types

When documenting packet fields, use these specific type names to indicate the encoding and format:

### By Encoding

| Type | Encoding | Description |
|------|----------|-------------|
| `ascii` | ASCII | 7-bit ASCII characters |
| `unicode-le` | Unicode (Little Endian) | UTF-16LE, each character is 2 bytes |
| `unicode-be` | Unicode (Big Endian) | UTF-16BE, each character is 2 bytes |
| `utf8` | UTF-8 | Variable-width encoding (rarely used, mainly book packets) |

### By Format

| Suffix | Format | Example |
|--------|--------|---------|
| (no suffix) | Fixed length, padded/truncated | `ascii` with `size: 30` = exactly 30 bytes |
| `-null` | Null-terminated | `ascii-null` = string followed by 0x00 |
| `-prefixed` | Length-prefixed | `unicode-le-prefixed` = ushort length + characters |

### Common Combinations

| Full Type | Description | SpanWriter Method |
|-----------|-------------|-------------------|
| `ascii` | Fixed-length ASCII | `WriteAscii(text, maxLength)` |
| `ascii-null` | Null-terminated ASCII | `WriteAsciiNull(text)` |
| `unicode-le` | Fixed-length Unicode LE | `WriteLittleUni(text, maxLength)` |
| `unicode-le-null` | Null-terminated Unicode LE | `WriteLittleUniNull(text)` |
| `unicode-be` | Fixed-length Unicode BE | `WriteBigUni(text, maxLength)` |
| `unicode-be-null` | Null-terminated Unicode BE | `WriteBigUniNull(text)` |
| `utf8-null` | Null-terminated UTF-8 | `WriteUtf8Null(text)` |

### Size Calculations

- **Fixed ASCII**: Size in bytes = specified size
- **Fixed Unicode**: Size in bytes = specified size (characters) * 2
- **Null-terminated**: Size = string length + 1 (ASCII) or string length * 2 + 2 (Unicode)
- **Length-prefixed**: Size = 2 (ushort length) + string data

### Examples in JSON

```json
// Fixed-length ASCII (30 bytes exactly)
{ "name": "characterName", "type": "ascii", "size": 30, "description": "Character name" }

// Null-terminated ASCII (variable length)
{ "name": "message", "type": "ascii-null", "description": "Chat message" }

// Fixed-length Unicode Little Endian
{ "name": "text", "type": "unicode-le", "size": 64, "description": "Unicode text (64 chars)" }

// Null-terminated Unicode Big Endian
{ "name": "affix", "type": "unicode-be-null", "description": "Affix string" }
```

## Numeric Types

| Type | Size | Range | Signed |
|------|------|-------|--------|
| `byte` | 1 | 0-255 | No |
| `sbyte` | 1 | -128 to 127 | Yes |
| `short` | 2 | -32768 to 32767 | Yes |
| `ushort` | 2 | 0-65535 | No |
| `int` | 4 | -2^31 to 2^31-1 | Yes |
| `uint` | 4 | 0 to 2^32-1 | No |
| `long` | 8 | -2^63 to 2^63-1 | Yes |
| `ulong` | 8 | 0 to 2^64-1 | No |
| `bool` | 1 | 0 (false) or 1 (true) | N/A |

## Byte Order

- All numeric types are **Big Endian** (network byte order) unless noted otherwise
- IP addresses use Little Endian (marked with `LE` suffix)
- Some client-specific fields may use Little Endian (always documented)

## Packet Flags

Common flag values used in mobile/item packets:

| Flag | Value | Description |
|------|-------|-------------|
| Hidden | 0x80 | Entity is hidden |
| Poisoned | 0x04 | Mobile is poisoned |
| Blessed | 0x08 | Mobile is blessed/invulnerable |
| Warmode | 0x40 | Mobile is in war mode |
| Female | 0x02 | Mobile is female |

## Direction Values

| Value | Direction |
|-------|-----------|
| 0 | North |
| 1 | Right (NE) |
| 2 | East |
| 3 | Down (SE) |
| 4 | South |
| 5 | Left (SW) |
| 6 | West |
| 7 | Up (NW) |
| 0x80 | Running flag (OR with direction) |

## Notoriety Values

| Value | Name | Color |
|-------|------|-------|
| 0 | Invalid | Gray |
| 1 | Innocent | Blue |
| 2 | Ally/Friend | Green |
| 3 | Gray/Attackable | Gray |
| 4 | Criminal | Gray |
| 5 | Enemy | Orange |
| 6 | Murderer | Red |
| 7 | Invulnerable | Yellow |
