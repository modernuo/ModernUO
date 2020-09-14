namespace Server.Items
{
    public class AcidProofRobe : Robe
    {
        [Constructible]
        public AcidProofRobe()
        {
            Hue = 0x455;
            LootType = LootType.Blessed;
        }

        public AcidProofRobe(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1095236; // Acid-Proof Robe [Replica]

        public override int BaseFireResistance => 4;

        public override int InitMinHits => 150;
        public override int InitMaxHits => 150;

        public override bool CanFortify => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1 && Hue == 1)
            {
                Hue = 0x455;
            }
        }
    }
}
