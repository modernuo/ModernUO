using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Network;

namespace Server.Accounting
{
    public static class AccountAttackLimiter
    {
        public static bool Enabled;

        private static readonly List<InvalidAccountAccessLog> m_List = new();

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

        public static bool Throttle(int packetId, NetState ns, out bool drop)
        {
            var accessLog = FindAccessLog(ns);

            if (accessLog == null)
            {
                drop = false;
                return true;
            }

            var date = Core.Now;
            var access = accessLog.LastAccessTime + ComputeThrottle(accessLog.Counts);
            var allow = date >= access;
            drop = !allow;
            return allow;
        }

        public static InvalidAccountAccessLog FindAccessLog(NetState ns)
        {
            if (ns == null)
            {
                return null;
            }

            var ipAddress = ns.Address;

            for (var i = 0; i < m_List.Count; ++i)
            {
                var accessLog = m_List[i];

                if (accessLog.HasExpired)
                {
                    m_List.RemoveAt(i--);
                }
                else if (accessLog.Address.Equals(ipAddress))
                {
                    return accessLog;
                }
            }

            return null;
        }

        public static void RegisterInvalidAccess(NetState ns)
        {
            if (ns == null || !Enabled)
            {
                return;
            }

            var accessLog = FindAccessLog(ns);

            if (accessLog == null)
            {
                m_List.Add(accessLog = new InvalidAccountAccessLog(ns.Address));
            }

            accessLog.Counts += 1;
            accessLog.RefreshAccessTime();

            if (accessLog.Counts >= 3)
            {
                try
                {
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

        public static TimeSpan ComputeThrottle(int counts)
        {
            if (counts >= 15)
            {
                return TimeSpan.FromMinutes(5.0);
            }

            if (counts >= 10)
            {
                return TimeSpan.FromMinutes(1.0);
            }

            if (counts >= 5)
            {
                return TimeSpan.FromSeconds(20.0);
            }

            if (counts >= 3)
            {
                return TimeSpan.FromSeconds(10.0);
            }

            if (counts >= 1)
            {
                return TimeSpan.FromSeconds(2.0);
            }

            return TimeSpan.Zero;
        }
    }

    public class InvalidAccountAccessLog
    {
        public InvalidAccountAccessLog(IPAddress address)
        {
            Address = address;
            RefreshAccessTime();
        }

        public IPAddress Address { get; set; }

        public DateTime LastAccessTime { get; set; }

        public bool HasExpired => Core.Now >= LastAccessTime + TimeSpan.FromHours(1.0);

        public int Counts { get; set; }

        public void RefreshAccessTime()
        {
            LastAccessTime = Core.Now;
        }
    }
}
