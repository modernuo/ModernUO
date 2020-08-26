using Server.Engines.Craft;

namespace Server.Items
{
    public class MortarPestle : BaseTool
    {
        [Constructible]
        public MortarPestle() : base(0xE9B) => Weight = 1.0;

        [Constructible]
        public MortarPestle(int uses) : base(uses, 0xE9B) => Weight = 1.0;

        public MortarPestle(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefAlchemy.CraftSystem;

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
