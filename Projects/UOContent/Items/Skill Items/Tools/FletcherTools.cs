using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x1022, 0x1023)]
    public class FletcherTools : BaseTool
    {
        [Constructible]
        public FletcherTools() : base(0x1022) => Weight = 2.0;

        [Constructible]
        public FletcherTools(int uses) : base(uses, 0x1022) => Weight = 2.0;

        public FletcherTools(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefBowFletching.CraftSystem;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Weight == 1.0)
            {
                Weight = 2.0;
            }
        }
    }
}
