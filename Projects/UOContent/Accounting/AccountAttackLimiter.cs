using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Network;

namespace Server.Accounting
{
    public class AccountAttackLimiter
    {
        public static bool Enabled;

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("accountAttackLimiter.enable", true);
        }

        private static readonly List<InvalidAccountAccessLog> m_List = new List<InvalidAccountAccessLog>();

        public static void Initialize()
        {
            if (!Enabled)
                return;

            PacketHandlers.RegisterThrottler(0x80, Throttle_Callback);
            PacketHandlers.RegisterThrottler(0x91, Throttle_Callback);
            PacketHandlers.RegisterThrottler(0xCF, Throttle_Callback);
        }

        public static TimeSpan Throttle_Callback(NetState ns)
        {
            InvalidAccountAccessLog accessLog = FindAccessLog(ns);

            if (accessLog == null)
                return TimeSpan.Zero;

            DateTime date = DateTime.UtcNow;
            DateTime access = accessLog.LastAccessTime + ComputeThrottle(accessLog.Counts);
            return date >= access ? TimeSpan.Zero : date - access;
        }

        public static InvalidAccountAccessLog FindAccessLog(NetState ns)
        {
            if (ns == null)
                return null;

            IPAddress ipAddress = ns.Address;

            for (int i = 0; i < m_List.Count; ++i)
            {
                InvalidAccountAccessLog accessLog = m_List[i];

                if (accessLog.HasExpired)
                    m_List.RemoveAt(i--);
                else if (accessLog.Address.Equals(ipAddress))
                    return accessLog;
            }

            return null;
        }

        public static void RegisterInvalidAccess(NetState ns)
        {
            if (ns == null || !Enabled)
                return;

            InvalidAccountAccessLog accessLog = FindAccessLog(ns);

            if (accessLog == null)
                m_List.Add(accessLog = new InvalidAccountAccessLog(ns.Address));

            accessLog.Counts += 1;
            accessLog.RefreshAccessTime();

            if (accessLog.Counts >= 3)
                try
                {
                    using StreamWriter op = new StreamWriter("throttle.log", true);
                    op.WriteLine(
                        "{0}\t{1}\t{2}",
                        DateTime.UtcNow,
                        ns,
                        accessLog.Counts);
                }
                catch
                {
                    // ignored
                }
        }

        public static TimeSpan ComputeThrottle(int counts)
        {
            if (counts >= 15)
                return TimeSpan.FromMinutes(5.0);

            if (counts >= 10)
                return TimeSpan.FromMinutes(1.0);

            if (counts >= 5)
                return TimeSpan.FromSeconds(20.0);

            if (counts >= 3)
                return TimeSpan.FromSeconds(10.0);

            if (counts >= 1)
                return TimeSpan.FromSeconds(2.0);

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

        public bool HasExpired => DateTime.UtcNow >= LastAccessTime + TimeSpan.FromHours(1.0);

        public int Counts { get; set; }

        public void RefreshAccessTime()
        {
            LastAccessTime = DateTime.UtcNow;
        }
    }
}
