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
using System.Runtime.InteropServices;

namespace Server.Misc;

public static class IPLimiter
{
    private static readonly Dictionary<IPAddress, int> _connectionAttempts = new(128);
    private static readonly HashSet<IPAddress> _throttledAddresses = new();

    private static long _lastClearedThrottles;
    private static long _lastClearedAttempts;

    public static readonly IPAddress[] Exemptions =
    {
        IPAddress.Parse( "127.0.0.1" )
    };

    public static TimeSpan ClearConnectionAttemptsDuration { get; private set; }

    public static TimeSpan ClearThrottledDuration { get; private set; }

    public static bool Enabled { get; private set; }
    public static int MaxAddresses { get; private set; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("ipLimiter.enable", true);
        MaxAddresses = ServerConfiguration.GetOrUpdateSetting("ipLimiter.maxConnectionsPerIP", 10);
        ClearConnectionAttemptsDuration = ServerConfiguration.GetOrUpdateSetting("ipLimiter.clearConnectionAttemptsDuration", TimeSpan.FromSeconds(10));
        ClearThrottledDuration = ServerConfiguration.GetOrUpdateSetting("ipLimiter.clearThrottledDuration", TimeSpan.FromMinutes(2));
    }

    public static bool IsExempt(IPAddress ip)
    {
        for (int i = 0; i < Exemptions.Length; i++)
        {
            if (ip.Equals(Exemptions[i]))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Verify(IPAddress ourAddress)
    {
        if (!Enabled || IsExempt(ourAddress))
        {
            return true;
        }

        var now = Core.TickCount;

        if (_throttledAddresses.Count > 0)
        {
            if (now - _lastClearedThrottles > ClearThrottledDuration.TotalMilliseconds)
            {
                _lastClearedThrottles = now;
                ClearThrottledAddresses();
            }
            else if (_throttledAddresses.Contains(ourAddress))
            {
                return false;
            }
        }

        if (_connectionAttempts.Count > 0 && now - _lastClearedAttempts > ClearConnectionAttemptsDuration.TotalMilliseconds)
        {
            _lastClearedAttempts = now;
            ClearConnectionAttempts();
        }

        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_connectionAttempts, ourAddress, out _);
        count++;

        if (count > MaxAddresses)
        {
            _connectionAttempts.Remove(ourAddress);
            _throttledAddresses.Add(ourAddress);
            return false;
        }

        return true;
    }

    private static void ClearThrottledAddresses()
    {
        _throttledAddresses.Clear();
    }

    private static void ClearConnectionAttempts()
    {
        _connectionAttempts.Clear();
        _connectionAttempts.TrimExcess(128);
    }
}
