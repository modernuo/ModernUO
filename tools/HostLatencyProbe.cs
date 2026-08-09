#:property TreatWarningsAsErrors=false
// Host operation-cost probe.
//
// The ModernUO event loop reads the clock every iteration and, on Windows, polls pending accept
// slots with a syscall. On bare metal those cost tens of nanoseconds and vanish. On a virtualised
// host without invariant-TSC passthrough they can trap to the hypervisor and cost microseconds,
// which is the difference between a loop running 1,200,000 cycles/sec and one running 20,000.
//
// This measures the primitives directly so a slow shard can be attributed to the host rather than
// guessed at. It touches nothing in ModernUO and needs no shard running.
//
// Run:  dotnet run tools/HostLatencyProbe.cs
//
// Reference (Windows desktop, dedicated cores) is printed alongside each result.

using System.Diagnostics;
using System.Runtime.InteropServices;

const int Warmup = 100_000;
const int Iterations = 2_000_000;

Console.WriteLine($"OS         : {RuntimeInformation.OSDescription}");
Console.WriteLine($"Arch       : {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Processors : {Environment.ProcessorCount}");
Console.WriteLine($"QPC freq   : {Stopwatch.Frequency:N0} Hz");
Console.WriteLine($"HighRes    : {Stopwatch.IsHighResolution}");
Console.WriteLine();
Console.WriteLine($"{"operation",-34}{"ns/op",12}  {"desktop ref",-14} verdict");
Console.WriteLine(new string('-', 86));

Measure("Stopwatch.GetTimestamp()", 20, () => Stopwatch.GetTimestamp());
Measure("DateTime.UtcNow", 25, () => DateTime.UtcNow.Ticks);

if (OperatingSystem.IsWindows())
{
    // Mirrors CheckAcceptExCompletions, which polls each pending accept slot this way. An
    // already-signalled event is the cheapest possible case, so this is a floor, not a typical cost.
    var evt = CreateEventW(0, 1, 1, 0);
    if (evt != 0)
    {
        Measure("WaitForSingleObject(signalled, 0)", 250, () => (long)WaitForSingleObject(evt, 0));
        CloseHandle(evt);
    }
}

Console.WriteLine();
Console.WriteLine("A host whose clock reads cost microseconds rather than nanoseconds is trapping to");
Console.WriteLine("the hypervisor. That penalises every loop iteration and cannot be tuned away in");
Console.WriteLine("the server -- it is a host or VM-configuration problem (TSC passthrough).");

static void Measure(string name, double desktopNs, Func<long> op)
{
    long sink = 0;
    for (var i = 0; i < Warmup; i++)
    {
        sink += op();
    }

    var sw = Stopwatch.StartNew();
    for (var i = 0; i < Iterations; i++)
    {
        sink += op();
    }

    sw.Stop();
    GC.KeepAlive(sink);

    var ns = sw.Elapsed.TotalNanoseconds / Iterations;
    var ratio = ns / desktopNs;
    var verdict = ratio switch
    {
        < 3 => "normal",
        < 10 => "SLOW (~" + ratio.ToString("F0") + "x)",
        _ => "TRAPPING (~" + ratio.ToString("F0") + "x)"
    };

    Console.WriteLine($"{name,-34}{ns,12:F1}  {desktopNs + " ns",-14} {verdict}");
}

[DllImport("kernel32.dll")]
static extern nint CreateEventW(nint attrs, int manualReset, int initialState, nint name);

[DllImport("kernel32.dll")]
static extern uint WaitForSingleObject(nint handle, uint ms);

[DllImport("kernel32.dll")]
static extern int CloseHandle(nint handle);
