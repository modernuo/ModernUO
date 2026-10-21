using System;
using Server;
using Server.Mobiles;
using Server.Spells.Necromancy;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Mobiles;

// A deleted master must not leave creatures pointing at it.
[Collection("Sequential UOContent Tests")]
public class DeletedMasterTests : IDisposable
{
    private readonly MobileTestHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private TestCreature NewCreature() => _harness.NewCreature();

    private PlayerMobile NewPlayer() => _harness.NewPlayer();

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
