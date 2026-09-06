using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SaveStabilityTests
{
    // Serializes the current time, the classic way a record drifts between saves with no mutation.
    private sealed class ClockStampedItem : Item
    {
        public ClockStampedItem() : base(0x1F03)
        {
        }

        public ClockStampedItem(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Core.Now);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadDateTime();
        }
    }

    private sealed class CreatureStub : BaseCreature
    {
        public CreatureStub() : base(AIType.AI_Animal, FightMode.Closest, 10, 1)
        {
            Body = 0xEE; // rat
        }

        public CreatureStub(Serial serial) : base(serial)
        {
        }

        // NPCSpeeds is not configured in the test fixture; the AIType ctor would hit the empty table.
        public override void GetSpeeds(out double activeSpeed, out double passiveSpeed)
        {
            activeSpeed = 0.2;
            passiveSpeed = 0.4;
        }
    }

    [Fact]
    public void StableEntitiesHashIdenticallyAfterTimePasses()
    {
        var bag = new Bag();
        bag.DropItem(new Gold(100));
        bag.DropItem(new Dagger());
        bag.MoveToWorld(new Point3D(1450, 1450, 0), Map.Felucca);

        var creature = new CreatureStub();
        creature.MoveToWorld(new Point3D(1451, 1450, 0), Map.Felucca);

        var entities = new List<ISerializable> { bag, creature };
        entities.AddRange(bag.Items);

        var now = Core._now;

        try
        {
            var captured = SaveStability.CaptureHashes(entities);
            Core._now = now + TimeSpan.FromMinutes(5);

            var report = SaveStability.Compare(entities, captured);

            Assert.Equal(entities.Count, report.Checked);
            Assert.Equal(0, report.Changed);
        }
        finally
        {
            Core._now = now;
            bag.Delete();
            creature.Delete();
        }
    }

    [Fact]
    public void ClockDerivedRecordIsReportedByType()
    {
        var item = new ClockStampedItem();
        item.MoveToWorld(new Point3D(1452, 1450, 0), Map.Felucca);

        var now = Core._now;

        try
        {
            var entities = new List<ISerializable> { item };
            var captured = SaveStability.CaptureHashes(entities);

            // Same tick: the drift is invisible, which is why the command waits in real time.
            Assert.Equal(0, SaveStability.Compare(entities, captured).Changed);

            Core._now = now + TimeSpan.FromMinutes(1);
            var report = SaveStability.Compare(entities, captured);

            Assert.Equal(1, report.Changed);
            Assert.Equal(1, report.ByType[typeof(ClockStampedItem)].Changed);
            Assert.Equal(1, report.ByType[typeof(ClockStampedItem)].Total);
        }
        finally
        {
            Core._now = now;
            item.Delete();
        }
    }

    [Fact]
    public void SnapshotCoversEveryRegisteredEntityPersistence()
    {
        var item = new Bag();
        item.MoveToWorld(new Point3D(1453, 1450, 0), Map.Felucca);
        var mobile = new CreatureStub();
        mobile.MoveToWorld(new Point3D(1454, 1450, 0), Map.Felucca);

        try
        {
            var snapshot = SaveStability.SnapshotEntities();

            Assert.Contains(item, snapshot);
            Assert.Contains(mobile, snapshot);

            var names = new List<string>();
            foreach (var persistence in Persistence.EntityPersistences)
            {
                names.Add(persistence.Name);
            }

            Assert.Contains("Items", names);
            Assert.Contains("Mobiles", names);
            Assert.Contains("Guilds", names);
        }
        finally
        {
            item.Delete();
            mobile.Delete();
        }
    }
}
