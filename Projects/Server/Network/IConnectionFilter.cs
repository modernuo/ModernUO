/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IConnectionFilter.cs                                            *
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
/// A gate consulted for every inbound connection, before the socket is configured and before any
/// per-connection allocation. Implementations decide membership only — the accept path neither knows
/// nor cares where a filter's data comes from, so a filter may be a handful of admin-curated entries,
/// a millions-strong list hydrated from a file, or a query against something else entirely. Core owns
/// the question; content owns every answer (see <c>Firewall</c> and <c>BlocklistFilter</c> in UOContent).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ShouldDeny"/> runs on the game loop once per accepted socket, which is the path that has
/// to survive a DDoS. Implementations MUST be allocation-free and O(log n) at worst, MUST NOT perform
/// I/O, and MUST NOT block. Anything expensive (parsing, reloading, reporting to an external service)
/// belongs off the loop or behind a bounded, non-blocking enqueue.
/// </para>
/// <para>
/// Side effects that a hit implies (contributing to <c>BanChannel</c>, promoting to an OS firewall,
/// suppressing duplicate reports) are the filter's own business, not the accept path's. This is why
/// <see cref="ShouldDeny"/> returns a bare bool: the accept path asks one question and does one thing.
/// </para>
/// </remarks>
public interface IConnectionFilter
{
    /// <summary>Stable id for logging/config (e.g. <c>firewall</c>, <c>blocklist</c>).</summary>
    string Name { get; }

    /// <summary>Reads configuration. Called by <see cref="ConnectionFilters.Register"/>. No I/O beyond config.</summary>
    void Register();

    /// <summary>Starts any background hydration. The token is cancelled on shutdown.</summary>
    void Start(CancellationToken token);

    /// <summary>Flushes and tears down. Called during shutdown.</summary>
    void Stop();

    /// <summary>
    /// True to deny the connection. Must be allocation-free and non-blocking; see the remarks on
    /// <see cref="IConnectionFilter"/>.
    /// </summary>
    bool ShouldDeny(IPAddress address);
}
