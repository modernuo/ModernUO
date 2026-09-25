using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// A meer mage fights FightMode.Evil and its enraged creatures carry -1000 karma, so the karma rules alone
// make each an enemy of the other.
[Collection("Sequential UOContent Tests")]
public class MeerEnragedTests : IDisposable
{
    private readonly List<Mobile> _created = [];

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private T Track<T>(T m) where T : Mobile
    {
        _created.Add(m);
        return m;
    }

    private static void Place(BaseCreature bc, int x)
    {
        var map = Map.Felucca;
        bc.MoveToWorld(new Point3D(x, 1600, map.GetAverageZ(x, 1600)), map); // flat, clear line of sight both ways
        bc.AIObject.AITimer.Stop();
    }

    private static bool Acquire(BaseCreature creature)
    {
        creature.NextReacquireTime = Core.TickCount - 1;
        return creature.AIObject.AcquireFocusMob(creature.RangePerception, creature.FightMode, false, false, true);
    }

    [Fact]
    public void MeerAndItsEnragedCreature_AreNotEnemies()
    {
        var meer = Track(new MeerMage());
        var enraged = Track(new EnragedRabbit(meer));

        Assert.False(enraged.IsEnemy(meer));
        Assert.False(meer.IsEnemy(enraged));
    }

    [Fact]
    public void AnotherMeersEnragedCreature_StaysAnEnemy()
    {
        var meer = Track(new MeerMage());
        var other = Track(new MeerMage());
        var enraged = Track(new EnragedRabbit(other));

        Assert.True(enraged.IsEnemy(meer));
        Assert.True(meer.IsEnemy(enraged));
    }

    [SkippableFact]
    public void MeerAndItsEnragedCreature_NeverAcquireEachOther()
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");

        var meer = Track(new MeerMage());
        var enraged = Track(new EnragedBlackBear(meer));
        Place(meer, 1600);
        Place(enraged, 1601);

        Assert.False(Acquire(enraged));
        Assert.False(Acquire(meer));
    }
}
