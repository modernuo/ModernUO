namespace Server.Items
{
    public class GladiatorsCollar : PlateGorget
    {
        [Constructible]
        public GladiatorsCollar()
        {
            Hue = 0x26d;

            Attributes.BonusHits = 10;
            Attributes.AttackChance = 10;

            ArmorAttributes.MageArmor = 1;
        }

        public GladiatorsCollar(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094917; // Gladiator's Collar [Replica]

        public override int BasePhysicalResistance => 18;
        public override int BaseFireResistance => 18;
        public override int BaseColdResistance => 17;
        public override int BasePoisonResistance => 18;
        public override int BaseEnergyResistance => 16;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

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
