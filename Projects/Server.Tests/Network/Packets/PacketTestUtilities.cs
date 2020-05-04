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
    public static void CopyASCIITo(this string str, Span<byte> bytes) => Encoding.ASCII.GetBytes(str.AsSpan(), bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIITo(this string str, int max, Span<byte> bytes) => Encoding.ASCII.GetBytes(str.AsSpan(0, Math.Min(str.Length, max)), bytes);


    // Ref Positions
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
    public static void CopyASCIITo(this string str, ref int pos, Span<byte> bytes)
    {
      pos += Encoding.ASCII.GetBytes(str.AsSpan(), bytes.Slice(pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyASCIITo(this string str, ref int pos, int max, Span<byte> bytes)
    {
      pos += Encoding.ASCII.GetBytes(str.AsSpan(0, Math.Min(str.Length, max)), bytes.Slice(pos));
    }
  }
}
