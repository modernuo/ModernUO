using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Network;

namespace Server.Tests.Network.Packets
{
  public static class PacketTestUtilities
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> Compile(this Packet p) =>
      p.Compile(false, out int length).AsSpan(0, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this byte value, Span<byte> bytes) => bytes[0] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this bool value, Span<byte> bytes) => bytes[0] = (byte)(value ? 0x1 : 0x0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this ushort number, Span<byte> bytes) => BinaryPrimitives.WriteUInt16BigEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this short number, Span<byte> bytes) => BinaryPrimitives.WriteInt16BigEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this Serial s, Span<byte> bytes) => BinaryPrimitives.WriteUInt32BigEndian(bytes, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this uint number, Span<byte> bytes) => BinaryPrimitives.WriteUInt32BigEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this int number, Span<byte> bytes) => BinaryPrimitives.WriteInt32BigEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this ulong number, Span<byte> bytes) => BinaryPrimitives.WriteUInt64BigEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this long number, Span<byte> bytes) => BinaryPrimitives.WriteInt64BigEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this ushort number, Span<byte> bytes) => BinaryPrimitives.WriteUInt16LittleEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this short number, Span<byte> bytes) => BinaryPrimitives.WriteInt16LittleEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this Serial s, Span<byte> bytes) => BinaryPrimitives.WriteUInt32LittleEndian(bytes, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this uint number, Span<byte> bytes) => BinaryPrimitives.WriteUInt32LittleEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this int number, Span<byte> bytes) => BinaryPrimitives.WriteInt32LittleEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this ulong number, Span<byte> bytes) => BinaryPrimitives.WriteUInt64LittleEndian(bytes, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyToLE(this long number, Span<byte> bytes) => BinaryPrimitives.WriteInt64LittleEndian(bytes, number);

    // With Position Reference
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this byte value, ref int pos, Span<byte> bytes) => bytes[pos++] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this bool value, ref int pos, Span<byte> bytes) => bytes[pos++] = (byte)(value ? 0x1 : 0x0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this ushort number, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(pos, 2), number);
      pos += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this short number, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteInt16BigEndian(bytes.Slice(pos, 2), number);
      pos += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this Serial s, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt32BigEndian(bytes.Slice(pos, 4), s);
      pos += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this uint number, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt32BigEndian(bytes.Slice(pos, 4), number);
      pos += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this int number, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteInt32BigEndian(bytes.Slice(pos, 4), number);
      pos += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this ulong number, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(pos, 8), number);
      pos += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this long number, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteInt64BigEndian(bytes.Slice(pos, 8), number);
      pos += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyRawASCIITo(this string str, ref int pos, Span<byte> bytes)
    {
      pos += Encoding.ASCII.GetBytes(str.AsSpan(), bytes.Slice(pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyRawASCIITo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      pos += Encoding.ASCII.GetBytes(str.AsSpan(0, Math.Min(str.Length, max)), bytes.Slice(pos));
    }

    // Ascii prepended with two-byte length
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIITo(this string str, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(pos, 2), (ushort)str.Length);
      pos += 2 + Encoding.ASCII.GetBytes(str.AsSpan(), bytes.Slice(pos + 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIITo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      var length = Math.Min(str.Length, max);
      BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(pos, 2), (ushort)length);
      pos += 2 + Encoding.ASCII.GetBytes(str.AsSpan(0, length), bytes.Slice(pos + 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIINullTo(this string str, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(pos, 2), (ushort)(str.Length + 1));
      pos += 3 + Encoding.ASCII.GetBytes(str.AsSpan(), bytes.Slice(pos + 2));
      bytes[pos - 1] = 0; // null terminator
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIINullTo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      var length = Math.Min(str.Length, max - 1);
      BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(pos, 2), (ushort)(length + 1));
      pos += 3 + Encoding.ASCII.GetBytes(str.AsSpan(0, length), bytes.Slice(pos + 2));
      bytes[pos - 1] = 0; // null terminator
    }

    // Ascii prepended with one-byte length
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopySmallASCIITo(this string str, ref int pos, Span<byte> bytes)
    {
      bytes[pos++] = (byte)str.Length;
      pos += Encoding.ASCII.GetBytes(str.AsSpan(), bytes.Slice(pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopySmallASCIITo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      var length = Math.Min(255, Math.Min(str.Length, max));
      bytes[pos++] = (byte)length;
      pos += Encoding.ASCII.GetBytes(str.AsSpan(0, length), bytes.Slice(pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopySmallASCIINullTo(this string str, ref int pos, Span<byte> bytes)
    {
      bytes[pos++] = (byte)(str.Length + 1);
      pos += Encoding.ASCII.GetBytes(str.AsSpan(), bytes.Slice(pos));
      bytes[pos++] = 0; // null terminator
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopySmallASCIINullTo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      var length = Math.Min(255, Math.Min(str.Length, max));
      bytes[pos++] = (byte)length;
      pos += Encoding.ASCII.GetBytes(str.AsSpan(0, length), bytes.Slice(pos));
      bytes[pos++] = 0; // null terminator
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIIFixedTo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      int bytesWritten = Encoding.ASCII.GetBytes(str.AsSpan(0, Math.Min(str.Length, max)), bytes.Slice(pos));

      // This is not needed in the current stackalloc implementation, but not guaranteed in the future.
      if (bytesWritten < max)
        bytes.Slice(pos + bytesWritten, max - bytesWritten).Clear();

      pos += max;
    }

    // Unicode prepended with two-byte length
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyUnicodeBigEndianTo(this string str, ref int pos, Span<byte> bytes)
    {
      BinaryPrimitives.WriteUInt16BigEndian(bytes.Slice(pos, 2), (ushort)str.Length);
      pos += 2 + Encoding.BigEndianUnicode.GetBytes(str.AsSpan(), bytes.Slice(pos + 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this byte[] src, ref int pos, Span<byte> bytes) =>
      src.CopyTo(bytes.Slice(pos, src.Length));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this byte[] src, ref int pos, int max, Span<byte> bytes) =>
      src.CopyTo(bytes.Slice(pos, Math.Min(max, src.Length)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this Span<byte> src, ref int pos, Span<byte> bytes) =>
      src.CopyTo(bytes.Slice(pos, src.Length));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this Span<byte> src, ref int pos, int max, Span<byte> bytes) =>
      src.CopyTo(bytes.Slice(pos, Math.Min(max, src.Length)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this ReadOnlySpan<byte> src, ref int pos, Span<byte> bytes) =>
      src.CopyTo(bytes.Slice(pos, src.Length));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo(this ReadOnlySpan<byte> src, ref int pos, int max, Span<byte> bytes) =>
      src.CopyTo(bytes.Slice(pos, Math.Min(max, src.Length)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(this Span<byte> span, ref int pos, int amount)
    {
      span.Slice(pos, amount).Clear();
      pos += amount;
    }
  }
}
