using System;
using Xunit;
using static Server.Tests.TestClientHarness;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class RedundantMobilePacketTests : IDisposable
{
    private static readonly Point3D _loc = new(1700, 1700, 0);

    private readonly bool _oplWasEnabled;

    public RedundantMobilePacketTests()
    {
        _oplWasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;
    }

    public void Dispose() => ObjectPropertyList.Enabled = _oplWasEnabled;

    [Fact]
    public void HitsChange_DoesNotResendPropertyListHash()
    {
        var (watcherNs, watcher) = CreateClient(_loc);
        var (subjectNs, subject) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            subject.Str = 50;
            subject.Hits = 50;
            subject.ProcessDelta();
            ClearSent(watcherNs);

            subject.Hits = 40;
            subject.ProcessDelta();

            Assert.Equal(1, CountPacketsForSerial(watcherNs, 0xA1, subject.Serial));
            Assert.False(ReceivedOplInfo(watcherNs, subject.Serial));
        }
        finally
        {
            DisposeClient(watcherNs, watcher);
            DisposeClient(subjectNs, subject);
        }
    }

    [Fact]
    public void HueChange_ResendsPropertyListHashAfterRemove()
    {
        var (watcherNs, watcher) = CreateClient(_loc);
        var (subjectNs, subject) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            subject.ProcessDelta();
            ClearSent(watcherNs);

            subject.Hue = 0x21;
            subject.ProcessDelta();

            Assert.True(ReceivedRemove(watcherNs, subject.Serial));
            Assert.True(ReceivedOplInfo(watcherNs, subject.Serial));
        }
        finally
        {
            DisposeClient(watcherNs, watcher);
            DisposeClient(subjectNs, subject);
        }
    }

    [Fact]
    public void FirstAggression_SendsOneNotorietyRefreshWithoutMobileIncoming()
    {
        var (defenderNs, defender) = CreateClient(_loc);
        var (aggressorNs, aggressor) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            ClearSent(defenderNs);

            defender.AggressiveAction(aggressor);

            Assert.Equal(1, CountPacketsForSerial(defenderNs, 0x77, aggressor.Serial));
            Assert.Equal(0, CountPacketsForSerial(defenderNs, 0x78, aggressor.Serial, 3));
        }
        finally
        {
            DisposeClient(defenderNs, defender);
            DisposeClient(aggressorNs, aggressor);
        }
    }
}
