namespace Server.Items
{
    public class GlovesOfThePugilist : LeatherGloves
    {
        [Constructible]
        public GlovesOfThePugilist()
        {
            Hue = 0x6D1;
            SkillBonuses.SetValues(0, SkillName.Wrestling, 10.0);
            Attributes.BonusDex = 8;
            Attributes.WeaponDamage = 15;
        }

        public GlovesOfThePugilist(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1070690;

        public override int BasePhysicalResistance => 18;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
