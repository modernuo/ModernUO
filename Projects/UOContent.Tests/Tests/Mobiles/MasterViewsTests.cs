using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

// One stored master, read through gates on Controlled and Summoned.
[Collection("Sequential UOContent Tests")]
public class MasterViewsTests : IDisposable
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

    public enum Shape
    {
        Wild,
        Pet,
        ControlledSummon,
        UncontrolledSummon,
        Enraged
    }

    [Theory]
    [InlineData(Shape.Wild)]
    [InlineData(Shape.Pet)]
    [InlineData(Shape.ControlledSummon)]
    [InlineData(Shape.UncontrolledSummon)]
    [InlineData(Shape.Enraged)]
    public void Views_ByShape(Shape shape)
    {
        var bc = NewCreature();
        var master = NewPlayer();

        switch (shape)
        {
            case Shape.Pet:
                {
                    bc.SetControlMaster(master);
                    break;
                }
            case Shape.ControlledSummon:
                {
                    bc.SetControlMaster(master);
                    bc.Summoned = true;
                    break;
                }
            case Shape.UncontrolledSummon:
                {
                    bc.Summoned = true;
                    bc.Master = master;
                    break;
                }
            case Shape.Enraged:
                {
                    bc.Master = master;
                    break;
                }
        }

        var controlled = shape is Shape.Pet or Shape.ControlledSummon;
        var summoned = shape is Shape.ControlledSummon or Shape.UncontrolledSummon;

        Assert.Equal(shape == Shape.Wild ? null : master, bc.Master);
        Assert.Equal(controlled ? master : null, bc.ControlMaster);
        Assert.Equal(summoned ? master : null, bc.SummonMaster);
        Assert.Equal(controlled || summoned ? master : null, bc.GetMaster());
    }

    [Fact]
    public void Master_Reassignment_MovesFollowerSlots()
    {
        var bc = NewCreature();
        bc.ControlSlots = 2;
        var first = NewPlayer();
        var second = NewPlayer();

        bc.Master = first;
        Assert.Equal(2, first.Followers);

        bc.Master = second;
        Assert.Equal(0, first.Followers);
        Assert.Equal(2, second.Followers);
        Assert.Null(bc.ControlMaster); // not Controlled

        bc.Master = null;
        Assert.Equal(0, second.Followers);
    }

    [Fact]
    public void SetControlMasterNull_KeepsAnUncontrolledSummonsCaster()
    {
        var bc = NewCreature();
        var caster = NewPlayer();
        bc.Summoned = true;
        bc.Master = caster;

        bc.SetControlMaster(null);

        Assert.Equal(caster, bc.SummonMaster);
        Assert.Equal(bc.ControlSlots, caster.Followers);
    }

    [Fact]
    public void Delete_ReturnsTheCastersFollowerSlots()
    {
        var bc = NewCreature();
        var caster = NewPlayer();
        bc.Summoned = true;
        bc.Master = caster;

        bc.Delete();

        Assert.Equal(0, caster.Followers);
        Assert.Null(bc.Master);
    }
}
