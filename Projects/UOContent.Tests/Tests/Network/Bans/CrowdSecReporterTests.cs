/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecReporterTests.cs                                        *
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Network.Bans.CrowdSec;
using Xunit;

namespace Server.Tests.Network.Bans;

public class CrowdSecReporterTests
{
    private static CrowdSecSettings Settings() => new()
    {
        MachineId = "shard",
        Password = "secret",
        Origin = "modernuo",
        ManualBanDuration = TimeSpan.FromHours(168)
    };

    [Fact]
    public void BuildAlerts_DedupsByIp()
    {
        var now = DateTime.UnixEpoch;
        var items = new List<CrowdSecReporter.ReportItem>
        {
            new(IPAddress.Parse("1.1.1.1"), TimeSpan.FromHours(1), "rate-limit", false),
            new(IPAddress.Parse("1.1.1.1"), TimeSpan.FromHours(1), "rate-limit", false),
            new(IPAddress.Parse("2.2.2.2"), TimeSpan.FromHours(1), "rate-limit", false)
        };

        var alerts = CrowdSecReporter.BuildAlerts(items, Settings(), now);

        Assert.Equal(2, alerts.Count);
        Assert.All(alerts, a => Assert.Single(a.Decisions));
        Assert.Contains(alerts, a => a.Source.Value == "1.1.1.1");
        Assert.Contains(alerts, a => a.Source.Value == "2.2.2.2");
    }

    [Fact]
    public void BuildAlerts_ScenarioFromReason_OriginFromSettings()
    {
        var alerts = CrowdSecReporter.BuildAlerts(
            [new(IPAddress.Parse("3.3.3.3"), TimeSpan.FromHours(1), "manual", false)],
            Settings(),
            DateTime.UnixEpoch);

        var decision = Assert.Single(alerts).Decisions[0];
        Assert.Equal("modernuo/manual", alerts[0].Scenario);
        Assert.Equal("modernuo", decision.Origin);
        Assert.Equal("ban", decision.Type);
        Assert.Equal("Ip", decision.Scope);
        Assert.Equal("3.3.3.3", decision.Value);
    }

    [Fact]
    public void BuildAlerts_ScenarioFromReason_Blocklist()
    {
        var alerts = CrowdSecReporter.BuildAlerts(
            [new(IPAddress.Parse("5.5.5.5"), TimeSpan.FromHours(1), "blocklist", false)],
            Settings(),
            DateTime.UnixEpoch);

        var decision = Assert.Single(alerts).Decisions[0];
        Assert.Equal("modernuo/blocklist", alerts[0].Scenario);
        Assert.Equal("modernuo/blocklist", decision.Scenario);
    }

    /// <summary>
    /// LAPI dereferences scenario_hash/scenario_version unconditionally when persisting an alert, so an
    /// omitted field is a 500, not a validation error. Asserted on the serialized payload rather than the
    /// DTO because that is what actually goes on the wire.
    /// </summary>
    [Fact]
    public void BuildAlerts_SerializedPayload_CarriesRequiredScenarioFields()
    {
        var alerts = CrowdSecReporter.BuildAlerts(
            [new(IPAddress.Parse("9.9.9.9"), TimeSpan.FromHours(1), "rate-limit", false)],
            Settings(),
            DateTime.UnixEpoch);

        var payload = JsonSerializer.SerializeToNode(alerts)!.AsArray()[0]!.AsObject();

        Assert.True(payload.ContainsKey("scenario_hash"));
        Assert.True(payload.ContainsKey("scenario_version"));
        Assert.Equal(JsonValueKind.String, payload["scenario_hash"]!.GetValue<JsonElement>().ValueKind);
        Assert.Equal(JsonValueKind.String, payload["scenario_version"]!.GetValue<JsonElement>().ValueKind);
        Assert.Equal(1, payload["capacity"]!.GetValue<int>());
    }

    /// <summary>
    /// ':' is the time-separator specifier in a custom .NET format string, so a shard under fi-FI used to
    /// emit "T00.00.00.000Z" — which Go's time.RFC3339 rejects, and LAPI answers 500 for. th-TH additionally
    /// shifts the year via the Buddhist calendar.
    /// </summary>
    [Theory]
    [InlineData("fi-FI")]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    public void FormatTimestamp_IsIso8601_RegardlessOfCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            Assert.Equal("1970-01-01T00:00:00.000Z", CrowdSecReporter.FormatTimestamp(DateTime.UnixEpoch));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    /// <summary>A non-UTC input must still be stamped as UTC — the trailing 'Z' is a literal, not a claim.</summary>
    [Fact]
    public void FormatTimestamp_ConvertsNonUtcInput()
    {
        var local = new DateTimeOffset(1970, 1, 1, 2, 0, 0, TimeSpan.FromHours(2)).LocalDateTime;

        Assert.Equal("1970-01-01T00:00:00.000Z", CrowdSecReporter.FormatTimestamp(local));
    }

    [Fact]
    public void FormatDuration_UsesSeconds_FloorsAtOne()
    {
        Assert.Equal("3600s", CrowdSecReporter.FormatDuration(TimeSpan.FromHours(1)));
        Assert.Equal("1s", CrowdSecReporter.FormatDuration(TimeSpan.Zero));
        Assert.Equal("1s", CrowdSecReporter.FormatDuration(TimeSpan.FromMilliseconds(10)));
    }

