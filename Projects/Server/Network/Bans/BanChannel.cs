/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BanChannel.cs                                                   *
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

namespace Server.Network.Bans;

/// <summary>
/// Coordinates the configured <see cref="IBanReporter"/> contribution sinks. Enforcement is NOT here —
/// the accept path calls <see cref="Firewall.IsBlocked"/> directly. This channel only fans locally-decided
/// bans out to external systems (CrowdSec), which distribute them to OS-level bouncers.
/// </summary>
public static class BanChannel
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BanChannel));

    private static IBanReporter[] _reporters = [];

    public static IReadOnlyList<IBanReporter> Reporters => _reporters;

    public static void Configure()
    {
        // Load ban policy for the accept path. Reporters are NOT built here — content registers them
        // via Register() during the same Configure() sweep, in any order relative to this call, so we
        // must not clobber any registrations that may already have arrived.
        BanConfiguration.Configure();
    }

    /// <summary>
    /// Registers a contribution sink from content (inversion of control). Idempotent by
    /// <see cref="IBanReporter.Name"/>: a second registration of the same name is ignored. Configures the
    /// reporter immediately so it is ready before <see cref="Start"/>.
    /// </summary>
    public static void Register(IBanReporter reporter)
    {
        if (reporter == null)
        {
            return;
        }

        foreach (var existing in _reporters)
        {
            if (existing.Name == reporter.Name)
            {
                return;
            }
        }

        reporter.Configure();

        var updated = new IBanReporter[_reporters.Length + 1];
        Array.Copy(_reporters, updated, _reporters.Length);
        updated[^1] = reporter;
        _reporters = updated;

        logger.Information("Ban channel registered reporter '{Name}'", reporter.Name);
    }

    internal static void ConfigureForTesting(IBanReporter[] reporters) => _reporters = reporters ?? [];

    public static void Start(CancellationToken token)
    {
        foreach (var reporter in _reporters)
        {
            try
            {
                reporter.Start(token);
            }
            catch (Exception e)
            {
                // A broken contribution path must not crash boot — enforcement is local and unaffected.
                logger.Error(e, "Ban reporter '{Name}' failed to start; continuing without it", reporter.Name);
            }
        }
    }

    public static void Stop()
    {
        foreach (var reporter in _reporters)
        {
            try
            {
                reporter.Stop();
            }
            catch (Exception e)
            {
                logger.Warning(e, "Ban reporter '{Name}' threw while stopping", reporter.Name);
            }
        }

        // Flush the local firewall's pending writes on the way down.
        Firewall.Save();
    }

    /// <summary>Fans a locally-decided ban out to every reporter. Non-blocking; never throws.</summary>
    public static void Report(IPAddress ip, TimeSpan ttl, string reason)
    {
        var reporters = _reporters;
        for (var i = 0; i < reporters.Length; i++)
        {
            try
            {
                reporters[i].Report(ip, ttl, reason);
            }
            catch (Exception e)
            {
                logger.Warning(e, "Ban reporter '{Name}' threw during Report", reporters[i].Name);
            }
        }
    }

    /// <summary>Fans a retraction (manual unban) out to every retract-capable reporter.</summary>
    public static void Retract(IPAddress ip)
    {
        var reporters = _reporters;
        for (var i = 0; i < reporters.Length; i++)
        {
            if (!reporters[i].CanRetract)
            {
                continue;
            }

            try
            {
                reporters[i].Retract(ip);
            }
            catch (Exception e)
            {
                logger.Warning(e, "Ban reporter '{Name}' threw during Retract", reporters[i].Name);
            }
        }
    }
}
