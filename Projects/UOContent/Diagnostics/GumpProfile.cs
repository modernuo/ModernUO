using System;
using System.Collections.Generic;

namespace Server.Diagnostics;

public class GumpProfile : BaseProfile
{
    private static readonly Dictionary<Type, GumpProfile> _profiles = new();

    public GumpProfile(Type type) : base(type.FullName)
    {
    }

    public static IEnumerable<GumpProfile> Profiles => _profiles.Values;

    public static GumpProfile Acquire(Type type)
    {
        if (!Core.Profiling)
        {
            return null;
        }

        if (!_profiles.TryGetValue(type, out var prof))
        {
            _profiles.Add(type, prof = new GumpProfile(type));
        }

        return prof;
    }
}
