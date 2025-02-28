/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Firewall.cs                                                     *
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server.Network;

public static class Firewall
{
    private static InternalValidationEntry _validationEntry;
    private static readonly ConcurrentDictionary<IPAddress, (bool IsBlocked, int Version)> _isBlockedCache = [];
    private static readonly ReaderWriterLockSlim _firewallLock = new(LockRecursionPolicy.NoRecursion);

    private static int _firewallVersion;
    private static readonly SortedSet<IFirewallEntry> _firewallSet = [];

    public static int FirewallSetCount => _firewallSet.Count;

    public static void ReadFirewallSet(Action<IReadOnlySet<IFirewallEntry>> callback)
    {
        _firewallLock.EnterReadLock();
        try
        {
            callback(_firewallSet);
        }
        finally
        {
            _firewallLock.ExitReadLock();
        }
    }

    internal static bool IsBlocked(IPAddress address)
    {
        if (_isBlockedCache.TryGetValue(address, out var cacheEntry) && cacheEntry.Version == _firewallVersion)
        {
            return cacheEntry.IsBlocked;
        }

        if (_validationEntry == null)
        {
            _validationEntry = new InternalValidationEntry(address);
        }
        else
        {
            _validationEntry.Address = address;
        }

        if (CheckBlocked())
        {
            _isBlockedCache[address] = (true, _firewallVersion);
            return true;
        }

        return false;
    }

    private static bool CheckBlocked()
    {
        if (_firewallSet.Count == 0)
        {
            return false;
        }

        _firewallLock.EnterReadLock();
        try
        {
            var min = _firewallSet.Min;
            if (min!.MinIpAddress <= _validationEntry.MinIpAddress && min.MaxIpAddress >= _validationEntry.MaxIpAddress)
            {
                return true;
            }

            // Get all entries that are lower than our validation entry
            var view = _firewallSet.GetViewBetween(min, _validationEntry);

            // Loop backward since there shouldn't be any entries where the Min address is higher than ours
            foreach (var firewallEntry in view.Reverse())
            {
                if (firewallEntry.IsBlocked(_validationEntry.MinIpAddress))
                {
                    return true;
                }
            }

            return view.Max?.IsBlocked(_validationEntry.MinIpAddress) == true;
        }
        finally
        {
            _firewallLock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Add(IFirewallEntry firewallEntry)
    {
        _firewallLock.EnterWriteLock();
        try
        {
            if (_firewallSet.Add(firewallEntry))
            {
                Interlocked.Increment(ref _firewallVersion); // Update version
                return true;
            }
            return false;
        }
        finally
        {
            _firewallLock.ExitWriteLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove(IFirewallEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        _firewallLock.EnterWriteLock();
        try
        {
            if (_firewallSet.Remove(entry))
            {
                Interlocked.Increment(ref _firewallVersion); // Update version
                return true;
            }
            return false;
        }
        finally
        {
            _firewallLock.ExitWriteLock();
        }
    }

    private class InternalValidationEntry : BaseFirewallEntry
    {
        private UInt128 _address;

        public IPAddress Address
        {
            set => _address = value.ToUInt128();
        }

        public override UInt128 MinIpAddress => _address;
        public override UInt128 MaxIpAddress => _address;

        public InternalValidationEntry(IPAddress ipAddress) => Address = ipAddress;

        public InternalValidationEntry(UInt128 ipAddress) => _address = ipAddress;
    }
}
