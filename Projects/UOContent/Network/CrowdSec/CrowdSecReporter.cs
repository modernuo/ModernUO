/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecReporter.cs                                             *
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
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Network.Bans.CrowdSec;

/// <summary>
/// Contributes locally-decided bans to CrowdSec via the LAPI alerts API. Non-blocking on the accept
/// path: <see cref="Report"/> enqueues onto a bounded, drop-on-overflow channel drained by a single
/// background task that coalesces by IP and POSTs batched alerts.
/// </summary>
public sealed class CrowdSecReporter : IBanReporter
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(CrowdSecReporter));

    internal readonly record struct ReportItem(IPAddress Ip, TimeSpan Ttl, string Reason, bool Retract);

    // Bounded retry for transient LAPI failures during a drain send (network blips, 5xx). Distinct from
    // CrowdSecAlertClient.SendWithRetryAsync's single 401-relogin retry, which is an auth concern.
    private static readonly TimeSpan[] _retryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)];

    private ICrowdSecAlertClient _client;
    private CrowdSecSettings _settings;
    private Channel<ReportItem> _queue;
    private CancellationTokenSource _cts;
    private Task _drainTask;
    private int _dropped;
    private int _sendFailures;

    public CrowdSecReporter()
    {
    }

    // Test/embedding ctor with an injected client + settings.
    internal CrowdSecReporter(ICrowdSecAlertClient client, CrowdSecSettings settings)
    {
        _client = client;
        _settings = settings;
        _queue = CreateQueue(settings.MaxQueue);
    }

    public string Name => "crowdsec";
    public bool CanRetract => true;
    public int DroppedCount => _dropped;

    /// <summary>
    /// Batches ultimately dropped after the bounded transient-retry in <see cref="DrainLoop"/> gave up.
    /// Distinct from <see cref="DroppedCount"/> (queue-overflow drops on the accept path): this counts
    /// sustained LAPI outages so operators can see contribution loss instead of it being silent.
    /// </summary>
    public int SendFailureCount => _sendFailures;

    /// <summary>The drain loop's task, so tests can assert it stays alive until the loop exits.</summary>
    internal Task DrainTaskForTesting => _drainTask;

    public static void Configure()
    {
        BanChannel.Register(new CrowdSecReporter());
    }

    public void Register()
    {
        CrowdSecConfiguration.Load();
        _settings ??= CrowdSecConfiguration.Settings;
    }

    public void Start(CancellationToken token)
    {
        if (!_settings.ReportingEnabled)
        {
            logger.Information("CrowdSec reporter disabled (machineId/password empty in crowdsec.json)");
            return;
        }

        _client ??= new CrowdSecAlertClient(_settings);
        _queue ??= CreateQueue(_settings.MaxQueue);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _drainTask = Task.Run(() => DrainLoop(_cts.Token), _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();

        // The flush below reads a SingleReader channel, so wait for the drain to actually exit first.
        var drainExited = true;
        try
        {
            // Wait(timeout) is false only on timeout; a throw means faulted/cancelled, which is still exited.
            drainExited = _drainTask == null || _drainTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignored: a faulted wait means the drain has completed and released the channel.
        }

        _cts?.Dispose();
        _cts = null;

        if (drainExited)
        {
            FlushRemainingOnStop();
        }

        _client?.Dispose();
        _drainTask = null;
    }

    /// <summary>
    /// Best-effort bounded flush of whatever is still queued at shutdown. Blocking is correct here — the
    /// loop has stopped ticking — but must not happen on the loop thread: <see cref="Stop"/> runs where
    /// <c>SynchronizationContext.Current</c> is the <c>EventLoopContext</c>, and a captured continuation
    /// would be posted to a queue nothing pumps any more. <see cref="Task.Run(Func{Task})"/> keeps the
    /// chain on the pool; the bounded wait caps a wedged send at a few seconds of shutdown.
    /// </summary>
    private void FlushRemainingOnStop()
    {
        if (_queue == null || _client == null)
        {
            return;
        }

        _queue.Writer.TryComplete();

        List<ReportItem> reports = [];
        List<ReportItem> retracts = [];
        while (_queue.Reader.TryRead(out var item))
        {
            (item.Retract ? retracts : reports).Add(item);
        }

        if (reports.Count == 0 && retracts.Count == 0)
        {
            return;
        }

        try
        {
            if (!Task.Run(() => FlushRemainingOnStopAsync(reports, retracts)).Wait(TimeSpan.FromSeconds(4)))
            {
                logger.Warning(
                    "CrowdSec flush-on-stop timed out; {Count} item(s) not contributed",
                    reports.Count + retracts.Count
                );
            }
        }
        catch (Exception e)
        {
            logger.Warning(e, "CrowdSec flush-on-stop failed");
        }
    }

    /// <summary>
    /// Uses a fresh token, not the drain loop's already-cancelled one, which would fail every send
    /// immediately. Reports go as one deduped batch; retracts go as individual DELETEs so an admin's
    /// unban propagates on a clean shutdown. Leftovers self-heal via
    /// <see cref="CrowdSecSettings.ManualBanDuration"/>.
    /// </summary>
    private async Task FlushRemainingOnStopAsync(List<ReportItem> reports, List<ReportItem> retracts)
    {
        using var flushCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        if (reports.Count > 0)
        {
            var alerts = BuildAlerts(reports, _settings, DateTime.UtcNow);
            try
            {
                await _client.PostAlertsAsync(alerts, flushCts.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Warning(e, "CrowdSec flush-on-stop reports failed");
                RecordSendFailure(alerts.Count);
            }
        }

        HashSet<string> seen = [];
        for (var i = 0; i < retracts.Count; i++)
        {
            if (flushCts.IsCancellationRequested)
            {
                break; // out of budget; the rest self-heal via ManualBanDuration
            }

            var ip = retracts[i].Ip;
            if (!seen.Add(ip.ToString()))
            {
                continue;
            }

            try
            {
                await _client.DeleteDecisionsAsync(_settings.Origin, ip, flushCts.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.Warning(e, "CrowdSec flush-on-stop retract failed for {Address}", ip);
                RecordSendFailure(1);
            }
        }
    }

    public void Report(IPAddress address, TimeSpan ttl, string reason) =>
        Enqueue(new ReportItem(address, ttl, reason, false));

    public void Retract(IPAddress address) =>
        Enqueue(new ReportItem(address, TimeSpan.Zero, "retract", true));

    private void Enqueue(ReportItem item)
    {
        if (_queue == null || !_queue.Writer.TryWrite(item))
        {
            Interlocked.Increment(ref _dropped);
        }
    }

    // FullMode.Wait (the default) makes TryWrite return false immediately when the channel is full
    // instead of blocking the caller — exactly the non-blocking drop-on-overflow behavior the accept
    // path requires. DropWrite would silently discard the new item and always report success, which
    // would make overflow undetectable.
    private static Channel<ReportItem> CreateQueue(int capacity) =>
        Channel.CreateBounded<ReportItem>(new BoundedChannelOptions(Math.Max(1, capacity))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    // Must return Task: Start() passes this to Task.Run, which has no Func<ValueTask> overload, so a
    // ValueTask would bind to Task.Run<TResult> and yield a Task<ValueTask> that completes at the first
    // await rather than when the loop exits.
    private async Task DrainLoop(CancellationToken token)
    {
        var reader = _queue.Reader;

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    return;
                }

                // Coalesce a burst before flushing.
                await Task.Delay(_settings.FlushInterval, token).ConfigureAwait(false);

                List<ReportItem> reports = [];
                List<ReportItem> retracts = [];
                while (reader.TryRead(out var item))
                {
                    (item.Retract ? retracts : reports).Add(item);
                }

                if (reports.Count > 0)
                {
                    var alerts = BuildAlerts(reports, _settings, DateTime.UtcNow);
                    if (!await SendWithBoundedRetryAsync(() => _client.PostAlertsAsync(alerts, token), token)
                            .ConfigureAwait(false))
                    {
                        RecordSendFailure(alerts.Count);
                    }
                }

                for (var i = 0; i < retracts.Count; i++)
                {
                    var ip = retracts[i].Ip;
                    if (!await SendWithBoundedRetryAsync(() => _client.DeleteDecisionsAsync(_settings.Origin, ip, token), token)
                            .ConfigureAwait(false))
                    {
                        RecordSendFailure(1);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                // Contribution is auxiliary: log and keep draining. Never crash the shard.
                logger.Warning(e, "CrowdSec contribution flush failed; dropped this batch");
            }
        }
    }

    /// <summary>
    /// Up to 3 attempts (1 initial + 2 retries) backing off 1s then 2s, for transient LAPI failures
    /// (network blips, 5xx). Backoff uses <see cref="Task.Delay"/> so it never blocks the thread, and a
    /// cancellation during it propagates so the drain loop exits cleanly. Returns false rather than
    /// throwing once attempts are exhausted, so the caller counts the drop and keeps draining.
    /// </summary>
    private static async ValueTask<bool> SendWithBoundedRetryAsync(Func<ValueTask> send, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await send().ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (attempt >= _retryDelays.Length)
                {
                    logger.Warning(e, "CrowdSec send failed after {Attempts} attempt(s); giving up", attempt + 1);
                    return false;
                }

                var delay = _retryDelays[attempt];
                logger.Warning(e, "CrowdSec send failed (attempt {Attempt}); retrying in {Delay}", attempt + 1, delay);
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
        }
    }

    private void RecordSendFailure(int itemCount)
    {
        var total = Interlocked.Increment(ref _sendFailures);
        logger.Warning(
            "CrowdSec contribution batch dropped after retries ({Items} item(s)); total dropped batches: {Total}",
            itemCount,
            total
        );
    }

    /// <summary>Coalesces items by IP (last write wins) and builds one alert per unique address.</summary>
    internal static List<CrowdSecAlert> BuildAlerts(IEnumerable<ReportItem> items, CrowdSecSettings settings, DateTime nowUtc)
    {
        Dictionary<string, ReportItem> byIp = [];
        foreach (var item in items)
        {
            byIp[item.Ip.ToString()] = item;
        }

        var timestamp = FormatTimestamp(nowUtc);
        var alerts = new List<CrowdSecAlert>(byIp.Count);

        foreach (var (value, item) in byIp)
        {
            var ttl = item.Reason == BanReasons.Manual || item.Ttl <= TimeSpan.Zero ? settings.ManualBanDuration : item.Ttl;
            var scenario = $"{settings.Origin}/{item.Reason}";

            alerts.Add(new CrowdSecAlert
            {
                Scenario = scenario,
                Message = $"ModernUO {item.Reason} ban for {value}",
                StartAt = timestamp,
                StopAt = timestamp,
                Source = new CrowdSecSource { Scope = "Ip", Value = value },
                Decisions =
                [
                    new CrowdSecDecisionDto
                    {
                        Origin = settings.Origin,
                        Type = "ban",
                        Scope = "Ip",
                        Value = value,
                        Duration = FormatDuration(ttl),
                        Scenario = scenario
                    }
                ]
            });
        }

        return alerts;
    }

    /// <summary>
    /// ISO8601/RFC3339 UTC timestamp for <c>start_at</c>/<c>stop_at</c>. LAPI parses these with Go's
    /// <c>time.RFC3339</c> and answers 500 when the parse fails, so the format must be culture-independent:
    /// ':' is the *time separator* specifier in a custom .NET format string, and a shard running under a
    /// culture like fi-FI would otherwise emit "T12.34.56.789Z". InvariantCulture also pins the Gregorian
    /// calendar, which non-Gregorian cultures (th-TH, ar-SA) would otherwise shift the year for.
    /// </summary>
    internal static string FormatTimestamp(DateTime time) =>
        (time.Kind == DateTimeKind.Utc ? time : time.ToUniversalTime())
        .ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    /// <summary>CrowdSec accepts Go durations; whole seconds are unambiguous and sufficient.</summary>
    internal static string FormatDuration(TimeSpan ttl)
    {
        var seconds = (long)ttl.TotalSeconds;
        return $"{Math.Max(1, seconds)}s";
    }
}
