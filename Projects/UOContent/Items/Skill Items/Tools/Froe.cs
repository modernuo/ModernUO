using Server.Engines.Craft;

namespace Server.Items
{
    public class Froe : BaseTool
    {
        [Constructible]
        public Froe() : base(0x10E5) => Weight = 1.0;

        [Constructible]
        public Froe(int uses) : base(uses, 0x10E5) => Weight = 1.0;

        public Froe(Serial serial) : base(serial)
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
