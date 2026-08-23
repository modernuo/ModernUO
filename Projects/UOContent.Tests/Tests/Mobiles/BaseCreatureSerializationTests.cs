using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

// BaseCreature's move to the SerializationGenerator (v23) is guarded three ways: the new
// SaveFlag format round-trips both a default and a fully-populated creature with exact
// byte consumption, back-to-back saves are byte-identical (freeze-time stability), and a
// byte-authentic pre-codegen v22 stream (written by a fossilized replica of the old
// Serialize) loads through the legacy path with the table-speed migration applied.
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
        Assert.Equal(0.6, copy.ActiveMoveSpeed);   // pulled from the table, not the wire
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

    private sealed class MobileStub : Mobile
    {
        public MobileStub() => Body = 0xC9;
    }

    // Byte-authentic replica of the pre-codegen v22 tail — fossilized so the legacy
    // upgrade path stays covered without an old save binary. The full stream is a plain
    // Mobile section (identical layout for every Mobile subclass) followed by this tail.
    private static void WriteLegacyV22Tail(IGenericWriter writer)
    {
            writer.Write(22);                      // version
            writer.Write((int)AIType.AI_Melee);    // current AI
            writer.Write((int)AIType.AI_Melee);    // default AI
            writer.Write(10);                      // RangePerception
            writer.Write(1);                       // RangeFight
            writer.Write(0);                       // Team
            writer.Write(0.3);                     // active (matches the stub table)
            writer.Write(0.6);                     // passive
            writer.Write(0.6);                     // current
            writer.Write(2000);                    // Home X
            writer.Write(2100);                    // Home Y
            writer.Write(7);                       // Home Z
            writer.Write(6);                       // RangeHome
            writer.Write((int)FightMode.Closest);
            writer.Write(false);                   // controlled
            writer.Write((Mobile)null);            // control master
            writer.Write((Mobile)null);            // control target
            writer.Write(Point3D.Zero);            // control dest
            writer.Write((int)OrderType.None);
            writer.Write(0.0);                     // min tame skill
            writer.Write(true);                    // tamable
            writer.Write(false);                   // summoned
            writer.Write(2);                       // control slots
            writer.Write(73);                      // loyalty
            writer.Write((Item)null);              // waypoint
            writer.Write((Mobile)null);            // summon master
            writer.Write(180);                     // hits seed
            writer.Write(-1);                      // stam seed
            writer.Write(-1);                      // mana seed
            writer.Write(7);                       // damage min
            writer.Write(14);                      // damage max
            writer.Write(30);                      // phys resist
            writer.Write(100);                     // phys damage
            writer.Write(10);                      // fire resist
            writer.Write(0);                       // fire damage
            writer.Write(0);                       // cold resist
            writer.Write(0);                       // cold damage
            writer.Write(0);                       // poison resist
            writer.Write(0);                       // poison damage
            writer.Write(0);                       // energy resist
            writer.Write(0);                       // energy damage
            writer.Write(new List<Mobile>());      // owners
            writer.Write(false);                   // dead pet
            writer.Write(false);                   // bonded
            writer.Write(DateTime.MinValue);       // bonding begin
            writer.Write(DateTime.MinValue);       // abandon time
            writer.Write(true);                    // has generated loot
            writer.Write(false);                   // paragon
            writer.Write(false);                   // has friends
            writer.Write(false);                   // remove if untamed
            writer.Write(0);                       // remove step
            writer.Write(TimeSpan.Zero);           // delete time left
            writer.Write((string)null);            // corpse name override
            writer.Write((Map)null);               // home map
            writer.Write(0.0);                     // active move speed (v22)
            writer.Write(0.0);                     // passive move speed (v22)
    }

    [Fact]
    public void LegacyV22Stream_LoadsThroughLegacyPath()
    {
        // Every serialized BaseCreature starts with the Mobile base section; a plain
        // Mobile donor produces a byte-authentic one.
        var donor = new MobileStub();
        donor.DefaultMobileInit();
        _created.Add(donor);

        var writer = new BufferWriter(true);
        donor.Serialize(writer);
        WriteLegacyV22Tail(writer);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = new CreatureStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(10, copy.RangePerception);
        Assert.Equal(new Point3D(2000, 2100, 7), copy.Home);
        Assert.Equal(6, copy.RangeHome);
        Assert.True(copy.Tamable);
        Assert.Equal(2, copy.ControlSlots);
        Assert.Equal(73, copy.Loyalty);
        Assert.Equal(180, copy.HitsMaxSeed);
        Assert.Equal(7, copy.DamageMin);
        Assert.Equal(14, copy.DamageMax);
        Assert.Equal(30, copy.PhysicalResistanceSeed);
        Assert.Equal(10, copy.FireResistSeed);
        Assert.Equal(0.3, copy.ActiveSpeed);
        // v22 wrote explicit zeros for the move overrides ("inherit"), so the resolved
        // pace falls back to the think clock.
        Assert.Equal(0, copy.ActiveMoveSpeed);
        Assert.Equal(0.6, copy.CurrentMoveSpeed); // passive mode, inheriting
    }
}
