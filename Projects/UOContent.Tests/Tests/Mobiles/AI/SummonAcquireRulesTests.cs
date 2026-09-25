using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class SummonAcquireRulesTests : IDisposable
{
    private readonly List<Mobile> _created = [];
    private readonly Expansion _previous = Core.Expansion;

    private PlayerMobile Player(Map map, int x, int intel)
    {
        var player = new PlayerMobile { Player = true, RawInt = intel };
        player.MoveToWorld(new Point3D(x, 1600, map.GetAverageZ(x, 1600)), map);
        _created.Add(player);
        return player;
    }

    private BaseCreature Summon(BaseCreature summon, Mobile caster, int x)
    {
        _created.Add(summon);
        summon.Summoned = true;
        summon.Master = caster;
        summon.MoveToWorld(new Point3D(x, 1600, caster.Map.GetAverageZ(x, 1600)), caster.Map);
        summon.AIObject.AITimer.Stop();
        return summon;
    }

    private static bool Acquire(BaseCreature creature)
    {
        creature.NextReacquireTime = Core.TickCount - 1;
        return creature.AIObject.AcquireFocusMob(creature.RangePerception, creature.FightMode, false, false, true);
    }

    [SkippableTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreAosFeluccaVortexTurnsOnItsCaster(bool bladeSpirits)
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        Core.Expansion = Expansion.UOR;

        var caster = Player(Map.Felucca, 1500, 100);
        var summon = Summon(bladeSpirits ? new BladeSpirits() : new EnergyVortex(), caster, 1499);

        Assert.True(Acquire(summon));
        Assert.Same(caster, summon.FocusMob);
    }

    [SkippableFact]
    public void PreAosFeluccaVortexPrefersAnyoneOverItsCaster()
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        Core.Expansion = Expansion.UOR;

        // The caster outranks the bystander on the vortex's int-over-distance ranking.
        var caster = Player(Map.Felucca, 1500, 100);
        var bystander = Player(Map.Felucca, 1496, 10);
        var vortex = Summon(new EnergyVortex(), caster, 1499);

        Assert.True(Acquire(vortex));
        Assert.Same(bystander, vortex.FocusMob);
    }

    [SkippableTheory]
    [InlineData(Expansion.UOR, 1)] // Trammel
    [InlineData(Expansion.AOS, 0)] // Felucca
    public void VortexSparesItsCasterOutsidePreAosFelucca(Expansion expansion, int mapIndex)
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        Core.Expansion = expansion;

        var caster = Player(Map.Maps[mapIndex], 1500, 100);
        var vortex = Summon(new EnergyVortex(), caster, 1499);

        Assert.False(Acquire(vortex));
        Assert.Null(vortex.FocusMob);
    }

    [SkippableFact]
    public void MasterlessSummonDoesNotBreakAcquisition()
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        Core.Expansion = Expansion.AOS;

        var player = Player(Map.Felucca, 1500, 100);
        var hunter = new PetTestStub();
        _created.Add(hunter);
        hunter.MoveToWorld(new Point3D(1499, 1600, player.Z), player.Map);
        hunter.AIObject.AITimer.Stop();

        var masterless = new PetTestStub { Summoned = true };
        _created.Add(masterless);
        masterless.MoveToWorld(new Point3D(1498, 1600, player.Z), player.Map);
        masterless.AIObject.AITimer.Stop();

        Assert.True(Acquire(hunter));
    }

    public void Dispose()
    {
        Core.Expansion = _previous;

        foreach (var mobile in _created)
        {
            mobile.Delete();
        }
    }
}
