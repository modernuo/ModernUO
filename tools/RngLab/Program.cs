using System;
using BenchmarkDotNet.Running;
using RngLab;

switch (args.Length > 0 ? args[0] : "verify")
{
    case "verify":
        Verify.Run();
        break;
    case "streaks":
        Streaks.Run(args.Length > 1 ? long.Parse(args[1]) : 200_000_000);
        break;
    case "disasm":
        // Interface calls force a standalone NextUInt64 body. Run with
        // DOTNET_TieredCompilation=0 DOTNET_JitDisasm=NextUInt64
        ulong acc = 0;
        foreach (var g in (IRng64[])[new Pcg64Dxsm(1), new L64X128Mix(1)])
        {
            acc ^= g.NextUInt64();
        }

        Console.WriteLine(acc);
        break;
    case "bench":
        BenchmarkSwitcher.FromAssembly(typeof(Verify).Assembly).Run(args[1..]);
        break;
    default:
        Console.WriteLine("usage: verify | streaks [n] | disasm | bench [--filter *] [--join] [--affinity 4]");
        break;
}
