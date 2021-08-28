namespace Server.Items
{
    public class SpiritOfTheTotem : BearMask
    {
        [Constructible]
        public SpiritOfTheTotem()
        {
            Hue = 0x455;

            Attributes.BonusStr = 20;
            Attributes.ReflectPhysical = 15;
            Attributes.AttackChance = 15;
        }

        public SpiritOfTheTotem(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061599; // Spirit of the Totem

        public override int ArtifactRarity => 11;

        public override int BasePhysicalResistance => 20;

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

            switch (version)
            {
                case 0:
                    {
                        Resistances.Physical = 0;
                        break;
                    }
            }
        }
    }
}
