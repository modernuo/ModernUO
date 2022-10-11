using System.Collections.Generic;
using System.IO;

namespace Server.Diagnostics;

public class TimerProfile : BaseProfile
{
    private static readonly Dictionary<string, TimerProfile> _profiles = new();

    public TimerProfile(string name)
        : base(name)
    {
    }

    public static IEnumerable<TimerProfile> Profiles => _profiles.Values;

    public long Created { get; set; }

    public long Started { get; set; }

    public long Stopped { get; set; }

    public static TimerProfile Acquire(string name)
    {
        if (!Core.Profiling)
        {
            return null;
        }

        if (!_profiles.TryGetValue(name, out var prof))
        {
            _profiles.Add(name, prof = new TimerProfile(name));
        }

        return prof;
    }

    public override void WriteTo(TextWriter op)
    {
        base.WriteTo(op);

        op.Write("\t{0,12:N0} {1,12:N0} {2,-12:N0}", Created, Started, Stopped);
    }
}
