using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

[Collection("Sequential UOContent Tests")]
public class GuardFriendlyFireTests : IDisposable
{
    private readonly List<Mobile> _created = [];

    // AI_Mage stands in for a shipped caster: MageAI's combat loop has its own reacquire path.
    private sealed class MagePet : BaseCreature
    {
        public MagePet() : base(AIType.AI_Mage, FightMode.Closest, 10, 1) => Body = 0xC8;

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }

    private (PlayerMobile owner, PetTestStub pet) Spawn()
    {
        Skip.If(!Server.Tests.TestServerInitializer.TileDataLoaded, "Requires UO client map data.");
        var z = Map.Felucca.GetAverageZ(1500, 1600);
        // The constructed PlayerMobile, not PetTestSetup's deserialization-shaped one: the harmful
        // checks read PermaFlags, which only the full constructor allocates.
        var owner = new PlayerMobile();
        owner.MoveToWorld(new Point3D(1500, 1600, z), Map.Felucca);
        _created.Add(owner);
        var pet = OtherPet(owner, new Point3D(1499, 1600, z));
        pet.IssueOrder(OrderType.Guard, owner);
        pet.AIObject.AITimer.Stop();
        return (owner, pet);
    }

    private PetTestStub OtherPet(PlayerMobile owner, Point3D location)
    {
        var pet = new PetTestStub();
        pet.MoveToWorld(location, owner.Map);
        pet.SetControlMaster(owner);
        pet.AIObject.AITimer.Stop();
        _created.Add(pet);
        return pet;
    }

    [SkippableFact]
    public void GuardFallbackDoesNotAcquireItsOwner()
    {
        var (owner, pet) = Spawn();
        Assert.Null(pet.ControlTarget); // speech dispatch clears this; guard must not fall through to wild acquisition.
        Assert.True(pet.CanSee(owner));
        Assert.True(pet.InLOS(owner));
        pet.NextReacquireTime = Core.TickCount - 1;
        Assert.False(pet.AIObject.AcquireFocusMob(pet.RangePerception, pet.FightMode, false, false, true));
        Assert.Null(pet.FocusMob);
        Assert.Null(pet.Combatant);
    }

    [SkippableFact]
    public void GuardDropsStaleCombatAgainstAnotherPetOfItsOwner()
    {
        var (owner, pet) = Spawn();
        var sibling = OtherPet(owner, new Point3D(1498, 1600, owner.Z));
        pet.IssueOrder(OrderType.Attack, owner, sibling);
        Assert.Same(sibling, pet.Combatant);
        pet.IssueOrder(OrderType.Guard, owner);
        pet.AIObject.DoOrderGuard();
        Assert.Null(pet.Combatant);
        Assert.Null(pet.FocusMob);
        Assert.Equal(OrderType.Guard, pet.ControlOrder);
    }

    [SkippableFact]
    public void GuardDoesNotRetaliateAgainstOwnerFriendlyDamage()
    {
        var (owner, pet) = Spawn();
        pet.AggressiveAction(owner, false);
        Assert.Null(pet.Combatant);
        Assert.Equal(OrderType.Guard, pet.ControlOrder);
    }

    [SkippableFact]
    public void GuardDoesNotRetaliateAgainstSiblingFriendlyDamage()
    {
        var (owner, pet) = Spawn();
        var sibling = OtherPet(owner, new Point3D(1498, 1600, owner.Z));
        pet.AggressiveAction(sibling, false);
        Assert.Null(pet.Combatant);
    }

