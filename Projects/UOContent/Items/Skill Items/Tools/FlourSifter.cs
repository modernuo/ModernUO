using Server.Engines.Craft;

namespace Server.Items
{
    public class FlourSifter : BaseTool
    {
        [Constructible]
        public FlourSifter() : base(0x103E) => Weight = 1.0;

        [Constructible]
        public FlourSifter(int uses) : base(uses, 0x103E) => Weight = 1.0;

        public FlourSifter(Serial serial) : base(serial)
        {
        }

        public override CraftSystem CraftSystem => DefCooking.CraftSystem;

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
