using BenchmarkDotNet.Attributes;

namespace RngLab;

// Each op = 1024 draws, so per-draw cost = reported time / 1024.
// Pin to one core on hybrid/noisy machines: bench --filter * --join --affinity 4
public class RawBench
{
    private const int N = 1024;
    private readonly Xoshiro256SS _xo = new(1);
    private readonly Xoroshiro128PP _xoro = new(1);
    private readonly Sfc64 _sfc = new(1);
    private readonly L64X128Mix _lxm = new(1);
    private readonly Pcg64Dxsm _pcg = new(1);

    [Benchmark(Baseline = true)]
    public ulong Xoshiro256SS()
    {
        ulong acc = 0;
        for (var i = 0; i < N; i++) { acc ^= _xo.NextUInt64(); }
        return acc;
    }

    [Benchmark]
    public ulong Xoroshiro128PP()
    {
        ulong acc = 0;
        for (var i = 0; i < N; i++) { acc ^= _xoro.NextUInt64(); }
        return acc;
    }

    [Benchmark]
    public ulong Sfc64()
    {
        ulong acc = 0;
        for (var i = 0; i < N; i++) { acc ^= _sfc.NextUInt64(); }
        return acc;
    }

    [Benchmark]
    public ulong L64X128Mix()
    {
        ulong acc = 0;
        for (var i = 0; i < N; i++) { acc ^= _lxm.NextUInt64(); }
        return acc;
    }

    [Benchmark]
    public ulong Pcg64Dxsm()
    {
        ulong acc = 0;
        for (var i = 0; i < N; i++) { acc ^= _pcg.NextUInt64(); }
        return acc;
    }
}

// What game code actually calls: Utility.Random(100) and Utility.RandomDouble().
public class ApiBench
{
    private const int N = 1024;
    private readonly System.Random _sys = new();

    [Benchmark(Baseline = true)]
    public int SystemRandom_Next100()
    {
        var acc = 0;
        for (var i = 0; i < N; i++) { acc += _sys.Next(100); }
        return acc;
    }

    [Benchmark]
    public int StaticXoshiro_Next100()
    {
        var acc = 0;
        for (var i = 0; i < N; i++) { acc += StaticXoshiro.Next(100); }
        return acc;
    }

    [Benchmark]
    public int StaticPcg_Next100()
    {
        var acc = 0;
        for (var i = 0; i < N; i++) { acc += StaticPcg.Next(100); }
        return acc;
    }

    [Benchmark]
    public int StaticLxm_Next100()
    {
        var acc = 0;
        for (var i = 0; i < N; i++) { acc += StaticLxm.Next(100); }
        return acc;
    }

    [Benchmark]
    public double SystemRandom_NextDouble()
    {
        double acc = 0;
        for (var i = 0; i < N; i++) { acc += _sys.NextDouble(); }
        return acc;
    }

    [Benchmark]
    public double StaticXoshiro_NextDouble()
    {
        double acc = 0;
        for (var i = 0; i < N; i++) { acc += StaticXoshiro.NextDouble(); }
        return acc;
    }

    [Benchmark]
    public double StaticPcg_NextDouble()
    {
        double acc = 0;
        for (var i = 0; i < N; i++) { acc += StaticPcg.NextDouble(); }
        return acc;
    }

    [Benchmark]
    public double StaticLxm_NextDouble()
    {
        double acc = 0;
        for (var i = 0; i < N; i++) { acc += StaticLxm.NextDouble(); }
        return acc;
    }
}
