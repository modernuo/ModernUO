using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x102C, 0x102D)]
    public class MouldingPlane : BaseTool
    {
        [Constructible]
        public MouldingPlane() : base(0x102C) => Weight = 2.0;

        [Constructible]
        public MouldingPlane(int uses) : base(uses, 0x102C) => Weight = 2.0;

        public MouldingPlane(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefCarpentry.CraftSystem;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
