using System;

namespace RngLab;

// Compares observed failure-run lengths against the exact geometric law. A generator that
// "fell into close repeats" would show excess long runs (positive z) at some success chance.
public static class Streaks
{
    private const double TwoPow53 = 1.0 / (1UL << 53);

    public static void Run(long n)
    {
        foreach (var p in (double[])[0.9, 0.5, 0.02])
        {
            Console.WriteLine($"\n=== success chance p = {p}  (n = {n:N0} rolls per generator) ===");
            Report("Xoshiro256**", Simulate(new Xoshiro256SS(12345), p, n, false), p);
            Report("PCG64-DXSM", Simulate(new Pcg64Dxsm(12345), p, n, false), p);
            Report("SFC64", Simulate(new Sfc64(12345), p, n, false), p);
            Report("L64X128Mix", Simulate(new L64X128Mix(12345), p, n, false), p);
            // Shared global instance: other consumers burn 0..63 draws between this player's rolls.
            Report("Xoshiro256** shared", Simulate(new Xoshiro256SS(999), p, n, true), p);
        }

        PrdDemo(n);
    }

    // hist[k] = number of maximal failure runs of length exactly k (k capped at the last bin).
    private static long[] Simulate(IRng64 g, double p, long n, bool interleave)
    {
        var hist = new long[2000];
        long run = 0;
        for (long i = 0; i < n; i++)
        {
            if (interleave)
            {
                var skip = (int)(g.NextUInt64() >> 58);
                for (var s = 0; s < skip; s++)
                {
                    g.NextUInt64();
                }
            }

            if ((g.NextUInt64() >> 11) * TwoPow53 < p)
            {
                if (run > 0)
                {
                    hist[Math.Min(run, hist.Length - 1)]++;
                }

                run = 0;
            }
            else
            {
                run++;
            }
        }

        return hist;
    }

    private static void Report(string name, long[] hist, double p)
    {
        var q = 1 - p;
        long runs = 0;
        for (var k = 1; k < hist.Length; k++)
        {
            runs += hist[k];
        }

        // P(run length = k | a run) = q^(k-1) * p. Chi-square over bins with expected >= 50, tail pooled.
        double chi = 0;
        var dof = -1;
        double tailObs = 0, tailExp = 0;
        var longest = 0;
        for (var k = 1; k < hist.Length; k++)
        {
            var e = runs * Math.Pow(q, k - 1) * p;
            if (hist[k] > 0)
            {
                longest = k;
            }

            if (e >= 50)
            {
                chi += (hist[k] - e) * (hist[k] - e) / e;
                dof++;
            }
            else
            {
                tailObs += hist[k];
                tailExp += e;
            }
        }

        if (tailExp >= 50)
        {
            chi += (tailObs - tailExp) * (tailObs - tailExp) / tailExp;
            dof++;
        }

        var z = (chi - dof) / Math.Sqrt(2.0 * dof);
        Console.WriteLine($"{name,-22} runs={runs,12:N0}  chi2={chi,8:F1} dof={dof,4}  z={z,6:F2}  longest fail streak={longest}");
    }

    // Pseudo-random distribution: chance = C * (failures since last success + 1), same long-run mean.
    private static void PrdDemo(long n)
    {
        const double p = 0.02;
        var c = SolvePrdC(p);
        var g = new Xoshiro256SS(7);
        Console.WriteLine($"\n=== Bad-luck protection (PRD, C = {c:F6}) vs plain rolls, nominal p = {p} ===");

        long winsPlain = 0, winsPrd = 0, worstPlain = 0, worstPrd = 0, runPlain = 0, runPrd = 0, longPlain = 0, longPrd = 0;
        for (long i = 0; i < n; i++)
        {
            if ((g.NextUInt64() >> 11) * TwoPow53 < p)
            {
                winsPlain++;
                longPlain += runPlain >= 150 ? 1 : 0;
                worstPlain = Math.Max(worstPlain, runPlain);
                runPlain = 0;
            }
            else
            {
                runPlain++;
            }

            if ((g.NextUInt64() >> 11) * TwoPow53 < c * (runPrd + 1))
            {
                winsPrd++;
                longPrd += runPrd >= 150 ? 1 : 0;
                worstPrd = Math.Max(worstPrd, runPrd);
                runPrd = 0;
            }
            else
            {
                runPrd++;
            }
        }

        Console.WriteLine($"plain: rate={winsPlain / (double)n:F5}  worst dry spell={worstPlain,4}  dry spells >=150 per 1k wins={1000.0 * longPlain / winsPlain:F2}");
        Console.WriteLine($"PRD  : rate={winsPrd / (double)n:F5}  worst dry spell={worstPrd,4}  dry spells >=150 per 1k wins={1000.0 * longPrd / winsPrd:F2}");
    }

    private static double SolvePrdC(double p)
    {
        double lo = 0, hi = p;
        for (var iter = 0; iter < 100; iter++)
        {
            var c = (lo + hi) / 2;
            double survive = 1, expected = 0;
            for (var k = 1; survive > 1e-15; k++)
            {
                expected += survive;
                survive *= 1 - Math.Min(1, c * k);
            }

            if (1 / expected < p)
            {
                lo = c;
            }
            else
            {
                hi = c;
            }
        }

        return (lo + hi) / 2;
    }
}
