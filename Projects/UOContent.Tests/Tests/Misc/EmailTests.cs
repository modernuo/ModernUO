using System;
using System.IO;
using Server.Configurations;
using Server.Misc;
using Xunit;

namespace UOContent.Tests;

// SendCrashEmail returned early when email was enabled, so no crash email ever left, and ran on when it
// was disabled, building a message nobody would send; the retry loop squared its wait in an int, which
// overflowed. The first two tests pin the disabled side, which is what the test host is (it never runs
// EmailConfiguration.Configure): each method must return before it touches its input. The rest pin the
// retry schedule. Sending needs an SMTP server and is not tested here.
[Collection("Sequential UOContent Tests")]
public class EmailTests
{
    [Fact]
    public void SendCrashEmailDoesNothingWhileEmailIsDisabled()
    {
        Assert.False(EmailConfiguration.EmailEnabled, "The test host never enables email");

        // A report that does not exist: attaching it throws, so a method that runs on fails here even
        // when the addresses are configured.
        var report = Path.Combine(Path.GetTempPath(), $"crash-email-test-{Guid.NewGuid():N}", "Crash.log");

        Email.SendCrashEmail(report);
    }

    [Fact]
    public void SendQueueEmailDoesNothingWhileEmailIsDisabled()
    {
        Assert.False(EmailConfiguration.EmailEnabled, "The test host never enables email");

        // No page at all: a method that runs on dereferences it on its first line.
        Email.SendQueueEmail(null, "Other");
    }

    // The defaults (emailSendRetryCount 5, emailSendRetryDelay 3): four waits between five attempts.
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

    // Squaring an int wrapped: with a delay of 8 the third wait went negative, and Task.Delay threw.
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
