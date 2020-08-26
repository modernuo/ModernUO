using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x1034, 0x1035)]
    public class Saw : BaseTool
    {
        [Constructible]
        public Saw() : base(0x1034) => Weight = 2.0;

        [Constructible]
        public Saw(int uses) : base(uses, 0x1034) => Weight = 2.0;

        public Saw(Serial serial) : base(serial)
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
