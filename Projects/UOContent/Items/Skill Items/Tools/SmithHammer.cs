using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x13E3, 0x13E4)]
    public class SmithHammer : BaseTool
    {
        [Constructible]
        public SmithHammer() : base(0x13E3)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
        }

        [Constructible]
        public SmithHammer(int uses) : base(uses, 0x13E3)
        {
            Weight = 8.0;
            Layer = Layer.OneHanded;
        }

        public SmithHammer(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;

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
