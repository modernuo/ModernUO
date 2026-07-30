/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FirewallConnectionFilter.cs                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Net;
using System.Threading;

namespace Server.Network;

/// <summary>
/// Exposes the admin-curated <see cref="Firewall"/> set to the accept path as an
/// <see cref="IConnectionFilter"/>. The firewall keeps its own API (the admin gump and commands mutate
/// it directly); this is only the accept-path adapter, since a static class cannot implement an
/// interface. It is the cheapest gate to consult — an empty set costs one length compare — so
/// <see cref="Firewall.Configure"/> registers it first.
/// </summary>
internal sealed class FirewallConnectionFilter : IConnectionFilter
{
    public static readonly FirewallConnectionFilter Instance = new();

    private FirewallConnectionFilter()
    {
    }

    public string Name => "firewall";

    // Firewall.Configure() owns loading and registration.
    public void Register()
    {
    }

    // Nothing to hydrate: the set loads at Configure and is maintained by a main-loop timer.
    public void Start(CancellationToken token)
    {
    }

    /// <summary>Flushes pending writes on the way down so a TTL expiry or late admin edit is not lost.</summary>
    public void Stop() => Firewall.Save();

    public bool ShouldDeny(IPAddress address) => Firewall.IsBlocked(address);
}
