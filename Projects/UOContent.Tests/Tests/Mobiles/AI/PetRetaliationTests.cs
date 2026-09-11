using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles.AI;

// Publish 51 (26 March 2008): a pet told to follow, come, stay or stop "will not attack
// anything, even if it is attacked". Guard and attack are unaffected.
[Collection("Sequential UOContent Tests")]
public class PetRetaliationTests : IDisposable
{
    private readonly List<Mobile> _created = new();

    public void Dispose()
    {
        foreach (var m in _created)
        {
            m?.Delete();
        }

        _created.Clear();
    }

    private sealed class StandDownPet : PetTestStub
    {
        public override bool StandsDownOnCommand => true;
    }

    private sealed class FightBackPet : PetTestStub
    {
        public override bool StandsDownOnCommand => false;
    }

    private (PlayerMobile master, T pet) Spawn<T>() where T : BaseCreature, new()
    {
        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        master.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
        _created.Add(master);

        var pet = new T();
        pet.MoveToWorld(new Point3D(1001, 1000, 0), Map.Felucca);
        pet.SetControlMaster(master);
        pet.AIObject.AITimer?.Stop();
        _created.Add(pet);

        return (master, pet);
    }

    private void Order(BaseCreature pet, Mobile master, OrderType order)
    {
        if (order == OrderType.None) // Publish 51's "stop": stops, may wander, will not attack
        {
            pet.ControlTarget = master;
            pet.ControlOrder = OrderType.Follow;
            pet.ControlOrder = OrderType.Stop;
            return;
        }

        pet.ControlTarget = master;
        pet.ControlOrder = order;
    }

    private BaseCreature Attack(BaseCreature pet)
    {
        var attacker = new PetTestStub();
        attacker.MoveToWorld(new Point3D(1002, 1000, 0), pet.Map);
        _created.Add(attacker);
        pet.AIObject.AITimer?.Stop();

        attacker.Combatant = pet; // a mob starts attacking the pet
        return attacker;
    }

    [Theory]
    [InlineData(OrderType.Follow)]
    [InlineData(OrderType.Come)]
    [InlineData(OrderType.Stay)]
    [InlineData(OrderType.None)] // stopped
    public void StandDownOrder_IgnoresTheAttacker(OrderType order)
    {
        var (master, pet) = Spawn<StandDownPet>();
        Order(pet, master, order);
        var resting = pet.ControlOrder;

        Attack(pet);

        Assert.Equal(resting, pet.ControlOrder); // never converts to Attack
        Assert.Null(pet.Combatant);
        Assert.False(pet.Warmode);
    }

    [Theory]
    [InlineData(OrderType.Follow)]
    [InlineData(OrderType.Come)]
    [InlineData(OrderType.Stay)]
    [InlineData(OrderType.None)]
    public void WithoutStandDown_TheSameOrdersRetaliate(OrderType order)
    {
        var (master, pet) = Spawn<FightBackPet>();
        Order(pet, master, order);

        var attacker = Attack(pet);

        Assert.Equal(OrderType.Attack, pet.ControlOrder);
        Assert.Same(attacker, pet.Combatant);
    }

    // No damage callback may put a stand-down pet back into combat behind the policy's back.
    [Theory]
    [InlineData(Expansion.AOS, false)]
    [InlineData(Expansion.AOS, true)]
    [InlineData(Expansion.ML, false)]
    [InlineData(Expansion.ML, true)]
    public void StandDownPet_StaysDown_ThroughRepeatedDamage(Expansion era, bool spellDamage)
    {
        var previous = Core.Expansion;

        try
        {
            Core.Expansion = era;
            var (master, pet) = Spawn<StandDownPet>();
            Order(pet, master, OrderType.Follow);
            var attacker = Attack(pet);

            Assert.Equal(OrderType.Follow, pet.ControlOrder); // the initial aggression stood down

            for (var i = 0; i < 500 && pet.ControlOrder == OrderType.Follow; i++)
            {
                if (spellDamage)
                {
                    pet.OnDamagedBySpell(attacker, 1);
                }
                else
                {
                    pet.OnDamage(1, attacker, false);
                }
            }

            Assert.Equal(OrderType.Follow, pet.ControlOrder);
            Assert.Null(pet.Combatant);
        }
        finally
        {
            Core.Expansion = previous;
        }
    }

    // "Guard: the pet should guard as it does currently."
    [Fact]
    public void GuardingPet_StillFights_UnderStandDown()
    {
        var (master, pet) = Spawn<StandDownPet>();
        Order(pet, master, OrderType.Guard);

        var attacker = Attack(pet);

        Assert.Equal(OrderType.Guard, pet.ControlOrder);
        Assert.Same(attacker, pet.Combatant);
        Assert.True(pet.Warmode);
    }
}
