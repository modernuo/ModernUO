using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Compression
{
  public static class ZLib
  {
    public static readonly ICompressor Compressor;

    static ZLib()
    {
      if (RuntimeUtility.IsUnix)
        Compressor = new UnixCompressor();
      else
        Compressor = new Compressor();
    }

    public static int MaxPackSize(int sourceLength) => (int)Compressor.CompressBound((ulong)sourceLength);

    public static unsafe ZLibError Pack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, int sourceLength)
    {
      var destLengthLong = (ulong)destLength;
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        var e = Compressor.Compress(Unsafe.AsRef<int>(dPtr), ref destLengthLong, Unsafe.AsRef<int>(sPtr),
          (ulong)sourceLength);
        destLength = (int)destLengthLong;
        return e;
      }
    }

    public static unsafe ZLibError Pack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, ZLibQuality quality)
    {
      var destLengthLong = (ulong)destLength;
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        var e = Compressor.Compress(Unsafe.AsRef<int>(dPtr), ref destLengthLong, Unsafe.AsRef<int>(sPtr),
          (ulong)source.Length, quality);
        destLength = (int)destLengthLong;
        return e;
      }
    }

    public static unsafe ZLibError Pack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, int sourceLength,
      ZLibQuality quality)
    {
      var destLengthLong = (ulong)destLength;
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        var e = Compressor.Compress(Unsafe.AsRef<int>(dPtr), ref destLengthLong, Unsafe.AsRef<int>(sPtr),
          (ulong)sourceLength, quality);
        destLength = (int)destLengthLong;
        return e;
      }
    }

    public static unsafe ZLibError Unpack(Span<byte> dest, ref int destLength, ReadOnlySpan<byte> source, int sourceLength)
    {
      var destLengthLong = (ulong)destLength;
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        var e = Compressor.Decompress(Unsafe.AsRef<int>(dPtr), ref destLengthLong, Unsafe.AsRef<int>(sPtr),
          (ulong)sourceLength);
        destLength = (int)destLengthLong;
        return e;
      }
    }

    public static ulong MaxPackSize(ulong sourceLength) => Compressor.CompressBound(sourceLength);

    public static unsafe ZLibError Pack(Span<byte> dest, ref ulong destLength, ReadOnlySpan<byte> source, ulong sourceLength)
    {
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        return Compressor.Compress(Unsafe.AsRef<int>(dPtr), ref destLength, Unsafe.AsRef<int>(sPtr), sourceLength);
      }
    }

    public static unsafe ZLibError Pack(Span<byte> dest, ref ulong destLength, ReadOnlySpan<byte> source,
      ZLibQuality quality)
    {
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        return Compressor.Compress(Unsafe.AsRef<int>(dPtr), ref destLength, Unsafe.AsRef<int>(sPtr), (ulong)source.Length,
          quality);
      }
    }

    public static unsafe ZLibError Pack(Span<byte> dest, ref ulong destLength, ReadOnlySpan<byte> source, ulong sourceLength,
      ZLibQuality quality)
    {
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        return Compressor.Compress(Unsafe.AsRef<int>(dPtr), ref destLength, Unsafe.AsRef<int>(sPtr), sourceLength, quality);
      }
    }

    public static unsafe ZLibError Unpack(Span<byte> dest, ref ulong destLength, ReadOnlySpan<byte> source,
      ulong sourceLength)
    {
      fixed (byte* dPtr = &MemoryMarshal.GetReference(dest), sPtr = &MemoryMarshal.GetReference(source))
      {
        return Compressor.Decompress(Unsafe.AsRef<int>(dPtr), ref destLength, Unsafe.AsRef<int>(sPtr), sourceLength);
      }
    }
  }

  public interface ICompressor
  {
    string Version { get; }
    ZLibError Compress(in int dest, ref ulong destLength, in int source, ulong sourceLength);
    ZLibError Compress(in int dest, ref ulong destLength, in int source, ulong sourceLength, ZLibQuality quality);
    ZLibError Decompress(in int dest, ref ulong destLength, in int source, ulong sourceLength);
    ulong CompressBound(ulong sourceLength);
  }

  public class Compressor : ICompressor
  {
    public string Version => SafeNativeMethods.zlibVersion();

    public ZLibError Compress(in int dest, ref ulong destLength, in int source, ulong sourceLength) =>
      SafeNativeMethods.compress(dest, ref destLength, source, sourceLength);

    public ZLibError Compress(in int dest, ref ulong destLength, in int source, ulong sourceLength,
      ZLibQuality quality) => SafeNativeMethods.compress2(dest, ref destLength, source, sourceLength, quality);

    public ZLibError Decompress(in int dest, ref ulong destLength, in int source, ulong sourceLength) =>
      SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);

    public ulong CompressBound(ulong sourceLength) => SafeNativeMethods.compressBound(sourceLength);

    private static class SafeNativeMethods
    {
      [DllImport("zlib", CharSet = CharSet.Unicode)]
      internal static extern string zlibVersion();

      [DllImport("zlib")]
      internal static extern ZLibError compress(in int dest, ref ulong destLength, in int source, ulong sourceLength);

      [DllImport("zlib")]
      internal static extern ZLibError compress2(in int dest, ref ulong destLength, in int source, ulong sourceLength,
        ZLibQuality quality);

      [DllImport("zlib")]
      internal static extern ZLibError uncompress(in int dest, ref ulong destLength, in int source, ulong sourceLength);

      [DllImport("zlib")]
      internal static extern ulong compressBound(ulong sourceLen);
    }
  }

  public class UnixCompressor : ICompressor
  {
    public string Version => SafeNativeMethods.zlibVersion();

    public ZLibError Compress(in int dest, ref ulong destLength, in int source, ulong sourceLength) =>
      SafeNativeMethods.compress(dest, ref destLength, source, sourceLength);

    public ZLibError Compress(in int dest, ref ulong destLength, in int source, ulong sourceLength,
      ZLibQuality quality) => SafeNativeMethods.compress2(dest, ref destLength, source, sourceLength, quality);

    public ZLibError Decompress(in int dest, ref ulong destLength, in int source, ulong sourceLength) =>
      SafeNativeMethods.uncompress(dest, ref destLength, source, sourceLength);

    public ulong CompressBound(ulong sourceLength) => SafeNativeMethods.compressBound(sourceLength);

    internal static class SafeNativeMethods
    {
      [DllImport("libz", CharSet = CharSet.Unicode)]
      internal static extern string zlibVersion();

      [DllImport("libz")]
      internal static extern ZLibError compress(in int dest, ref ulong destLength, in int source, ulong sourceLength);

      [DllImport("libz")]
      internal static extern ZLibError compress2(in int dest, ref ulong destLength, in int source, ulong sourceLength,
        ZLibQuality quality);

      [DllImport("libz")]
      internal static extern ZLibError uncompress(in int dest, ref ulong destLength, in int source, ulong sourceLength);

      [DllImport("libz")]
      internal static extern ulong compressBound(ulong sourceLen);
    }
  }

  public enum ZLibError
  {
    VersionError = -6,
    BufferError = -5,
    MemoryError = -4,
    DataError = -3,
    StreamError = -2,
    FileError = -1,
    Okay = 0,
    StreamEnd = 1,
    NeedDictionary = 2
  }

  public enum ZLibQuality
  {
    Default = -1,
    None = 0,
    Speed = 1,
    Size = 9
  }
}
