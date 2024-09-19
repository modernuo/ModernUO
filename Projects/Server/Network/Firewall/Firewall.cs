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
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Network;

public static class Firewall
{
    private static InternalValidationEntry _validationEntry;
    private static readonly Dictionary<IPAddress, bool> _isBlockedCache = new();

    private static readonly SortedSet<IFirewallEntry> _firewallSet = new();

    public static SortedSet<IFirewallEntry> FirewallSet => _firewallSet;

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
    public static bool Add(IFirewallEntry firewallEntry)
    {
        if (_firewallSet.Add(firewallEntry))
        {
            _isBlockedCache.Clear();
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove(IFirewallEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (_firewallSet.Remove(entry))
        {
            _isBlockedCache.Clear();
            return true;
        }

        return false;
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
