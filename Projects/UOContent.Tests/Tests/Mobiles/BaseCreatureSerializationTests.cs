using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

// BaseCreature's SaveFlag format round-trips both a default and a fully-populated
// creature with exact byte consumption, and back-to-back saves are byte-identical
// (freeze-time stability).
[Collection("Sequential UOContent Tests")]
public class BaseCreatureSerializationTests : IDisposable
{
    private readonly List<Mobile> _created = new();

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private class CreatureStub : BaseCreature
    {
        public CreatureStub() : base(AIType.AI_Melee) => Body = 0xC9;

        public CreatureStub(Serial serial) : base(serial) => Body = 0xC9;

        // Stands in for the npc-speeds table (unconfigured in the test fixture).
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }

        public override void GetMoveSpeeds(out double activeMoveSpeed, out double passiveMoveSpeed)
        {
            activeMoveSpeed = 0.6;
            passiveMoveSpeed = 1.2;
        }
    }

    private CreatureStub NewCreature()
    {
        var bc = new CreatureStub();
        _created.Add(bc);
        return bc;
    }

    private static byte[] Snapshot(Mobile m)
    {
        var writer = new BufferWriter(true);
        m.Serialize(writer);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
        return buffer;
    }

    private CreatureStub Load(byte[] buffer)
    {
        var copy = new CreatureStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position); // exact consumption
        return copy;
    }

    [Fact]
    public void DefaultCreature_RoundTrips_AndElidesEverything()
    {
        var bc = NewCreature();

        var buffer = Snapshot(bc);
        var copy = Load(buffer);

        Assert.Equal(AIType.AI_Melee, copy.AI);
        Assert.Equal(BaseCreature.DefaultRangePerception, copy.RangePerception);
        Assert.Equal(0.3, copy.ActiveSpeed);
        Assert.Equal(0.6, copy.PassiveSpeed);
        Assert.Equal(0.6, copy.CurrentSpeed);
        Assert.Equal(0.6, copy.ActiveMoveSpeed);   // class None (no table in tests): restored from the wire
        Assert.Equal(1.2, copy.PassiveMoveSpeed);
        Assert.Equal(100, copy.PhysicalDamage);
        Assert.Equal(BaseCreature.MaxLoyalty, copy.Loyalty);
        Assert.Equal(1, copy.ControlSlots);
        Assert.NotNull(copy.Owners);
        Assert.Empty(copy.Owners);
    }

    [Fact]
    public void BackToBackSaves_AreByteIdentical()
    {
        var bc = NewCreature();
        bc.SetDamage(5, 10);
        bc.PhysicalResistanceSeed = 25;

        Assert.Equal(Snapshot(bc), Snapshot(bc));
    }

    [Fact]
    public void PopulatedCreature_RoundTrips()
    {
        var bc = NewCreature();
        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        World.AddEntity(master); // ReadEntity resolves the reference through the world table
        _created.Add(master);

        bc.Tamable = true;
        bc.MinTameSkill = 47.1;
        bc.SetControlMaster(master);
        bc.Owners.Add(master);
        bc.ControlOrder = OrderType.Guard;
        bc.SetDamage(11, 17);
        bc.SetSpeed(0.2, 0.4); // hand-tuned: no longer matches the stub table
        bc.SetMoveSpeed(0.25, 0.5);
        bc.PhysicalResistanceSeed = 40;
        bc.EnergyResistSeed = 15;
        bc.FireDamage = 25;
        bc.PhysicalDamage = 75;
        bc.HitsMaxSeed = 250;
        bc.Loyalty = 55;
        bc.Home = new Point3D(1000, 1100, 5);
        bc.RangeHome = 4;
        bc.Team = 3;
        bc.IsBonded = true;
        bc.BondingBegin = Core.Now;
        bc.RemoveIfUntamed = true;
        bc.RemoveStep = 2;
        bc.CorpseNameOverride = "a test corpse";

        var copy = Load(Snapshot(bc));

        Assert.True(copy.Controlled);
        Assert.Equal(master, copy.ControlMaster);
        Assert.Equal(OrderType.Guard, copy.ControlOrder);
        Assert.True(copy.Tamable);
        Assert.Equal(47.1, copy.MinTameSkill);
        Assert.Equal(11, copy.DamageMin);
        Assert.Equal(17, copy.DamageMax);
        Assert.Equal(0.2, copy.ActiveSpeed);
        Assert.Equal(0.4, copy.PassiveSpeed);
        Assert.Equal(0.25, copy.ActiveMoveSpeed);
        Assert.Equal(0.5, copy.PassiveMoveSpeed);
        Assert.Equal(40, copy.PhysicalResistanceSeed);
        Assert.Equal(15, copy.EnergyResistSeed);
        Assert.Equal(25, copy.FireDamage);
        Assert.Equal(75, copy.PhysicalDamage);
        Assert.Equal(250, copy.HitsMaxSeed);
        Assert.Equal(55, copy.Loyalty);
        Assert.Equal(new Point3D(1000, 1100, 5), copy.Home);
        Assert.Equal(4, copy.RangeHome);
        Assert.Equal(3, copy.Team);
        Assert.True(copy.IsBonded);
        Assert.Equal(bc.BondingBegin, copy.BondingBegin);
        Assert.True(copy.RemoveIfUntamed);
        Assert.Equal(2, copy.RemoveStep);
        Assert.Equal("a test corpse", copy.CorpseNameOverride);
        Assert.Equal(master, copy.LastOwner);
    }

    [Fact]
    public void UncontrolledSummon_KeepsItsSummonMaster()
    {
        var bc = NewCreature();
        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        World.AddEntity(master);
        _created.Add(master);

        // Energy vortex-style: summoned with a master, never controlled.
        bc.Summoned = true;
        bc.SummonMaster = master;

        var copy = Load(Snapshot(bc));

        Assert.True(copy.Summoned);
        Assert.False(copy.Controlled);
        Assert.Equal(master, copy.SummonMaster);
        Assert.Null(copy.ControlMaster);
    }

    private sealed class BucketStub : BaseCreature
    {
        public BucketStub() : base(AIType.AI_Melee) => Body = 0xC9;

        public BucketStub(Serial serial) : base(serial) => Body = 0xC9;

        public override SpeedLevel DefaultSpeedClass => SpeedLevel.Fast;
    }

    [Fact]
    public void SpeedClass_Assignment_AppliesBucket_AndRoundTrips()
    {
        NPCSpeeds.RegisterSpeed(new NPCSpeeds.SpeedClassEntry
        {
            Level = SpeedLevel.Fast, ActiveSpeed = 0.2, PassiveSpeed = 0.4,
            ActiveMoveSpeed = 0.3, PassiveMoveSpeed = 0.9, Types = new HashSet<Type>()
        });
        NPCSpeeds.RegisterSpeed(new NPCSpeeds.SpeedClassEntry
        {
            Level = SpeedLevel.VeryFast, ActiveSpeed = 0.125, PassiveSpeed = 0.3,
            ActiveMoveSpeed = 0.125, PassiveMoveSpeed = 0.6, Types = new HashSet<Type>()
        });

        var bc = new BucketStub();
        _created.Add(bc);

        Assert.Equal(0.2, bc.ActiveSpeed); // seeded from the default bucket
        Assert.Equal(0.3, bc.ActiveMoveSpeed);

        bc.SpeedClass = SpeedLevel.VeryFast; // boss state change

        Assert.Equal(SpeedLevel.VeryFast, bc.SpeedClass); // conforming assignment holds
        Assert.Equal(0.125, bc.ActiveSpeed);
        Assert.Equal(0.3, bc.PassiveSpeed);
        Assert.Equal(0.125, bc.ActiveMoveSpeed);
        Assert.Equal(0.6, bc.PassiveMoveSpeed);
        Assert.Equal(0.3, bc.CurrentSpeed); // stayed in the passive mode

        // The changed bucket persists; the (bucket-matching) speeds elide but restore
        // through the new bucket - the consistency the stateful SpeedClass guarantees.
        var writer = new BufferWriter(true);
        bc.Serialize(writer);
        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = new BucketStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(SpeedLevel.VeryFast, copy.SpeedClass);
        Assert.Equal(0.125, copy.ActiveSpeed);
        Assert.Equal(0.3, copy.PassiveSpeed);
        Assert.Equal(0.125, copy.ActiveMoveSpeed);
        Assert.Equal(0.6, copy.PassiveMoveSpeed);
    }

    [Fact]
    public void PartialSpeedTuning_MakesTheCreatureFullyCustom()
    {
        NPCSpeeds.RegisterSpeed(new NPCSpeeds.SpeedClassEntry
        {
            Level = SpeedLevel.Fast, ActiveSpeed = 0.2, PassiveSpeed = 0.4,
            ActiveMoveSpeed = 0.3, PassiveMoveSpeed = 0.9, Types = new HashSet<Type>()
        });

        var bc = new BucketStub();
        _created.Add(bc);

        bc.ActiveSpeed = 0.25; // one tuned value customizes the whole block

        Assert.Equal(SpeedLevel.None, bc.SpeedClass); // the bucket label never lies

        var writer = new BufferWriter(true);
        bc.Serialize(writer);
        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = new BucketStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        // All four persisted raw - no value is left silently tracking the table.
        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(SpeedLevel.None, copy.SpeedClass);
        Assert.Equal(0.25, copy.ActiveSpeed);
        Assert.Equal(0.4, copy.PassiveSpeed);
        Assert.Equal(0.3, copy.ActiveMoveSpeed);
        Assert.Equal(0.9, copy.PassiveMoveSpeed);
    }

}
