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
    private sealed class UnstableItem : Item
    {
        public UnstableItem() : base(0x1F03)
        {
        }

        public UnstableItem(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_pass++); // deliberately changes every call
        }

        private int _pass;

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
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
    public void StableEntitiesProduceIdenticalBytesTwice()
    {
        var bag = new Bag();
        bag.DropItem(new Gold(100));
        bag.DropItem(new Dagger());
        bag.MoveToWorld(new Point3D(1450, 1450, 0), Map.Felucca);

        var creature = new CreatureStub();
        creature.MoveToWorld(new Point3D(1451, 1450, 0), Map.Felucca);

        var entities = new List<ISerializable> { bag, creature };
        entities.AddRange(bag.Items);

        try
        {
            var report = SaveStability.Check(entities);

            Assert.Equal(entities.Count, report.Checked);
            Assert.Equal(0, report.Unstable);
        }
        finally
        {
            bag.Delete();
            creature.Delete();
        }
    }

    [Fact]
    public void UnstableEntityIsReportedByTypeWithOffset()
    {
        var item = new UnstableItem();
        item.MoveToWorld(new Point3D(1452, 1450, 0), Map.Felucca);

        try
        {
            var report = SaveStability.Check([item]);

            Assert.Equal(1, report.Unstable);
            Assert.Equal(1, report.UnstableByType[typeof(UnstableItem)]);
            Assert.Single(report.Examples);
            Assert.True(report.Examples[0].Offset > 0);
        }
        finally
        {
            item.Delete();
        }
    }
}
