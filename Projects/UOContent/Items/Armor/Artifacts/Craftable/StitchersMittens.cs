namespace Server.Items
{
    public class StitchersMittens : LeafGloves
    {
        [Constructible]
        public StitchersMittens()
        {
            Hue = 0x481;

            SkillBonuses.SetValues(0, SkillName.Healing, 10.0);

            Attributes.BonusDex = 5;
            Attributes.LowerRegCost = 30;
        }

        public StitchersMittens(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072932; // Stitcher's Mittens

        public override int BasePhysicalResistance => 20;
        public override int BaseColdResistance => 20;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
