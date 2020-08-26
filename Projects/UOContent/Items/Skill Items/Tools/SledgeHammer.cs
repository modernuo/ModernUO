using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0xFB5, 0xFB4)]
    public class SledgeHammer : BaseTool
    {
        [Constructible]
        public SledgeHammer() : base(0xFB5) => Layer = Layer.OneHanded;

        [Constructible]
        public SledgeHammer(int uses) : base(uses, 0xFB5) => Layer = Layer.OneHanded;

        public SledgeHammer(Serial serial) : base(serial)
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
