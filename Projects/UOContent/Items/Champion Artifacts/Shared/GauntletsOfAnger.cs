namespace Server.Items
{
    public class GuantletsOfAnger : PlateGloves
    {
        [Constructible]
        public GuantletsOfAnger()
        {
            Hue = 0x29b;

            Attributes.BonusHits = 8;
            Attributes.RegenHits = 2;
            Attributes.DefendChance = 10;
        }

        public GuantletsOfAnger(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094902; // Gauntlets of Anger [Replica]

        public override int BasePhysicalResistance => 4;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 5;
        public override int BasePoisonResistance => 6;
        public override int BaseEnergyResistance => 5;

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
