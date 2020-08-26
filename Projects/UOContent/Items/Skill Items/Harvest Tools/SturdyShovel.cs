using Server.Engines.Harvest;

namespace Server.Items
{
    public class SturdyShovel : BaseHarvestTool
    {
        [Constructible]
        public SturdyShovel(int uses = 180) : base(0xF39, uses)
        {
            Weight = 5.0;
            Hue = 0x973;
        }

        public SturdyShovel(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1045125; // sturdy shovel
        public override HarvestSystem HarvestSystem => Mining.System;

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
