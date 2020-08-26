namespace Server.Items
{
    public class CrownOfTalKeesh : Bandana
    {
        [Constructible]
        public CrownOfTalKeesh()
        {
            Hue = 0x4F2;

            Attributes.BonusInt = 8;
            Attributes.RegenMana = 4;
            Attributes.SpellDamage = 10;
        }

        public CrownOfTalKeesh(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094903; // Crown of Tal'Keesh [Replica]

        public override int BasePhysicalResistance => 0;
        public override int BaseFireResistance => 5;
        public override int BaseColdResistance => 9;
        public override int BasePoisonResistance => 20;
        public override int BaseEnergyResistance => 20;

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
