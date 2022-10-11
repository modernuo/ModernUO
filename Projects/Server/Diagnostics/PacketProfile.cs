using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server.Diagnostics;

public abstract class BasePacketProfile : BaseProfile
{
    protected BasePacketProfile(string name) : base(name)
    {
    }

    public long TotalLength { get; private set; }

    public double AverageLength => (double)TotalLength / Math.Max(Count, 1);

    public void Finish(long length)
    {
        Finish();

        TotalLength += length;
    }

    public override void WriteTo(TextWriter op)
    {
        base.WriteTo(op);

        op.Write("\t{0,12:F2} {1,-12:N0}", AverageLength, TotalLength);
    }
}

public class PacketSendProfile : BasePacketProfile
{
    private static readonly Dictionary<int, PacketSendProfile> _profiles = new();

    private long _created;

    public PacketSendProfile(int packetId) : base($"0x{packetId:X2}")
    {
    }

    public static IEnumerable<PacketSendProfile> Profiles => _profiles.Values;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PacketSendProfile Acquire(int packetId)
    {
        if (!_profiles.TryGetValue(packetId, out var prof))
        {
            _profiles.Add(packetId, prof = new PacketSendProfile(packetId));
        }

        return prof;
    }

    public void Increment()
    {
        Interlocked.Increment(ref _created);
    }

    public override void WriteTo(TextWriter op)
    {
        base.WriteTo(op);

        op.Write("\t{0,12:N0}", _created);
    }
}

public class PacketReceiveProfile : BasePacketProfile
{
    private static readonly Dictionary<int, PacketReceiveProfile>
        _profiles = new();

    public PacketReceiveProfile(int packetId) : base($"0x{packetId:X2}")
    {
    }

    public static IEnumerable<PacketReceiveProfile> Profiles => _profiles.Values;

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static PacketReceiveProfile Acquire(int packetId)
    {
        if (!_profiles.TryGetValue(packetId, out var prof))
        {
            _profiles.Add(packetId, prof = new PacketReceiveProfile(packetId));
        }

        return prof;
    }
}
