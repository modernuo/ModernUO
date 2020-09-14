namespace Server.Items
{
    public class OrnamentOfTheMagician : GoldBracelet
    {
        [Constructible]
        public OrnamentOfTheMagician()
        {
            Hue = 0x554;
            Attributes.CastRecovery = 3;
            Attributes.CastSpeed = 2;
            Attributes.LowerManaCost = 10;
            Attributes.LowerRegCost = 20;
            Resistances.Energy = 15;
        }

        public OrnamentOfTheMagician(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061105; // Ornament of the Magician
        public override int ArtifactRarity => 11;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Hue == 0x12B)
            {
                Hue = 0x554;
            }
        }
    }
}
