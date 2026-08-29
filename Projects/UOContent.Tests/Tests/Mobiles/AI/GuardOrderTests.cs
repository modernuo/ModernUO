using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// A guarding pet fights without leaving the Guard order, retargets toward the master's
// closest aggressor, and stands down when nothing threatens. Scene: the open
// (1495..1500, 1600) Trammel segment; targets are adjacent so no pathfinding runs.
[Collection("Sequential UOContent Tests")]
public class GuardOrderTests : IDisposable
{
    private readonly List<Mobile> _created = new();

    private sealed class AggressorStub : Mobile
    {
        public AggressorStub() => Body = 0xC9;
    }

    public void Dispose()
    {
        foreach (var m in _created)
        {
            m?.Delete();
        }

        _created.Clear();
    }

    private (PlayerMobile master, PetTestStub pet) SpawnGuardingPet(out Map map, out int z)
    {
        map = Map.Maps[1];
        Assert.NotNull(map);
        map.GetAverageZ(1500, 1600, out _, out z, out _);

        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(new Point3D(1500, 1600, (sbyte)z), map);
        _created.Add(master);

        var pet = new PetTestStub();
        pet.MoveToWorld(new Point3D(1499, 1600, (sbyte)z), map);
        pet.SetControlMaster(master);
        _created.Add(pet);

        pet.AIObject.AITimer?.Stop(); // drive manually
        pet.ControlOrder = OrderType.Guard;
        pet.AIObject.AITimer?.Stop(); // the order change restarts the timer

        return (master, pet);
    }

    private AggressorStub SpawnAggressor(PetTestStub pet, Point3D loc, Mobile attacking)
    {
        var aggr = new AggressorStub();
        aggr.MoveToWorld(loc, pet.Map);
        _created.Add(aggr);

        // Setup guard: the scene must stay LOS-clear and the combatant must not be vetoed.
        Assert.True(pet.InLOS(aggr), $"no LOS from pet to aggressor at {loc}");

        if (attacking != null)
        {
            aggr.Combatant = attacking;
            Assert.Same(attacking, aggr.Combatant);
        }

        return aggr;
    }

    [Fact]
    public void GuardEngage_KeepsGuardOrder()
    {
        var (master, pet) = SpawnGuardingPet(out _, out var z);
        var aggr = SpawnAggressor(pet, new Point3D(1498, 1600, (sbyte)z), master);

        pet.AIObject.Obey();

        Assert.Same(aggr, pet.Combatant);
        Assert.Equal(OrderType.Guard, pet.ControlOrder);
        Assert.Equal(OrderType.Guard, pet.AIObject.PersistentOrder);
    }

    [Fact]
    public void Guard_RetargetsToAggressorClosestToMaster()
    {
        var (master, pet) = SpawnGuardingPet(out _, out var z);
        var far = SpawnAggressor(pet, new Point3D(1495, 1600, (sbyte)z), master);
        var near = SpawnAggressor(pet, new Point3D(1498, 1600, (sbyte)z), master);

        pet.Combatant = far; // already fighting the far aggressor

        pet.AIObject.Obey();

        Assert.Same(near, pet.Combatant); // defends the master, not the current fight
        Assert.Equal(OrderType.Guard, pet.ControlOrder);
    }

    [Fact]
    public void ExplicitAttack_ResumesGuard_WithoutChainingIntoAttack()
    {
        var (master, pet) = SpawnGuardingPet(out _, out var z);

        // Explicit kill order on a target that then becomes invalid.
        var victim = SpawnAggressor(pet, new Point3D(1498, 1600, (sbyte)z), null);
        pet.ControlTarget = victim;
        pet.ControlOrder = OrderType.Attack;
        victim.Hidden = true;

        // A second aggressor is still after the master; FightMode.Closest would chain it.
        var aggr2 = SpawnAggressor(pet, new Point3D(1497, 1600, (sbyte)z), master);

        pet.AIObject.Obey(); // attack completes -> resume the persistent Guard

        Assert.Equal(OrderType.Guard, pet.ControlOrder);

        pet.AIObject.Obey(); // the guard scan engages the remaining aggressor in-order

        Assert.Same(aggr2, pet.Combatant);
        Assert.Equal(OrderType.Guard, pet.ControlOrder);
    }

    [Fact]
    public void PeacefulGuard_StandsDown()
    {
        var (_, pet) = SpawnGuardingPet(out _, out _);
        Assert.True(pet.Warmode); // the guard order opens in war stance

        pet.AIObject.Obey(); // nothing to guard against

        Assert.False(pet.Warmode);
        Assert.Null(pet.Combatant);
        Assert.Null(pet.FocusMob);
    }
}