    [Fact]
    public void Report_WhenQueueFull_DropsAndCounts()
    {
        var reporter = new CrowdSecReporter(new NullAlertClient(), new CrowdSecSettings
        {
            MachineId = "shard",
            Password = "secret",
            MaxQueue = 2
        });
        // Do NOT Start() the drain — so the queue fills and overflows deterministically.

        for (var i = 0; i < 10; i++)
        {
            reporter.Report(IPAddress.Parse("4.4.4." + i), TimeSpan.FromHours(1), "rate-limit");
        }

        Assert.True(reporter.DroppedCount >= 8);
    }

    // Stop() without a prior Start() drives FlushRemainingOnStop() synchronously (no drain task, no
    // Task.Delay backoff involved), so this is deterministic — no wall-clock timing dependency.
    [Fact]
    public void Stop_FlushesQueuedReports_ViaClient()
    {
        var client = new RecordingAlertClient();
        var reporter = new CrowdSecReporter(client, Settings());

        reporter.Report(IPAddress.Parse("6.6.6.6"), TimeSpan.FromHours(1), "rate-limit");
        reporter.Stop();

        var posted = Assert.Single(client.Posted);
        Assert.Equal("6.6.6.6", Assert.Single(posted).Source.Value);
        Assert.Equal(0, reporter.SendFailureCount);
    }

    [Fact]
    public void Stop_FlushesQueuedRetracts_ViaClient()
    {
        var client = new RecordingAlertClient();
        var reporter = new CrowdSecReporter(client, Settings());

        reporter.Retract(IPAddress.Parse("8.8.8.8"));
        reporter.Stop();

        Assert.Equal(IPAddress.Parse("8.8.8.8"), Assert.Single(client.Deleted));
        Assert.Equal(0, reporter.SendFailureCount);
    }

    [Fact]
    public void Stop_WhenFlushSendFails_CountsSendFailure()
    {
        var reporter = new CrowdSecReporter(new ThrowingAlertClient(), Settings());

        reporter.Report(IPAddress.Parse("7.7.7.7"), TimeSpan.FromHours(1), "rate-limit");
        reporter.Stop();

        Assert.Equal(1, reporter.SendFailureCount);
    }

    [Fact]
    public void Stop_WithEmptyQueue_DoesNotInvokeClientOrFail()
    {
        var client = new RecordingAlertClient();
        var reporter = new CrowdSecReporter(client, Settings());

        reporter.Stop();

        Assert.Empty(client.Posted);
        Assert.Equal(0, reporter.SendFailureCount);
    }

    /// <summary>
    /// The drain task must track the loop's lifetime, not just its first await — a ValueTask-returning
    /// drain loop passed to Task.Run yields a Task&lt;ValueTask&gt; that completes immediately, which makes
    /// Stop()'s drain-exited handshake a no-op. With an empty queue the loop parks on WaitToReadAsync,
    /// so a correctly unwrapped task cannot win this race; a slow pool only under-detects.
    /// </summary>
    [Fact]
    public async Task Start_DrainTaskSpansLoopLifetime_NotJustTheFirstAwait()
    {
        var reporter = new CrowdSecReporter(new NullAlertClient(), Settings());
        using var cts = new CancellationTokenSource();

        reporter.Start(cts.Token);

        var drain = reporter.DrainTaskForTesting;
        Assert.NotNull(drain);

        var first = await Task.WhenAny(drain, Task.Delay(TimeSpan.FromMilliseconds(500)));
        Assert.False(ReferenceEquals(first, drain), "drain task completed while the loop was still running");

        reporter.Stop();

        Assert.True(drain.IsCompleted, "Stop() returned before the drain loop exited");
    }

    private sealed class NullAlertClient : ICrowdSecAlertClient
    {
        public ValueTask PostAlertsAsync(IReadOnlyList<CrowdSecAlert> alerts, CancellationToken token) =>
            ValueTask.CompletedTask;

        public ValueTask DeleteDecisionsAsync(string origin, IPAddress ip, CancellationToken token) =>
            ValueTask.CompletedTask;

        public void Dispose() { }
    }

    private sealed class RecordingAlertClient : ICrowdSecAlertClient
    {
        public List<IReadOnlyList<CrowdSecAlert>> Posted { get; } = [];
        public List<IPAddress> Deleted { get; } = [];

        public ValueTask PostAlertsAsync(IReadOnlyList<CrowdSecAlert> alerts, CancellationToken token)
        {
            Posted.Add(alerts);
            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteDecisionsAsync(string origin, IPAddress ip, CancellationToken token)
        {
            Deleted.Add(ip);
            return ValueTask.CompletedTask;
        }

        public void Dispose() { }
    }

    private sealed class ThrowingAlertClient : ICrowdSecAlertClient
    {
        public ValueTask PostAlertsAsync(IReadOnlyList<CrowdSecAlert> alerts, CancellationToken token) =>
            throw new InvalidOperationException("simulated LAPI outage");

        public ValueTask DeleteDecisionsAsync(string origin, IPAddress ip, CancellationToken token) =>
            ValueTask.CompletedTask;

        public void Dispose() { }
    }
}
