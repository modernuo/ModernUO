/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ConnectionFilters.cs                                            *
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
using System.Threading;
using Server.Logging;

namespace Server.Network;

/// <summary>
/// Registry of the <see cref="IConnectionFilter"/> gates the accept path consults, and their lifecycle.
/// Filters are registered during the Configure sweep and the backing store is a plain array, so
/// <see cref="ShouldDeny"/> is an indexed loop over a field read — no enumerator, no closure, no
/// allocation. The whole accept path runs on the game loop, so no synchronization is needed.
/// </summary>
public static class ConnectionFilters
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ConnectionFilters));

    private static IConnectionFilter[] _filters = [];

    public static IReadOnlyList<IConnectionFilter> Filters => _filters;

    /// <summary>
    /// Registers a gate (inversion of control, mirroring <c>BanChannel.Register</c>). Idempotent by
    /// <see cref="IConnectionFilter.Name"/>. Filters are consulted in registration order, so register
    /// the cheapest and most selective first — core registers the firewall before content is swept.
    /// </summary>
    public static void Register(IConnectionFilter filter)
    {
        if (filter == null)
        {
            return;
        }

        foreach (var existing in _filters)
        {
            if (existing.Name == filter.Name)
            {
                return;
            }
        }

        filter.Register();

        var updated = new IConnectionFilter[_filters.Length + 1];
        Array.Copy(_filters, updated, _filters.Length);
        updated[^1] = filter;
        _filters = updated;

        logger.Information("Registered connection filter '{Name}'", filter.Name);
    }

    /// <summary>
    /// True when any filter denies the connection. Short-circuits on the first denial;
    /// <paramref name="deniedBy"/> names it for logging.
    /// </summary>
    public static bool ShouldDeny(IPAddress address, out string deniedBy)
    {
        var filters = _filters;
        for (var i = 0; i < filters.Length; i++)
        {
            // Try/catch costs nothing when nothing throws, and this is the one path where a faulty
            // third-party filter would otherwise take down the accept loop for every connection.
            try
            {
                if (filters[i].ShouldDeny(address))
                {
                    deniedBy = filters[i].Name;
                    return true;
                }
            }
            catch (Exception e)
            {
                Disable(filters[i], e);
            }
        }

        deniedBy = null;
        return false;
    }

    /// <summary>
    /// Drops a filter that threw on the accept path. A filter that faults once will fault for every
    /// subsequent connection, so leaving it registered means an exception and a log line per accept —
    /// exactly the amplification an attacker wants. Failing open here is deliberate: a broken filter
    /// must not be able to deny every connection either.
    /// </summary>
    private static void Disable(IConnectionFilter filter, Exception e)
    {
        logger.Error(e, "Connection filter '{Name}' threw on the accept path; unregistering it", filter.Name);

        var updated = new List<IConnectionFilter>(_filters.Length);
        foreach (var existing in _filters)
        {
            if (!ReferenceEquals(existing, filter))
            {
                updated.Add(existing);
            }
        }

        _filters = updated.ToArray();
    }

    public static void Start(CancellationToken token)
    {
        foreach (var filter in _filters)
        {
            try
            {
                filter.Start(token);
            }
            catch (Exception e)
            {
                // A filter that cannot hydrate must not crash boot; it simply denies nothing.
                logger.Error(e, "Connection filter '{Name}' failed to start; continuing without it", filter.Name);
            }
        }
    }

    public static void Stop()
    {
        foreach (var filter in _filters)
        {
            try
            {
                filter.Stop();
            }
            catch (Exception e)
            {
                logger.Warning(e, "Connection filter '{Name}' threw while stopping", filter.Name);
            }
        }
    }

    internal static void ResetForTesting() => _filters = [];
}
