using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using UOContent.Tests.Mobiles.AI;
using Xunit;

namespace UOContent.Tests.Commands;

[Collection("Sequential UOContent Tests")]
public class PropertyWriteOwnershipTests : IDisposable
{
    private const string Refused = "You cannot modify a higher privileged mobile.";

    private readonly List<IEntity> _created = [];

    public void Dispose()
    {
        for (var i = _created.Count - 1; i >= 0; i--)
        {
            _created[i].Delete();
        }

        _created.Clear();
    }

    private Mobile CreateStaff(AccessLevel level)
    {
        var m = new Mobile { AccessLevel = level };
        m.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        _created.Add(m);
        return m;
    }

    private PlayerMobile CreateOwnerWithPack(AccessLevel level, out Backpack pack)
    {
        var owner = new PlayerMobile { AccessLevel = level };
        owner.MoveToWorld(new Point3D(1501, 1600, 0), Map.Felucca);
        _created.Add(owner);

        pack = new Backpack { Hue = 5 };
        owner.AddItem(pack);
        _created.Add(pack);

        return owner;
    }

    private PetTestStub CreatePet(Mobile owner)
    {
        var pet = new PetTestStub();
        pet.MoveToWorld(new Point3D(1502, 1600, 0), owner.Map);
        pet.SetControlMaster(owner);
        pet.AIObject.AITimer.Stop();
        _created.Add(pet);
        return pet;
    }

    [Fact]
    public void ChainThroughHigherPrivilegedOwner_IsRefused()
    {
        var gm = CreateStaff(AccessLevel.GameMaster);
        var owner = CreateOwnerWithPack(AccessLevel.Owner, out var pack);
        var pet = CreatePet(owner);

        var result = Properties.SetValue(gm, pet, "ControlMaster.Backpack.Hue", "0");

        Assert.Equal(Refused, result);
        Assert.Equal(5, pack.Hue);
    }

    [Fact]
    public void ItemHeldByHigherPrivilegedMobile_IsRefused()
    {
        var admin = CreateStaff(AccessLevel.Administrator);
        var developer = CreateOwnerWithPack(AccessLevel.Developer, out var pack);

        var result = Properties.SetValue(admin, developer, "Backpack.Movable", "false");

        Assert.Equal(Refused, result);
        Assert.True(pack.Movable);
    }

    [Fact]
    public void ChainThroughPlayerOwner_Succeeds()
    {
        var gm = CreateStaff(AccessLevel.GameMaster);
        var owner = CreateOwnerWithPack(AccessLevel.Player, out var pack);
        var pet = CreatePet(owner);

        Assert.Equal("Property has been set.", Properties.SetValue(gm, pet, "ControlMaster.Backpack.Hue", "0"));
        Assert.Equal(0, pack.Hue);

        Assert.Equal("Property has been set.", Properties.SetValue(gm, owner, "Backpack.Movable", "false"));
        Assert.False(pack.Movable);
    }
}
