using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Network;

namespace Server.Accounting;

public static class AccountAttackLimiter
{
    public static bool Enabled;

    private static readonly Dictionary<IPAddress, InvalidAccountAccessLog> _table = [];

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("accountAttackLimiter.enable", true);
    }

    public static unsafe void Initialize()
    {
        if (!Enabled)
        {
            return;
        }

        IncomingPackets.RegisterThrottler(0x80, &Throttle);
        IncomingPackets.RegisterThrottler(0x91, &Throttle);
        IncomingPackets.RegisterThrottler(0xCF, &Throttle);
    }

    public static bool Throttle(int packetId, NetState ns)
    {
        if (FindAccessLog(ns, out var accessLog) && Core.Now < accessLog.LastAccessTime + ComputeThrottle(accessLog.Counts))
        {
            ns.Disconnect(null);
        }

        return false;
    }

    public static bool FindAccessLog(NetState ns, out InvalidAccountAccessLog log)
    {
        if (ns == null || !_table.TryGetValue(ns.Address, out log))
        {
            log = default;
            return false;
        }

        if (log.HasExpired)
        {
            _table.Remove(ns.Address);
            log = default;
            return false;
        }

        return true;
    }

    public static void RegisterInvalidAccess(NetState ns)
    {
        if (ns == null || !Enabled)
        {
            return;
        }

        if (!FindAccessLog(ns, out var accessLog))
        {
            _table[ns.Address] = accessLog = new InvalidAccountAccessLog();
        }

        accessLog.Counts += 1;
        accessLog.RefreshAccessTime();

        if (accessLog.Counts >= 3)
        {
            try
            {
                // TODO: Async sink with Serilog
                using var op = new StreamWriter("throttle.log", true);
                op.WriteLine(
                    "{0}\t{1}\t{2}",
                    Core.Now,
                    ns,
                    accessLog.Counts
                );
            }
            catch
            {
                // ignored
            }
        }
    }

    public static TimeSpan ComputeThrottle(int counts) =>
        counts switch
        {
            >= 15 => TimeSpan.FromMinutes(5.0),
            >= 10 => TimeSpan.FromMinutes(1.0),
            >= 5  => TimeSpan.FromSeconds(20.0),
            >= 3  => TimeSpan.FromSeconds(10.0),
            >= 1  => TimeSpan.FromSeconds(2.0),
            _     => TimeSpan.Zero
        };
}

public struct InvalidAccountAccessLog
{
    public InvalidAccountAccessLog()
    {
        RefreshAccessTime();
    }

    public DateTime LastAccessTime { get; set; }
    public int Counts { get; set; }

    public bool HasExpired => Core.Now >= LastAccessTime + TimeSpan.FromHours(1.0);

    public void RefreshAccessTime()
    {
        LastAccessTime = Core.Now;
    }
}
