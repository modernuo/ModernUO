using Server.Engines.Craft;

namespace Server.Items
{
    [Flippable(0x0FBF, 0x0FC0)]
    public class MapmakersPen : BaseTool
    {
        [Constructible]
        public MapmakersPen() : base(0x0FBF) => Weight = 1.0;

        [Constructible]
        public MapmakersPen(int uses) : base(uses, 0x0FBF) => Weight = 1.0;

        public MapmakersPen(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefCartography.CraftSystem;

        public override int LabelNumber => 1044167; // mapmaker's pen

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Weight == 2.0)
            {
                Weight = 1.0;
            }
        }
    }
}
