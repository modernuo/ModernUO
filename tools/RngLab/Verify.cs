using System;

namespace RngLab;

public static class Verify
{
    public static void Run()
    {
        // numpy 2.5.2: PCG64DXSM with state=0x0123456789ABCDEF0FEDCBA987654321, inc=(54<<1)|1, random_raw(4)
        Check(
            "Pcg64Dxsm (numpy)",
            new Pcg64Dxsm(0x0123456789ABCDEFUL, 0x0FEDCBA987654321UL),
            [0xe9518a0afe3e6ec2, 0x745b174f045f2e66, 0x55a7e7ebd32e9877, 0xe7c0ed5289dbbd8b]
        );

        // Computed from the lxm_random.hpp algorithm (Python port), raw init(a=1, s=0, x0=1, x1=2).
        Check(
            "L64X128Mix (lxm_random.hpp)",
            new L64X128Mix(1, 0, 1, 2),
            [0xc6caf8cba3316acc, 0x5c339740658b0fb1, 0x960b42f1f3ba767f, 0x37bb5aedcbb91d57]
        );

        Console.WriteLine($"Bmi2.X64.IsSupported = {System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported}");
    }

    private static void Check(string name, IRng64 rng, ulong[] expected)
    {
        var ok = true;
        for (var i = 0; i < expected.Length; i++)
        {
            var v = rng.NextUInt64();
            if (v != expected[i])
            {
                ok = false;
                Console.WriteLine($"  {name}[{i}] = 0x{v:x16}, expected 0x{expected[i]:x16}");
            }
        }

        Console.WriteLine($"{name}: {(ok ? "matches reference vector" : "MISMATCH")}");
    }
}
