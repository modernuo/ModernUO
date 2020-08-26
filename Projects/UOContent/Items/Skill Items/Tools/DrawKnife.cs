using Server.Engines.Craft;

namespace Server.Items
{
    public class DrawKnife : BaseTool
    {
        [Constructible]
        public DrawKnife() : base(0x10E4) => Weight = 1.0;

        [Constructible]
        public DrawKnife(int uses) : base(uses, 0x10E4) => Weight = 1.0;

        public DrawKnife(Serial serial) : base(serial)
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
