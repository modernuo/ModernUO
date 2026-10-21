using System;
using System.IO;
using System.Reflection;
using Server.Configurations;
using Server.Misc;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class EmailTests : IDisposable
{
    private static readonly PropertyInfo _emailEnabled =
        typeof(EmailConfiguration).GetProperty(nameof(EmailConfiguration.EmailEnabled));

    private readonly bool _savedEmailEnabled = EmailConfiguration.EmailEnabled;

    // Pinned off rather than read from the host's email.json, so a developer config with email
    // enabled cannot turn these into real sends.
    public EmailTests() => _emailEnabled.SetValue(null, false);

    public void Dispose() => _emailEnabled.SetValue(null, _savedEmailEnabled);

    [Fact]
    public void SendCrashEmailDoesNothingWhileEmailIsDisabled()
    {
        // Missing input must be ignored while email is disabled.
        var report = Path.Combine(Path.GetTempPath(), $"crash-email-test-{Guid.NewGuid():N}", "Crash.log");

        Email.SendCrashEmail(report);
    }

    [Fact]
    public void SendQueueEmailDoesNothingWhileEmailIsDisabled()
    {
        // A null entry would throw on the enabled path, so returning cleanly shows the guard held.
        Email.SendQueueEmail(null, "Other");
    }

    [Fact]
    public void RetryWaitsDoubleFromTheConfiguredDelayAndStopAtTheLastAttempt()
    {
        TimeSpan[] expected =
        [
            TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(24)
        ];

        Assert.Equal(expected, Email.RetryDelays(5, 3));
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    public void FewerThanTwoAttemptsNeverWait(int attempts)
    {
        Assert.Empty(Email.RetryDelays(attempts, 3));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(int.MaxValue)]
    public void RetryWaitsNeverOverflowAndStopDoublingAtAnHour(int firstDelaySeconds)
    {
        var delays = Email.RetryDelays(64, firstDelaySeconds);

        Assert.All(delays, delay => Assert.InRange(delay, TimeSpan.Zero, TimeSpan.FromHours(1)));
        Assert.Equal(TimeSpan.FromHours(1), delays[^1]);
    }
}
