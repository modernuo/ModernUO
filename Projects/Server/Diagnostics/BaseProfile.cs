using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Server.Diagnostics;

public abstract class BaseProfile
{
    private readonly Stopwatch _stopwatch;

    protected BaseProfile(string name)
    {
        Name = name;

        _stopwatch = new Stopwatch();
    }

    public string Name { get; }

    public long Count { get; private set; }

    public TimeSpan AverageTime => TimeSpan.FromTicks(TotalTime.Ticks / Math.Max(Count, 1));

    public TimeSpan PeakTime { get; private set; }

    public TimeSpan TotalTime { get; private set; }

    public static void WriteAll<T>(TextWriter op, IEnumerable<T> profiles) where T : BaseProfile
    {
        var list = new List<T>(profiles);

        list.Sort((a, b) => -a.TotalTime.CompareTo(b.TotalTime));

        foreach (var prof in list)
        {
            prof.WriteTo(op);
            op.WriteLine();
        }
    }

    public virtual void Start()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Reset();
        }

        _stopwatch.Start();
    }

    public virtual void Finish()
    {
        var elapsed = _stopwatch.Elapsed;

        TotalTime += elapsed;

        if (elapsed > PeakTime)
        {
            PeakTime = elapsed;
        }

        Count++;

        _stopwatch.Reset();
    }

    public virtual void WriteTo(TextWriter op)
    {
        op.Write(
            "{0,-100} {1,12:N0} {2,12:F5} {3,-12:F5} {4,12:F5}",
            Name,
            Count,
            AverageTime.TotalSeconds,
            PeakTime.TotalSeconds,
            TotalTime.TotalSeconds
        );
    }
}
