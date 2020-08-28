using Server.Engines.Craft;

namespace Server.Items
{
    public class Hammer : BaseTool
    {
        [Constructible]
        public Hammer() : base(0x102A) => Weight = 2.0;

        [Constructible]
        public Hammer(int uses) : base(uses, 0x102A) => Weight = 2.0;

        public Hammer(Serial serial) : base(serial)
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
