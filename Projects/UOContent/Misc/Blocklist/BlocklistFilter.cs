/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistFilter.cs                                              *
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Network.Bans;

/// <summary>
/// Accept-path gate for a large, file-sourced IP blocklist, hydrated from the file a generator
/// (<c>tools/Export-IpBlocklist.ps1</c>) writes on a schedule. Holds an immutable snapshot swapped
/// atomically by an off-loop reload poll, so accept-path reads are lock-free. Inert when no file is
/// configured or present.
/// </summary>
/// <remarks>
/// This is the demand-paging half of the design: an OS firewall cannot hold millions of entries on
/// Windows, so the millions live here and only addresses that actually connect are promoted to CrowdSec
/// (and from there to the OS firewall) through <see cref="BanChannel"/>. <see cref="PromotedGuard"/>
/// keeps a flood of repeat connections from re-reporting the same address before the bouncer picks it up.
/// </remarks>
public sealed class BlocklistFilter : IConnectionFilter
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BlocklistFilter));

    // Written by the reload poll (off-loop), read by the accept path (game loop): a single volatile
    // reference swap is the whole synchronization story — readers see the old or the new snapshot, whole.
    private volatile BlocklistSnapshot _snapshot = BlocklistSnapshot.Empty;

    private readonly PromotedGuard _guard = new();

    private string _path;
    private TimeSpan _interval;
    private bool _reportHits;
    private TimeSpan _banDuration;
    private long _suppressionMs;
    private string _lastGenerated;
    private DateTime _lastWriteUtc;
    private CancellationTokenSource _cts;

    public string Name => "blocklist";

    public int Count => _snapshot.Count;

    public static void Configure()
    {
        ConnectionFilters.Register(new BlocklistFilter());
    }

    public void Register()
    {
        BlocklistConfiguration.Load();
        var s = BlocklistConfiguration.Settings;

        _path = ResolvePath(s.File);
        _interval = s.ReloadInterval <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : s.ReloadInterval;
        _reportHits = s.ReportHits;
        _banDuration = s.BanDuration;
        _suppressionMs = (long)s.PromoteSuppression.TotalMilliseconds;
    }

    /// <summary>
    /// Resolves the configured path once: a relative path is anchored to <see cref="Core.BaseDirectory"/>
    /// (never the process working directory, which differs when the shard is launched from elsewhere), an
    /// absolute path is taken as-is so several shards can share one generated list.
    /// </summary>
    private static string ResolvePath(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        return Path.IsPathRooted(configured) ? configured : Path.Join(Core.BaseDirectory, configured);
    }

    public void Start(CancellationToken token)
    {
        if (_path == null)
        {
            logger.Information("Blocklist disabled (\"file\" empty in blocklist.json)");
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        // A missing file is the shipped default, not an error: the gate stays inert until the poll picks
        // up whatever the generator first writes. No restart needed.
        if (File.Exists(_path))
        {
            Reload(); // synchronous prime; empty on failure (fail-open)
        }
        else
        {
            logger.Information("Blocklist inert: no list at \"{Path}\"; polling every {Interval}", _path, _interval);
        }

        // Sweep the promote-guard so a distinct-IP flood cannot grow it unbounded.
        Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), SweepGuard);

        _ = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public bool ShouldDeny(IPAddress address)
    {
        if (!Evaluate(address, Core.TickCount, out var shouldReport))
        {
            return false;
        }

        if (shouldReport)
        {
            // Demand-page this address up to the OS-level bouncer. Enqueue-only; never blocks the loop.
            BanChannel.Report(address, _banDuration, "blocklist");
        }

        return true;
    }

    /// <summary>
    /// The pure decision, split out so the accept-path policy can be tested without a clock or a ban
    /// channel. <paramref name="shouldReport"/> is true at most once per suppression window.
    /// </summary>
    internal bool Evaluate(IPAddress address, long nowTicks, out bool shouldReport)
    {
        shouldReport = false;

        if (!_snapshot.IsBanned(address))
        {
            return false;
        }

        if (_reportHits)
        {
            shouldReport = _guard.TryMark(address.ToUInt128(), nowTicks, _suppressionMs);
        }

        return true;
    }

    private void SweepGuard() => _guard.Sweep(Core.TickCount);

    // Test hook: inject a snapshot and policy without file I/O.
    internal void LoadForTesting(BlocklistSnapshot snapshot, bool reportHits = true, long suppressionMs = 60000)
    {
        _snapshot = snapshot;
        _reportHits = reportHits;
        _suppressionMs = suppressionMs;
    }

    private async ValueTask PollLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                if (ChangedSinceLastLoad())
                {
                    // Parsing millions of lines competes with a save for CPU and page cache, so yield until
                    // the world is written out. See the threading policy in CLAUDE.md (rules #3 and #10).
                    while (World.Saving || World.WorldState == WorldState.PendingSave)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }

                    Reload();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                logger.Warning(e, "Blocklist reload check failed; keeping last snapshot ({Count})", Count);
            }
        }
    }

    private bool ChangedSinceLastLoad()
    {
        try
        {
            var info = new FileInfo(_path);
            if (!info.Exists)
            {
                return false;
            }

            if (info.LastWriteTimeUtc == _lastWriteUtc)
            {
                return false; // cheapest guard
            }
        }
        catch
        {
            return false;
        }

        return !BlocklistFile.TryReadHeader(_path, out var h) || h.Generated != _lastGenerated;
    }

    private void Reload()
    {
        // Capture the mtime/header BEFORE Load() so the markers describe the version being parsed, not
        // one the producer swapped in mid-parse. Stale markers only cost an extra reload next poll;
        // capturing after could skip a version entirely.
        var writeUtc = default(DateTime);
        try
        {
            writeUtc = new FileInfo(_path).LastWriteTimeUtc;
        }
        catch
        {
            /* keep default */
        }

        BlocklistFile.TryReadHeader(_path, out var h);

        var next = BlocklistFile.Load(_path, out var parsed, out var skipped);
        _snapshot = next; // single volatile swap; readers see old or new whole
        _lastGenerated = h.Generated;
        _lastWriteUtc = writeUtc;

        logger.Information("Blocklist loaded {Parsed} entr(ies) ({Count} ranges, {Skipped} skipped) gen={Gen}",
            parsed, next.Count, skipped, h.Generated);
    }
}
