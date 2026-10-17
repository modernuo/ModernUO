using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

// Lord Oaks' abilities and spell shield depend on his queen, so the queen must spawn from combat,
// never from a query such as GetMonsterAbilities or a damage calculation.
[Collection("Sequential UOContent Tests")]
public class LordOaksTests : IDisposable
{
    private readonly List<Mobile> _created = [];

    public void Dispose()
    {
        foreach (var m in World.Mobiles.Values.OfType<Silvani>().ToList())
        {
            m.Delete();
        }

        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private LordOaks NewOaks(Map map)
    {
        var oaks = new LordOaks();
        _created.Add(oaks);
        oaks.MoveToWorld(new Point3D(1000, 1000, 0), map);
        return oaks;
    }

    private Mobile NewAttacker()
    {
        var attacker = new Mongbat();
        _created.Add(attacker);
        return attacker;
    }

    private static int SilvaniCount() => World.Mobiles.Values.Count(m => m is Silvani);

    [Fact]
    public void QueryingOrDeletingAnUnengagedOaks_SpawnsNoQueen()
    {
        var oaks = NewOaks(Map.Felucca);

        var scalar = 1.0;
        oaks.AlterDamageScalarFrom(null, ref scalar);
        oaks.GetMonsterAbilities();
        oaks.Delete();

        Assert.Equal(1.0, scalar);
        Assert.Equal(0, SilvaniCount());
    }

    [Fact]
    public void Aggression_SpawnsTheQueenOnce_AndShieldsOaksWhileSheLives()
    {
        var oaks = NewOaks(Map.Felucca);
        var attacker = NewAttacker();

        oaks.AggressiveAction(attacker, false);
        oaks.AggressiveAction(attacker, false);

        var queen = World.Mobiles.Values.OfType<Silvani>().Single();

        var shielded = 1.0;
        oaks.AlterDamageScalarFrom(attacker, ref shielded);
        Assert.Equal(0.1, shielded, 6);
        Assert.True(oaks.CanUseAbility(MonsterAbilities.SummonPixiesCounter));

        queen.Delete();

        var exposed = 1.0;
        oaks.AlterDamageScalarFrom(attacker, ref exposed);
        oaks.AggressiveAction(attacker, false);

        Assert.Equal(1.0, exposed);
        Assert.False(oaks.CanUseAbility(MonsterAbilities.SummonPixiesCounter));
        Assert.Equal(0, SilvaniCount());
    }

    [Fact]
    public void AnOaksOnTheInternalMap_NeverSpawnsTheQueen()
    {
        var oaks = new LordOaks();
        _created.Add(oaks);

        oaks.AggressiveAction(NewAttacker(), false);

        Assert.Equal(0, SilvaniCount());
    }
}
