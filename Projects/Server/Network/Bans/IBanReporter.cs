/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IBanReporter.cs                                                 *
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
using System.Net;
using System.Threading;

namespace Server.Network.Bans;

/// <summary>
/// A contribution sink behind <see cref="BanChannel"/>. Reporters receive locally-decided bans
/// (manual admin bans, rate-limit trips) and forward them to an external system (e.g. CrowdSec),
/// which distributes them to OS-level bouncers. Reporters never answer the accept-path membership
/// query — enforcement is the local <see cref="Firewall"/>'s job.
/// </summary>
public interface IBanReporter
{
    /// <summary>Stable id for logging/config (e.g. <c>crowdsec</c>).</summary>
    string Name { get; }

    /// <summary>Reads configuration. No network or file I/O here.</summary>
    void Configure();

    /// <summary>Starts background delivery. The token is cancelled on shutdown.</summary>
    void Start(CancellationToken token);

    /// <summary>Flushes and tears down background delivery.</summary>
    void Stop();

    /// <summary>
    /// Enqueues a ban contribution. MUST be non-blocking and safe on the accept path: it may only
    /// enqueue (bounded, drop-on-overflow) and never perform synchronous I/O.
    /// </summary>
    /// <param name="ttl"><see cref="TimeSpan.Zero"/> or negative = use the reporter's default duration.</param>
    /// <param name="reason">Short slug (<c>manual</c>, <c>rate-limit</c>) used as the scenario suffix.</param>
    void Report(IPAddress address, TimeSpan ttl, string reason);

    /// <summary>True if this reporter can retract a previously-reported ban.</summary>
    bool CanRetract { get; }

    /// <summary>Enqueues a retraction (e.g. a manual unban). No-op if unsupported.</summary>
    void Retract(IPAddress address);
}
