using System;
using System.Collections.Generic;
using Server;
using Server.Items;
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
    private readonly List<Item> _createdItems = new();

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }

        for (var i = 0; i < _createdItems.Count; i++)
        {
            _createdItems[i].Delete();
        }
    }

    private class CreatureStub : BaseCreature
    {
        public CreatureStub() : base(AIType.AI_Melee) => Body = 0xC9;

        public CreatureStub(Serial serial) : base(serial) => Body = 0xC9;

        public DateTime SummonEndValue => SummonEnd;

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

    // ReadEntity resolves references through the world table, so the master must be registered.
    private PlayerMobile NewMaster()
    {
        var master = new PlayerMobile(World.NewMobile);
        master.DefaultMobileInit();
        World.AddEntity(master);
        _created.Add(master);
        return master;
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
        var master = NewMaster();

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

    // One serialized master; the Controlled and Summoned flags decide which views expose it.
    [Theory]
    [InlineData(true, false)]  // controlled pet
    [InlineData(true, true)]   // controlled summon
    [InlineData(false, true)]  // energy vortex: summoned, never controlled
    [InlineData(false, false)] // EnragedCreature: a summon master without Summoned
    public void MasterShapes_RoundTrip(bool controlled, bool summoned)
    {
        var bc = NewCreature();
        var master = NewMaster();

        bc.Summoned = summoned;

        if (controlled)
        {
            bc.SetControlMaster(master);
        }
        else
        {
            bc.Master = master;
        }

        var copy = Load(Snapshot(bc));

        Assert.Equal(controlled, copy.Controlled);
        Assert.Equal(summoned, copy.Summoned);
        Assert.Equal(master, copy.Master);
        Assert.Equal(controlled ? master : null, copy.ControlMaster);
        Assert.Equal(summoned ? master : null, copy.SummonMaster);
    }

    [Fact]
    public void ReferenceFields_RoundTrip()
    {
        var bc = NewCreature();
        var friend = NewMaster();
        var wayPoint = new WayPoint();
        _createdItems.Add(wayPoint);

        bc.AddPetFriend(friend);
        bc.CurrentWayPoint = wayPoint;
        bc.HomeMap = Map.Felucca;

        var copy = Load(Snapshot(bc));

        Assert.Equal(friend, Assert.Single(copy.Friends));
        Assert.Equal(wayPoint, copy.CurrentWayPoint);
        Assert.Equal(Map.Felucca, copy.HomeMap);
    }

    [Fact]
    public void RunningDeleteTimer_RoundTrips()
    {
        var bc = NewCreature();
        bc.BeginDeleteTimer();
        Assert.True(bc.DeleteTimeLeft > TimeSpan.Zero);

        var copy = Load(Snapshot(bc));

        // Anchored: the remaining countdown survives, not the absolute deadline.
        Assert.InRange(copy.DeleteTimeLeft, TimeSpan.FromDays(3.0) - TimeSpan.FromSeconds(5), TimeSpan.FromDays(3.0));
    }

    private sealed class VendorStub : BaseVendor
    {
        private static readonly List<SBInfo> _sbInfos = [];

        public VendorStub() : base("the stub")
        {
        }

        public VendorStub(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => _sbInfos;

        public override void InitSBInfo()
        {
        }

        public override void InitOutfit()
        {
        }

        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.3;
            passiveSpeed = 0.6;
        }
    }

    // BaseVendor is generated on top of the generated BaseCreature; the chain must write
    // and read both sections in order with exact consumption.
    [Fact]
    public void GeneratedVendorChain_RoundTrips()
    {
        var vendor = new VendorStub();
        _created.Add(vendor);
        vendor.Home = new Point3D(1500, 1600, 0);
        vendor.RangeHome = 2;

        var buffer = Snapshot(vendor);

        var copy = new VendorStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(AIType.AI_Vendor, copy.AI);
        Assert.Equal(FightMode.None, copy.FightMode);
        Assert.Equal(new Point3D(1500, 1600, 0), copy.Home);
        Assert.Equal(2, copy.RangeHome);
    }

    private sealed class MobileStub : Mobile
    {
        public MobileStub() => Body = 0xC9;
    }

    // Byte-authentic replica of the pre-codegen v22 tail — fossilized so the legacy
    // upgrade path stays covered without an old save binary. The full stream is a plain
    // Mobile section (identical layout for every Mobile subclass) followed by this tail.
    private static void WriteLegacyV22Tail(
        IGenericWriter writer,
        bool controlled = false,
        Mobile controlMaster = null,
        bool summoned = false,
        Mobile summonMaster = null,
        DateTime summonEnd = default
    )
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
        writer.Write(controlled);
        writer.Write(controlMaster);
        writer.Write((Mobile)null);            // control target
        writer.Write(Point3D.Zero);            // control dest
        writer.Write((int)OrderType.None);
        writer.Write(0.0);                     // min tame skill
        writer.Write(true);                    // tamable
        writer.Write(summoned);
        if (summoned)
        {
            writer.WriteAnchoredTime(summonEnd);
        }

        writer.Write(2);                       // control slots
        writer.Write(73);                      // loyalty
        writer.Write((Item)null);              // waypoint
        writer.Write(summonMaster);
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

    private CreatureStub LoadLegacyV22(
        bool controlled = false,
        Mobile controlMaster = null,
        bool summoned = false,
        Mobile summonMaster = null,
        DateTime summonEnd = default
    )
    {
        // Every serialized BaseCreature starts with the Mobile base section; a plain
        // Mobile donor produces a byte-authentic one.
        var donor = new MobileStub();
        donor.DefaultMobileInit();
        _created.Add(donor);

        var writer = new BufferWriter(true);
        donor.Serialize(writer);
        WriteLegacyV22Tail(writer, controlled, controlMaster, summoned, summonMaster, summonEnd);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = new CreatureStub(World.NewMobile);
        _created.Add(copy);
        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        return copy;
    }

    [Fact]
    public void LegacyV22Stream_LoadsThroughLegacyPath()
    {
        var copy = LoadLegacyV22();

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

    // A master survives without Summoned (EnragedCreature), and a controlled summon reports the
    // same mobile as both.
    [Theory]
    [InlineData(true, true, false, false)]  // controlled pet
    [InlineData(true, true, true, true)]    // controlled summon, SummonEnd on the wire
    [InlineData(false, false, false, true)] // EnragedCreature shape: SummonMaster only
    public void LegacyV22Stream_KeepsBothMasterReferences(
        bool controlled,
        bool hasControlMaster,
        bool summoned,
        bool hasSummonMaster
    )
    {
        var master = NewMaster();
        var summonEnd = Core.Now + TimeSpan.FromMinutes(5);

        var copy = LoadLegacyV22(
            controlled,
            hasControlMaster ? master : null,
            summoned,
            hasSummonMaster ? master : null,
            summonEnd
        );

        Assert.Equal(controlled, copy.Controlled);
        Assert.Equal(summoned, copy.Summoned);
        Assert.Equal(hasControlMaster ? master : null, copy.ControlMaster);
        Assert.Equal(master, copy.Master);
        Assert.Equal(summoned ? master : null, copy.SummonMaster);

        if (summoned)
        {
            Assert.InRange(copy.SummonEndValue, summonEnd - TimeSpan.FromSeconds(1), summonEnd + TimeSpan.FromSeconds(1));
        }
    }

    // Before summon ownership followed transfers, a traded summon kept its caster as summon master.
    [Fact]
    public void LegacyV22Stream_DifferingMasters_OwnerWins()
    {
        var owner = NewMaster();
        var caster = NewMaster();

        var copy = LoadLegacyV22(true, owner, true, caster, Core.Now + TimeSpan.FromMinutes(5));

        Assert.Equal(owner, copy.ControlMaster);
        Assert.Equal(owner, copy.SummonMaster);
    }

    // v23 bit positions: every save-flagged field in schema order (DefaultAI is not flagged).
    private const int V23Controlled = 13;
    private const int V23ControlMaster = 14;
    private const int V23Summoned = 20;
    private const int V23SummonEnd = 21;
    private const int V23SummonMaster = 22;
    private const int V23RemoveStep = 50;
    private const int V23CorpseNameOverride = 52;

    // v23 wrote the owner and the summoner to separate slots; v24 keeps one master.
    [Fact]
    public void V23Stream_MigratesToOneMaster_AndDefaultsAbsentFields()
    {
        var owner = NewMaster();
        var caster = NewMaster();
        var summonEnd = Core.Now + TimeSpan.FromMinutes(5);

        var donor = new MobileStub();
        donor.DefaultMobileInit();
        _created.Add(donor);

        var writer = new BufferWriter(true);
        donor.Serialize(writer);

        writer.Write(23); // version
        writer.Write(
            1UL << V23Controlled | 1UL << V23ControlMaster | 1UL << V23Summoned | 1UL << V23SummonEnd |
            1UL << V23SummonMaster | 1UL << V23RemoveStep | 1UL << V23CorpseNameOverride
        );
        writer.WriteEncodedInt((int)AIType.AI_Melee); // DefaultAI
        writer.Write(owner);                          // ControlMaster
        writer.WriteAnchoredTime(summonEnd);          // SummonEnd
        writer.Write(caster);                         // SummonMaster
        writer.WriteEncodedInt(3);                    // RemoveStep
        writer.Write("a migrated corpse");            // CorpseNameOverride

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);

        var copy = Load(buffer);

        Assert.True(copy.Controlled);
        Assert.True(copy.Summoned);
        Assert.Equal(owner, copy.ControlMaster);
        Assert.Equal(owner, copy.SummonMaster);
        Assert.InRange(copy.SummonEndValue, summonEnd - TimeSpan.FromSeconds(1), summonEnd + TimeSpan.FromSeconds(1));
        Assert.Equal(3, copy.RemoveStep);
        Assert.Equal("a migrated corpse", copy.CorpseNameOverride);

        // Absent fields take the class defaults, not default(T).
        Assert.Equal(AIType.AI_Melee, copy.AI);
        Assert.Equal(-1, copy.HitsMaxSeed);
        Assert.Equal(1, copy.ControlSlots);
        Assert.Equal(BaseCreature.MaxLoyalty, copy.Loyalty);
        Assert.Equal(100, copy.PhysicalDamage);
        Assert.Equal(10, copy.RangeHome);
        Assert.Equal(FightMode.Closest, copy.FightMode);
        Assert.Equal(0.3, copy.ActiveSpeed);
        Assert.Equal(0.6, copy.PassiveSpeed);
        Assert.NotNull(copy.Owners);
    }
}
