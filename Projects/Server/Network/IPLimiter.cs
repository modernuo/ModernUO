/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IPLimiter.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Net;

namespace Server.Misc;

public static class IPLimiter
{
    private static readonly SortedSet<IPAccessLog> _connectionAttempts = [];
    private static readonly SortedSet<IPAccessLog> _throttledAddresses = [];

    private static readonly IPAddress _localHost = IPAddress.Parse("127.0.0.1");

    public static TimeSpan ConnectionAttemptsDuration { get; private set; }
    public static TimeSpan ConnectionThrottleDuration { get; private set; }

    public static bool Enabled { get; private set; }
    public static int MaxConnections { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("ipLimiter.enable", true);
        MaxConnections = ServerConfiguration.GetOrUpdateSetting("ipLimiter.maxConnectionsPerIP", 5);
        ConnectionAttemptsDuration = ServerConfiguration.GetOrUpdateSetting("ipLimiter.clearConnectionAttemptsDuration", TimeSpan.FromSeconds(10));
        ConnectionThrottleDuration = ServerConfiguration.GetOrUpdateSetting("ipLimiter.connectionThrottleDuration", TimeSpan.FromMinutes(5));
    }

    private static readonly IPAccessLog _accessCheck = new(IPAddress.None, DateTime.MinValue);

    public static bool Verify(IPAddress ourAddress)
    {
        if (!Enabled || ourAddress.Equals(_localHost))
        {
            return true;
        }

        var now = Core.Now;

        IPAccessLog accessLog;

        while (_throttledAddresses.Count > 0)
        {
            accessLog = _throttledAddresses.Min;
            if (now <= accessLog.Expiration)
            {
                break;
            }

            _throttledAddresses.Remove(accessLog);
        }

        _accessCheck.IPAddress = ourAddress;

        if (_connectionAttempts.TryGetValue(_accessCheck, out accessLog))
        {
            _connectionAttempts.Remove(accessLog);
            accessLog.Count++;
            accessLog.Expiration = now + ConnectionAttemptsDuration;

            if (now <= accessLog.Expiration && accessLog.Count >= MaxConnections)
            {
                _throttledAddresses.Add(accessLog);
                return false;
            }
        }
        else
        {
            accessLog = new IPAccessLog(ourAddress, now + ConnectionAttemptsDuration);
        }

        // Add it back so it is sorted properly
        _connectionAttempts.Add(accessLog);

        return true;
    }

    private class IPAccessLog : IComparable<IPAccessLog>
    {
        public IPAddress IPAddress;
        public DateTime Expiration;
        public int Count;

        public IPAccessLog(IPAddress ipAddress, DateTime expiration)
        {
            IPAddress = ipAddress;
            Expiration = expiration;
            Count = 1;
        }

        public int CompareTo(IPAccessLog other) =>
            IPAddress.Equals(other.IPAddress) ? 0 : Expiration.CompareTo(other.Expiration);
    }
}