    [SkippableFact]
    public void GuardReacquiresAnEnemyAttackingItsOwner()
    {
        var (owner, pet) = Spawn();
        var enemy = new PetTestStub();
        _created.Add(enemy);
        enemy.MoveToWorld(new Point3D(1498, 1600, owner.Z), owner.Map);
        enemy.AIObject.AITimer.Stop();
        enemy.Combatant = owner;
        Assert.Same(owner, enemy.Combatant);
        Assert.True(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Same(enemy, pet.FocusMob);
    }

    [SkippableFact]
    public void GuardStillDefendsAgainstAnotherPlayersPet()
    {
        var (owner, pet) = Spawn();
        var enemyOwner = new PlayerMobile();
        _created.Add(enemyOwner);
        enemyOwner.MoveToWorld(new Point3D(1497, 1600, owner.Z), owner.Map);
        var enemy = OtherPet(enemyOwner, new Point3D(1498, 1600, owner.Z));
        enemy.IssueOrder(OrderType.Attack, enemyOwner, owner);
        Assert.True(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Same(enemy, pet.FocusMob);
    }

    [SkippableFact]
    public void GuardIgnoresSiblingInOwnersAggressorHistory()
    {
        var (owner, pet) = Spawn();
        var sibling = OtherPet(owner, new Point3D(1498, 1600, owner.Z));
        owner.AggressiveAction(sibling, false);
        Assert.Contains(owner.Aggressors, entry => entry.Attacker == sibling);
        Assert.False(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Null(pet.FocusMob);
    }

    [SkippableFact]
    public void GuardIgnoresOwnControlledSummonAttackingOwner()
    {
        var (owner, pet) = Spawn();
        var summon = OtherPet(owner, new Point3D(1498, 1600, owner.Z));
        summon.Summoned = true;
        summon.Master = owner;
        summon.Combatant = owner;
        Assert.Same(owner, summon.Combatant);
        pet.AggressiveAction(summon, false);
        Assert.Null(pet.Combatant);
        Assert.False(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Null(pet.FocusMob);
    }

    [SkippableFact]
    public void GuardDefendsAgainstOwnUncontrolledSummonAttackingOwner()
    {
        var (owner, pet) = Spawn();
        var vortex = new PetTestStub { Summoned = true, Master = owner };
        _created.Add(vortex);
        vortex.MoveToWorld(new Point3D(1498, 1600, owner.Z), owner.Map);
        vortex.AIObject.AITimer.Stop();
        vortex.Combatant = owner;
        Assert.Same(owner, vortex.Combatant);
        Assert.True(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Same(vortex, pet.FocusMob);
    }

    [SkippableFact]
    public void FriendlyDamageDoesNotInterruptGuardCombat()
    {
        var (owner, pet) = Spawn();
        var enemy = new PetTestStub();
        _created.Add(enemy);
        enemy.MoveToWorld(new Point3D(1496, 1600, owner.Z), owner.Map);
        enemy.AIObject.AITimer.Stop();
        pet.Combatant = enemy;
        pet.AggressiveAction(owner, false);
        Assert.Same(enemy, pet.Combatant);
    }

    [SkippableFact]
    public void ExplicitAttackStillAcquiresTheCommandedTarget()
    {
        var (owner, pet) = Spawn();
        var sibling = OtherPet(owner, new Point3D(1498, 1600, owner.Z));
        pet.IssueOrder(OrderType.Attack, owner, sibling);
        Assert.True(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Same(sibling, pet.FocusMob);
    }

    [SkippableFact]
    public void BardProvocationStillOverridesGuard()
    {
        var (owner, pet) = Spawn();
        pet.BardProvoked = true;
        pet.BardTarget = owner;
        Assert.True(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Same(owner, pet.FocusMob);
    }

    [SkippableTheory]
    [InlineData(OrderType.Follow)]
    [InlineData(OrderType.Come)]
    [InlineData(OrderType.Stay)]
    public void NonCombatOrdersDoNotAcquireTheirFriendlyTarget(OrderType order)
    {
        var (owner, pet) = Spawn();
        pet.IssueOrder(order, owner, owner);
        Assert.False(pet.AIObject.AcquireFocusMob(10, pet.FightMode, false, false, true));
        Assert.Null(pet.FocusMob);
    }

    [SkippableFact]
    public void GuardingMageDoesNotTurnOnOwnerWhenEnemyDisappears()
    {
        var (owner, _) = Spawn();
        var mage = new MagePet();
        _created.Add(mage);
        mage.MoveToWorld(new Point3D(1498, 1600, owner.Z), owner.Map);
        mage.SetControlMaster(owner);
        mage.IssueOrder(OrderType.Guard, owner);
        mage.AIObject.AITimer.Stop();
        var enemy = new PetTestStub();
        _created.Add(enemy);
        enemy.MoveToWorld(new Point3D(1497, 1600, owner.Z), owner.Map);
        enemy.AIObject.AITimer.Stop();
        mage.Combatant = enemy;
        enemy.Hidden = true;
        mage.NextReacquireTime = Core.TickCount - 1;
        Assert.IsType<MageAI>(mage.AIObject);
        mage.AIObject.DoActionCombat();
        Assert.NotSame(owner, mage.Combatant);
        Assert.Null(mage.FocusMob);
        mage.AIObject.DoOrderGuard();
        Assert.Null(mage.Combatant);
    }

    public void Dispose()
    {
        foreach (var mobile in _created)
        {
            mobile.Delete();
        }
    }
}
