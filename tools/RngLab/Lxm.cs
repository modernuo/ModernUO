using System.Numerics;
using System.Runtime.CompilerServices;

namespace RngLab;

// L64X128MixRandom (Steele & Vigna 2021, Fig. 1): 64-bit LCG + raw xoroshiro128, summed, then the lea64 mixer.
// Output is computed from the pre-advance state, so the mixer runs off both update chains.
public sealed class L64X128Mix : IRng64
{
    private const ulong M = 0xD1342543DE82EF95UL;
    private const ulong LeaMul = 0xDABA0B6EB09322E3UL;

    private readonly ulong _a;
    private ulong _s, _x0, _x1;

    public L64X128Mix(ulong a, ulong s, ulong x0, ulong x1)
    {
        _a = a | 1;
        _s = s;
        _x0 = x0;
        _x1 = x1;
    }

    public L64X128Mix(ulong seed)
    {
        _a = SplitMix.Next(ref seed) | 1;
        _s = SplitMix.Next(ref seed);
        _x0 = SplitMix.Next(ref seed);
        _x1 = SplitMix.Next(ref seed);
        if ((_x0 | _x1) == 0)
        {
            _x0 = 0x9E3779B97F4A7C15UL;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Lea64(ulong z)
    {
        z = (z ^ (z >> 32)) * LeaMul;
        z = (z ^ (z >> 32)) * LeaMul;
        return z ^ (z >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
        ulong s = _s, q0 = _x0, q1 = _x1;
        var z = s + q0;

        _s = M * s + _a;

        q1 ^= q0;
        _x0 = BitOperations.RotateLeft(q0, 24) ^ q1 ^ (q1 << 16);
        _x1 = BitOperations.RotateLeft(q1, 37);

        return Lea64(z);
    }
}

// Xoroshiro128++ — the paper's own pick for a single-threaded application (§10).
public sealed class Xoroshiro128PP : IRng64
{
    private ulong _s0, _s1;

    public Xoroshiro128PP(ulong seed)
    {
        _s0 = SplitMix.Next(ref seed);
        _s1 = SplitMix.Next(ref seed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
        ulong s0 = _s0, s1 = _s1;
        var result = BitOperations.RotateLeft(s0 + s1, 17) + s0;
        s1 ^= s0;
        _s0 = BitOperations.RotateLeft(s0, 49) ^ s1 ^ (s1 << 21);
        _s1 = BitOperations.RotateLeft(s1, 28);
        return result;
    }
}

public static class StaticLxm
{
    private const ulong M = 0xD1342543DE82EF95UL;
    private const ulong A = 0x5851F42D4C957F2DUL;
    private static ulong _s = 0x0123456789ABCDEFUL, _x0 = 0x9E3779B97F4A7C15UL, _x1 = 0xBF58476D1CE4E5B9UL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NextUInt64()
    {
        ulong s = _s, q0 = _x0, q1 = _x1;
        var z = s + q0;
        _s = M * s + A;
        q1 ^= q0;
        _x0 = BitOperations.RotateLeft(q0, 24) ^ q1 ^ (q1 << 16);
        _x1 = BitOperations.RotateLeft(q1, 37);
        return L64X128Mix.Lea64(z);
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
