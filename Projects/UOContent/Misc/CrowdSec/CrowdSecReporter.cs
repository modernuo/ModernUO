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

    public void Configure()
    {
        CrowdSecConfiguration.Configure();
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

        // Main.HandleClosed cancels ClosingTokenSource BEFORE calling BanChannel.Stop(), so by the time
        // we get here the drain loop has almost always already observed cancellation and is unwinding.
        // Wait a short bounded grace period for it to actually EXIT before we touch the SingleReader channel
        // ourselves — flushing while the drain is still a live reader would be unsafe.
        var drainExited = true;
        try
        {
            // Task.Wait(timeout) returns false only on timeout (task still running); true when completed;
            // throws when the task faulted/cancelled (also completed). So drainExited is false only while
            // the drain is genuinely still alive.
            drainExited = _drainTask == null || _drainTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // A faulted/cancelled wait means the drain task has completed — it is no longer reading the
            // channel, so the flush below is safe.
        }

        _cts?.Dispose();
        _cts = null;

        // Only read the SingleReader channel once the drain has provably stopped reading it.
        if (drainExited)
        {
            FlushRemainingOnStop();
        }

        _client?.Dispose();
        _drainTask = null;
    }

    /// <summary>
    /// Best-effort bounded flush of whatever contribution items are still queued at shutdown. Runs on a
    /// fresh, short-lived token — NOT the drain loop's (already-cancelled) token — since a cancelled token
    /// would make the send fail immediately. Pending reports are deduped via <see cref="BuildAlerts"/> and
    /// sent as a single batch; pending retracts are issued as individual (deduped) DELETEs so an admin's
    /// explicit unban still propagates on a clean shutdown instead of lingering until
    /// <see cref="CrowdSecSettings.ManualBanDuration"/> elapses. One shared short budget bounds the whole
    /// flush: a shutdown must never hang on this, and any leftover retracts still self-heal via that duration.
    /// </summary>
    private void FlushRemainingOnStop()
    {
        if (_queue == null || _client == null)
        {
            return;
        }

        _queue.Writer.TryComplete();

        var reports = new List<ReportItem>();
        var retracts = new List<ReportItem>();
        while (_queue.Reader.TryRead(out var item))
        {
            (item.Retract ? retracts : reports).Add(item);
        }

        if (reports.Count == 0 && retracts.Count == 0)
        {
            return;
        }

        // One shared, short budget bounds the entire flush so shutdown can never hang on it.
        using var flushCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        if (reports.Count > 0)
        {
            var alerts = BuildAlerts(reports, _settings, DateTime.UtcNow);
            try
            {
                _client.PostAlertsAsync(alerts, flushCts.Token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                logger.Warning(e, "CrowdSec flush-on-stop reports failed");
                RecordSendFailure(alerts.Count);
            }
        }

        var seen = new HashSet<string>();
        foreach (var retract in retracts)
        {
            if (flushCts.IsCancellationRequested)
            {
                break; // out of budget; the rest self-heal via ManualBanDuration
            }

            if (!seen.Add(retract.Ip.ToString()))
            {
                continue;
            }

            try
            {
                _client.DeleteDecisionsAsync(_settings.Origin, retract.Ip, flushCts.Token).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                logger.Warning(e, "CrowdSec flush-on-stop retract failed for {Address}", retract.Ip);
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

    private async ValueTask DrainLoop(CancellationToken token)
    {
        var reader = _queue.Reader;

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(token))
                {
                    return;
                }

                // Coalesce a burst before flushing.
                await Task.Delay(_settings.FlushInterval, token);

                var reports = new List<ReportItem>();
                var retracts = new List<ReportItem>();
                while (reader.TryRead(out var item))
                {
                    (item.Retract ? retracts : reports).Add(item);
                }

                if (reports.Count > 0)
                {
                    var alerts = BuildAlerts(reports, _settings, DateTime.UtcNow);
                    if (!await SendWithBoundedRetryAsync(() => _client.PostAlertsAsync(alerts, token), token))
                    {
                        RecordSendFailure(alerts.Count);
                    }
                }

                foreach (var retract in retracts)
                {
                    var ip = retract.Ip;
                    if (!await SendWithBoundedRetryAsync(() => _client.DeleteDecisionsAsync(_settings.Origin, ip, token), token))
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
    /// Sends with up to 3 attempts total (1 initial + 2 retries), backing off 1s then 2s between
    /// attempts, for transient LAPI failures (network blips, 5xx). Backoff uses <see cref="Task.Delay"/>
    /// so it never blocks the thread; a cancellation during backoff propagates as
    /// <see cref="OperationCanceledException"/> so the drain loop exits cleanly. Returns false (never
    /// throws for a send failure) once attempts are exhausted, so the caller can count the drop and keep
    /// draining instead of losing the rest of the batch/queue.
    /// </summary>
    private static async ValueTask<bool> SendWithBoundedRetryAsync(Func<ValueTask> send, CancellationToken token)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await send();
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
                await Task.Delay(delay, token);
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
        var byIp = new Dictionary<string, ReportItem>();
        foreach (var item in items)
        {
            byIp[item.Ip.ToString()] = item;
        }

        var timestamp = nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var alerts = new List<CrowdSecAlert>(byIp.Count);

        foreach (var (value, item) in byIp)
        {
            var ttl = item.Reason == "manual" || item.Ttl <= TimeSpan.Zero ? settings.ManualBanDuration : item.Ttl;
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

    /// <summary>CrowdSec accepts Go durations; whole seconds are unambiguous and sufficient.</summary>
    internal static string FormatDuration(TimeSpan ttl)
    {
        var seconds = (long)ttl.TotalSeconds;
        return $"{Math.Max(1, seconds)}s";
    }
}
