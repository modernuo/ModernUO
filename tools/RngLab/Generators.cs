using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace RngLab;

public interface IRng64
{
    ulong NextUInt64();
}

public static class SplitMix
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Next(ref ulong x)
    {
        var z = x += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}

// Same algorithm .NET's unseeded System.Random uses on 64-bit: Xoshiro256**.
public sealed class Xoshiro256SS : IRng64
{
    private ulong _s0, _s1, _s2, _s3;

    public Xoshiro256SS(ulong seed)
    {
        _s0 = SplitMix.Next(ref seed);
        _s1 = SplitMix.Next(ref seed);
        _s2 = SplitMix.Next(ref seed);
        _s3 = SplitMix.Next(ref seed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
        ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;
        var result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
        var t = s1 << 17;
        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;
        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 45);
        _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;
        return result;
    }
}

// SFC64 (Doty-Humphrey). The counter guarantees a minimum period of 2^64 per seed.
public sealed class Sfc64 : IRng64
{
    private ulong _a, _b, _c, _w;

    public Sfc64(ulong seed)
    {
        _a = _b = _c = seed;
        _w = 1;
        for (var i = 0; i < 12; i++)
        {
            NextUInt64();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
        var tmp = _a + _b + _w++;
        _a = _b ^ (_b >> 11);
        _b = _c + (_c << 3);
        _c = BitOperations.RotateLeft(_c, 24) + tmp;
        return tmp;
    }
}

/// <summary>
/// PCG64-DXSM (numpy's PCG64DXSM), single stream. The LCG step and the output mixer both use the
/// 64-bit "cheap multiplier"; output is mixed from the pre-advance state so it runs off the state's
/// dependency chain. The increment is a compile-time constant (numpy's default stream, (54 &lt;&lt; 1) | 1):
/// its high half is 0, the add folds to an immediate, and the carry is the only cross-half dependency.
/// </summary>
/// <remarks>
/// Lowering notes (.NET 10 x64, verified by DOTNET_JitDisasm): Math.BigMul already emits MULX when BMI2 is
/// present, so an explicit Bmi2.X64.MultiplyNoFlags gains nothing. Every overload that returns the low half
/// (out/pointer/UInt128) spills it through a stack slot; the high-only MultiplyNoFlags(a, b) + a separate
/// IMUL avoids the spill but measured no faster. A UInt128-state version is ~30% slower; a runtime increment ~15%.
/// </remarks>
public sealed class Pcg64Dxsm : IRng64
{
    public const ulong CheapMul = 0xDA942042E4DD58B5UL;
    public const ulong Inc = 109;

    private ulong _hi, _lo;

    public Pcg64Dxsm(ulong stateHi, ulong stateLo)
    {
        _hi = stateHi;
        _lo = stateLo;
    }

    public Pcg64Dxsm(ulong seed) : this(SplitMix.Next(ref seed), SplitMix.Next(ref seed))
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
        ulong hi = _hi, lo = _lo;

        var nhi = Math.BigMul(lo, CheapMul, out var nlo) + hi * CheapMul;
        var sum = nlo + Inc;
        _hi = nhi + (sum < nlo ? 1UL : 0UL);
        _lo = sum;

        hi ^= hi >> 32;
        hi *= CheapMul;
        hi ^= hi >> 48;
        hi *= lo | 1;
        return hi;
    }
}

// 53-bit doubles in [0, 1).
public static class Bounded
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ToDouble(ulong bits) => (bits >> 11) * (1.0 / (1UL << 53));
}

// Shape of a drop-in BuiltInRng replacement: static state, no virtual dispatch, no System.Random wrapper.
public static class StaticPcg
{
    private static ulong _hi = 0x0123456789ABCDEFUL, _lo = 0x0FEDCBA987654321UL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NextUInt64()
    {
        ulong hi = _hi, lo = _lo;

        var nhi = Math.BigMul(lo, Pcg64Dxsm.CheapMul, out var nlo) + hi * Pcg64Dxsm.CheapMul;
        var sum = nlo + Pcg64Dxsm.Inc;
        _hi = nhi + (sum < nlo ? 1UL : 0UL);
        _lo = sum;

        hi ^= hi >> 32;
        hi *= Pcg64Dxsm.CheapMul;
        hi ^= hi >> 48;
        hi *= lo | 1;
        return hi;
    }

    // Lemire's nearly-divisionless bounded int (what System.Random does internally).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next(int maxValue)
    {
        var m = (ulong)(uint)maxValue * (uint)(NextUInt64() >> 32);
        var low = (uint)m;
        if (low < (uint)maxValue)
        {
            var threshold = (uint)-maxValue % (uint)maxValue;
            while (low < threshold)
            {
                m = (ulong)(uint)maxValue * (uint)(NextUInt64() >> 32);
                low = (uint)m;
            }
        }

        return (int)(m >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NextDouble() => Bounded.ToDouble(NextUInt64());
}

public static class StaticXoshiro
{
    private static ulong _s0 = 0x9E3779B97F4A7C15UL, _s1 = 0xBF58476D1CE4E5B9UL,
        _s2 = 0x94D049BB133111EBUL, _s3 = 0x0123456789ABCDEFUL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NextUInt64()
    {
        ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;
        var result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
        var t = s1 << 17;
        s2 ^= s0;
        s3 ^= s1;
        s1 ^= s2;
        s0 ^= s3;
        s2 ^= t;
        s3 = BitOperations.RotateLeft(s3, 45);
        _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;
        return result;
    }

    // Lemire's nearly-divisionless bounded int (what System.Random does internally).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Next(int maxValue)
    {
        var m = (ulong)(uint)maxValue * (uint)(NextUInt64() >> 32);
        var low = (uint)m;
        if (low < (uint)maxValue)
        {
            var threshold = (uint)-maxValue % (uint)maxValue;
            while (low < threshold)
            {
                m = (ulong)(uint)maxValue * (uint)(NextUInt64() >> 32);
                low = (uint)m;
            }
        }

        return (int)(m >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NextDouble() => Bounded.ToDouble(NextUInt64());
}
