namespace Server.Items
{
    public class Aegis : HeaterShield
    {
        [Constructible]
        public Aegis()
        {
            Hue = 0x47E;
            ArmorAttributes.SelfRepair = 5;
            Attributes.ReflectPhysical = 15;
            Attributes.DefendChance = 15;
            Attributes.LowerManaCost = 8;
        }

        public Aegis(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061602; // �gis
        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 15;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 1)
            {
                PhysicalBonus = 0;
            }
        }
    }
}
