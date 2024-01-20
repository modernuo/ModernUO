/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server.Network;

public static class Firewall
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Firewall));

    private static InternalValidationEntry _validationEntry;
    private static readonly Dictionary<IPAddress, bool> _isBlockedCache = new();

    private static readonly ConcurrentQueue<(IFirewallEntry FirewallyEntry, bool Remove)> _firewallQueue = new();
    private static readonly SortedSet<IFirewallEntry> _firewallSet = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFirewallEntry RequestAddSingleIPEntry(string entry)
    {
        try
        {
            var firewallEntry = new SingleIpFirewallEntry(entry);
            _firewallQueue.Enqueue((firewallEntry, false));
            return firewallEntry;
        }
        catch (Exception e)
        {
            logger.Warning(e, "Failed to add firewall entry: {Pattern}", entry);
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFirewallEntry RequestAddCIDREntry(string entry)
    {
        try
        {
            var firewallEntry = new CidrFirewallEntry(entry);
            _firewallQueue.Enqueue((firewallEntry, false));
            return firewallEntry;
        }
        catch (Exception e)
        {
            logger.Warning(e, "Failed to add firewall entry: {Pattern}", entry);
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequestAddEntry(IFirewallEntry entry)
    {
        _firewallQueue.Enqueue((entry, false));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequestRemoveEntry(IFirewallEntry entry)
    {
        _firewallQueue.Enqueue((entry, true));
    }

    internal static void ProcessQueue()
    {
        while (_firewallQueue.TryDequeue(out var entry))
        {
            if (entry.Remove)
            {
                RemoveEntry(entry.FirewallyEntry);
            }
            else
            {
                AddEntry(entry.FirewallyEntry);
            }
        }
    }

    internal static bool IsBlocked(IPAddress address)
    {
        ref var isBlocked = ref CollectionsMarshal.GetValueRefOrAddDefault(_isBlockedCache, address, out var exists);
        if (exists)
        {
            return isBlocked;
        }

        if (_validationEntry == null)
        {
            _validationEntry = new InternalValidationEntry(address);
        }
        else
        {
            _validationEntry.Address = address;
        }

        // Get all entries that are lower than our validation entry
        var view = _firewallSet.GetViewBetween(_firewallSet.Min, _validationEntry);

        // Loop backward since there shouldn't be any entries where the Min address is higher than ours
        foreach (var firewallEntry in view.Reverse())
        {
            if (firewallEntry.IsBlocked(_validationEntry.MinIpAddress))
            {
                isBlocked = true;
                return true;
            }
        }

        isBlocked = view.Max?.IsBlocked(_validationEntry.MinIpAddress) == true;

        return isBlocked;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddEntry(IFirewallEntry firewallEntry)
    {
        _firewallSet.Add(firewallEntry);
        _isBlockedCache.Clear();
    }

    private static void RemoveEntry(IFirewallEntry entry)
    {
        if (entry != null)
        {
            _firewallSet.Remove(entry);
            _isBlockedCache.Clear();
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
    }
}
