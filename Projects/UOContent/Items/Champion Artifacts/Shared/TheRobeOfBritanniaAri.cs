namespace Server.Items
{
    public class TheRobeOfBritanniaAri : BaseOuterTorso
    {
        [Constructible]
        public TheRobeOfBritanniaAri() : base(0x2684)
        {
            Hue = 0x48b;
            StrRequirement = 0;
        }

        public TheRobeOfBritanniaAri(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1094931; // The Robe of Britannia "Ari" [Replica]

        public override int BasePhysicalResistance => 10;

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
