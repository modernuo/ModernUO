using System;
using Server.Random;

namespace Server.Tests;

/// <summary>
/// Replaces <see cref="BuiltInRng.Generator"/> with a <see cref="System.Random"/> that returns
/// a fixed value, making skill checks and other RNG-dependent code deterministic in tests.
/// Restores the original generator on <see cref="Dispose"/>.
/// <para>Usage:</para>
/// <code>
/// using var rng = new PredictableRandom(10); // Utility.Random(21) returns 10
/// </code>
/// </summary>
public sealed class PredictableRandom : IDisposable
{
    private readonly System.Random _original;

    public PredictableRandom(int fixedValue)
    {
        _original = BuiltInRng.Generator;
        BuiltInRng.Generator = new FixedRandom(fixedValue);
    }

    public void Dispose()
    {
        BuiltInRng.Generator = _original;
    }

    private sealed class FixedRandom(int value) : System.Random
    {
        public override int Next() => value;
        public override int Next(int maxValue) => Math.Clamp(value, 0, maxValue - 1);
        public override int Next(int minValue, int maxValue) => Math.Clamp(value + minValue, minValue, maxValue - 1);
        public override long NextInt64() => value;
        public override long NextInt64(long maxValue) => Math.Clamp(value, 0, maxValue - 1);
        public override long NextInt64(long minValue, long maxValue) => Math.Clamp(value + minValue, minValue, maxValue - 1);
        public override double NextDouble() => Math.Clamp(value / 20.0, 0.0, 1.0);

        public override void NextBytes(byte[] buffer) => Array.Fill(buffer, (byte)Math.Clamp(value, 0, 255));
        public override void NextBytes(Span<byte> buffer) => buffer.Fill((byte)Math.Clamp(value, 0, 255));
    }
}
