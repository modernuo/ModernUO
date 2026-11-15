using System;
using Xunit;
using static Server.Tests.TestClientHarness;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class RedundantMobilePacketTests : IDisposable
{
    private static readonly Point3D _loc = new(1700, 1700, 0);

    private readonly bool _oplWasEnabled;
    private readonly NotorietyHandler _notorietyWas;

    public RedundantMobilePacketTests()
    {
        _oplWasEnabled = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

        _notorietyWas = Notoriety.Handler;
        Notoriety.Handler = AggressorNotoriety;
    }

    public void Dispose()
    {
        ObjectPropertyList.Enabled = _oplWasEnabled;
        Notoriety.Handler = _notorietyWas;
    }

    // The aggression part of the UOContent handler: someone who attacked you, or whom you attacked
    // without it being criminal, can be attacked.
    private static int AggressorNotoriety(Mobile source, Mobile target)
    {
        for (var i = 0; i < source.Aggressors.Count; i++)
        {
            if (source.Aggressors[i].Attacker == target)
            {
                return Notoriety.CanBeAttacked;
            }
        }

        for (var i = 0; i < source.Aggressed.Count; i++)
        {
            var info = source.Aggressed[i];

            if (!info.CriminalAggression && info.Defender == target)
            {
                return Notoriety.CanBeAttacked;
            }
        }

        return Notoriety.Innocent;
    }

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

    [Fact]
    public void RepeatedAggression_SendsNoNotorietyRefresh()
    {
        var (defenderNs, defender) = CreateClient(_loc);
        var (aggressorNs, aggressor) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            defender.AggressiveAction(aggressor);
            ClearSent(defenderNs);

            defender.AggressiveAction(aggressor);

            Assert.Equal(0, CountPacketsForSerial(defenderNs, 0x77, aggressor.Serial));
        }
        finally
        {
            DisposeClient(defenderNs, defender);
            DisposeClient(aggressorNs, aggressor);
        }
    }

    [Fact]
    public void AggressionThatLeavesNotorietyUnchanged_SendsNoRefresh()
    {
        var (defenderNs, defender) = CreateClient(_loc);
        var (aggressorNs, aggressor) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));
        Notoriety.Handler = (_, _) => Notoriety.Innocent;

        try
        {
            ClearSent(defenderNs);

            defender.AggressiveAction(aggressor);

            Assert.Equal(0, CountPacketsForSerial(defenderNs, 0x77, aggressor.Serial));
        }
        finally
        {
            DisposeClient(defenderNs, defender);
            DisposeClient(aggressorNs, aggressor);
        }
    }

    [Fact]
    public void RemovingAggressor_RefreshesNotorietyOnlyWhenItChanges()
    {
        var (defenderNs, defender) = CreateClient(_loc);
        var (aggressorNs, aggressor) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            defender.AggressiveAction(aggressor);
            ClearSent(defenderNs);

            defender.RemoveAggressor(aggressor);

            Assert.Equal(1, CountPacketsForSerial(defenderNs, 0x77, aggressor.Serial));

            ClearSent(defenderNs);
            defender.RemoveAggressor(aggressor);

            Assert.Equal(0, CountPacketsForSerial(defenderNs, 0x77, aggressor.Serial));
        }
        finally
        {
            DisposeClient(defenderNs, defender);
            DisposeClient(aggressorNs, aggressor);
        }
    }

    [Fact]
    public void Aggression_RefreshesTheAggressorsViewToo()
    {
        var (defenderNs, defender) = CreateClient(_loc);
        var (aggressorNs, aggressor) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            ClearSent(aggressorNs);

            defender.AggressiveAction(aggressor, false);

            Assert.Equal(1, CountPacketsForSerial(aggressorNs, 0x77, defender.Serial));
        }
        finally
        {
            DisposeClient(defenderNs, defender);
            DisposeClient(aggressorNs, aggressor);
        }
    }

    [Fact]
    public void RepeatedAggressionTurningCriminal_RefreshesTheAggressorsView()
    {
        var (defenderNs, defender) = CreateClient(_loc);
        var (aggressorNs, aggressor) = CreateClient(new Point3D(_loc.X + 1, _loc.Y, 0));

        try
        {
            defender.AggressiveAction(aggressor, false);
            ClearSent(aggressorNs);

            defender.AggressiveAction(aggressor, true);

            Assert.Equal(1, CountPacketsForSerial(aggressorNs, 0x77, defender.Serial));
        }
        finally
        {
            DisposeClient(defenderNs, defender);
            DisposeClient(aggressorNs, aggressor);
        }
    }
}
