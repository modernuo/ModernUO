using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Engines.CannedEvil;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.CannedEvil;

// Minions summoned by a champion belong to its spawn, so clearing the spawn removes them instead of
// leaving them behind for good.
[Collection("Sequential UOContent Tests")]
public class ChampionMinionTests : IDisposable
{
    private static readonly Point3D _location = new(1500, 1500, 0);

    private readonly List<IEntity> _created = [];

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

    private ChampionSpawn NewSpawn()
    {
        var spawn = new ChampionSpawn();
        _created.Add(spawn);
        spawn.MoveToWorld(_location, Map.Felucca);
        spawn.SpawnArea = new Rectangle2D(new Point2D(_location.X - 24, _location.Y - 24), new Point2D(_location.X + 24, _location.Y + 24));
        return spawn;
    }

    private LordOaks NewOaks()
    {
        var oaks = new LordOaks();
        _created.Add(oaks);
        oaks.MoveToWorld(_location, Map.Felucca);
        return oaks;
    }

    private Mobile NewAttacker()
    {
        var attacker = new Mongbat();
        _created.Add(attacker);
        return attacker;
    }

    [Fact]
    public void AChampionsMinion_IsDeletedWithTheSpawnsCreatures()
    {
        var spawn = NewSpawn();
        var oaks = NewOaks();
        spawn.Champion = oaks;

        oaks.AggressiveAction(NewAttacker(), false);
        var queen = World.Mobiles.Values.OfType<Silvani>().Single();

        Assert.Contains(queen, spawn.Minions);

        spawn.DeleteCreatures();

        Assert.True(queen.Deleted);
        Assert.Empty(spawn.Minions);
    }

    [Fact]
    public void AMinionOfAMinion_AlsoBelongsToTheSpawn()
    {
        var spawn = NewSpawn();
        var oaks = NewOaks();
        spawn.Champion = oaks;

        oaks.AggressiveAction(NewAttacker(), false);
        var queen = World.Mobiles.Values.OfType<Silvani>().Single();

        var pixie = new Pixie();
        _created.Add(pixie);
        Assert.True(queen.SpawnMinion(pixie));

        Assert.Contains(pixie, spawn.Minions);
    }

    [Fact]
    public void ACreatureOutsideAnySpawn_SummonsAStandaloneMinion()
    {
        var spawn = NewSpawn();
        var oaks = NewOaks(); // in the spawn's region, but not its champion

        oaks.AggressiveAction(NewAttacker(), false);
        var queen = World.Mobiles.Values.OfType<Silvani>().Single();

        spawn.DeleteCreatures();

        Assert.False(queen.Deleted);
        Assert.Null(ChampionSpawn.FindSpawnOf(oaks));
    }
}
