/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
  /// <summary>
  ///   Handles random number generation.
  /// </summary>
  public static class RandomImpl
  {
    private static readonly IRandomImpl _Random;

    static RandomImpl()
    {
      if (Core.Is64Bit)
      {
        if (Core.Unix && File.Exists("rdrand.so"))
          _Random = new RDRandUnix();
        else if (File.Exists("rdrand.dll"))
          _Random = new RDRand64();
      }

      if (_Random == null || (_Random is IHardwareRNG rng && !rng.IsSupported()))
        _Random = new CSPRandom();
    }

    public static bool IsHardwareRNG => _Random is IHardwareRNG;

    public static Type Type => _Random.GetType();

    public static int Next(int c) => _Random.Next(c);

    public static bool NextBool() => _Random.NextBool();

    public static void NextBytes(Span<byte> b) => _Random.NextBytes(b);

    public static double NextDouble() => _Random.NextDouble();
  }

  public interface IRandomImpl
  {
    int Next(int c);
    bool NextBool();
    void NextBytes(Span<byte> b);
    double NextDouble();
  }

  public interface IHardwareRNG
  {
    bool IsSupported();
  }

  public abstract class BaseRandom : IRandomImpl
  {
    internal abstract void GetBytes(Span<byte> b);
    internal abstract void GetBytes(byte[] b, int offset, int count);

    public virtual void NextBytes(Span<byte> b) => GetBytes(b);
    public virtual int Next(int c) => (int)(c * NextDouble());
    public virtual bool NextBool() => (NextByte() & 1) == 1;

    public virtual byte NextByte()
    {
      byte[] b = new byte[1];
      GetBytes(b, 0, 1);
      return b[0];
    }

    public virtual unsafe double NextDouble()
    {
      byte[] b = new byte[8];

      if (BitConverter.IsLittleEndian)
      {
        b[7] = 0;
        GetBytes(b, 0, 7);
      }
      else
      {
        b[0] = 0;
        GetBytes(b, 1, 7);
      }

      ulong r;
      fixed (byte* buf = b)
      {
        r = *(ulong*)&buf[0] >> 3;
      }

      /* double: 53 bits of significand precision
       * ulong.MaxValue >> 11 = 9007199254740991
       * 2^53 = 9007199254740992
       */

      return (double)r / 9007199254740992;
    }
  }

  public sealed class CSPRandom : BaseRandom
  {
    private static int BUFFER_SIZE = 0x4000;
    private static int LARGE_REQUEST = 0x40;
    private byte[] _Buffer = new byte[BUFFER_SIZE];
    private RNGCryptoServiceProvider _CSP = new RNGCryptoServiceProvider();

    private ManualResetEvent _filled = new ManualResetEvent(false);

    private int _Index;

    private object _sync = new object();

    private byte[] _Working = new byte[BUFFER_SIZE];

    public CSPRandom()
    {
      _CSP.GetBytes(_Working);
      Task.Run(Fill);
    }

    public override void NextBytes(Span<byte> b)
    {
      int c = b.Length;

      if (c >= LARGE_REQUEST)
      {
        lock (_sync)
        {
          _CSP.GetBytes(b);
        }

        return;
      }

      GetBytes(b);
    }

    private void CheckSwap(int c)
    {
      if (_Index + c < BUFFER_SIZE)
        return;

      _filled.WaitOne();

      byte[] b = _Working;
      _Working = _Buffer;
      _Buffer = b;
      _Index = 0;

      _filled.Reset();

      Task.Run(Fill);
    }

    private void Fill()
    {
      _CSP.GetBytes(_Buffer);
      _filled.Set();
    }

    internal override void GetBytes(Span<byte> b)
    {
      int c = b.Length;

      lock (_sync)
      {
        CheckSwap(c);
        _Working.CopyTo(b);
        _Index += c;
      }
    }

    internal override void GetBytes(byte[] b, int offset, int count)
    {
      GetBytes(b.AsSpan(offset, count));
    }

    public override byte NextByte()
    {
      lock (_sync)
      {
        CheckSwap(1);
        return _Working[_Index++];
      }
    }
  }

  public sealed class RDRandUnix : BaseRandom, IHardwareRNG
  {
    [DllImport("rdrand.so")]
    internal static extern RDRandError rdrand_32(ref uint rand, bool retry);

    [DllImport("rdrand.so")]
    internal static extern unsafe RDRandError rdrand_get_bytes(int n, byte* buffer);

    public bool IsSupported()
    {
      uint r = 0;
      return rdrand_32(ref r, true) == RDRandError.Success;
    }

    internal override unsafe void GetBytes(Span<byte> b)
    {
      fixed(byte* ptr = b)
        rdrand_get_bytes(b.Length, ptr);
    }

    internal override void GetBytes(byte[] b, int offset, int count)
    {
      GetBytes(b.AsSpan().Slice(offset, count));
    }
  }

  public sealed class RDRand64 : BaseRandom, IHardwareRNG
  {
    [DllImport("rdrand64")]
    internal static extern RDRandError rdrand_64(ref ulong rand, bool retry);

    [DllImport("rdrand64")]
    internal static extern unsafe RDRandError rdrand_get_bytes(int n, byte* buffer);

    public bool IsSupported()
    {
      ulong r = 0;
      return rdrand_64(ref r, true) == RDRandError.Success;
    }

    internal override unsafe void GetBytes(Span<byte> b)
    {
      fixed (byte* ptr = b)
        rdrand_get_bytes(b.Length, ptr);
    }

    internal override void GetBytes(byte[] b, int offset, int count)
    {
      GetBytes(b.AsSpan(offset, count));
    }
  }

  public enum RDRandError
  {
    Unknown = -4,
    Unsupported = -3,
    Supported = -2,
    NotReady = -1,

    Failure = 0,

    Success = 1
  }
}
