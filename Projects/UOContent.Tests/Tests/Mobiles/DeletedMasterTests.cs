using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Spells.Necromancy;
using Xunit;

namespace UOContent.Tests.Mobiles;

// A deleted master must not leave creatures pointing at it.
[Collection("Sequential UOContent Tests")]
public class DeletedMasterTests : IDisposable
{
    private readonly List<Mobile> _created = [];

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private sealed class Stub : BaseCreature
    {
        public Stub() : base(AIType.AI_Melee) => Body = 0xC9;

        public Stub(Serial serial) : base(serial)
        {
        }
    }

    private Stub NewCreature()
    {
        var bc = new Stub();
        _created.Add(bc);
        return bc;
    }

    private PlayerMobile NewPlayer()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        World.AddEntity(pm);
        _created.Add(pm);
        return pm;
    }

    [Fact]
    public void DeletingAPlayer_ReleasesAnUnbondedPet()
    {
        var owner = NewPlayer();
        var pet = NewCreature();
        pet.SetControlMaster(owner);

        owner.Delete();

        Assert.False(pet.Deleted);
        Assert.False(pet.Controlled);
        Assert.Null(pet.Master);
        Assert.True(pet.PendingDeleteTimer?.Running);
    }

    [Theory]
    [InlineData(false)] // bonded
    [InlineData(true)]  // bonded and dead
    public void DeletingAPlayer_DeletesABondedPet(bool dead)
    {
        var owner = NewPlayer();
        var pet = NewCreature();
        pet.SetControlMaster(owner);
        pet.IsBonded = true;
        pet.IsDeadPet = dead;

        owner.Delete();

        Assert.True(pet.Deleted);
    }

    [Theory]
    [InlineData(false)] // energy vortex
    [InlineData(true)]  // controlled summon
    public void DeletingAPlayer_DeletesItsSummons(bool controlled)
    {
        var caster = NewPlayer();
        var summon = NewCreature();

        if (controlled)
        {
            summon.SetControlMaster(caster);
        }

        summon.Summoned = true;
        summon.Master = caster;

        caster.Delete();

        Assert.True(summon.Deleted);
    }

    [Fact]
    public void DeletingAFamiliar_UnregistersIt()
    {
        var caster = NewPlayer();
        var familiar = NewCreature();
        familiar.SetControlMaster(caster);
        familiar.Summoned = true;
        SummonFamiliarSpell.Table[caster] = familiar;

        familiar.Delete();

        Assert.False(SummonFamiliarSpell.Table.ContainsKey(caster));
    }
}
