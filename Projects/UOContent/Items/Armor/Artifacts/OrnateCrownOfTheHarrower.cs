namespace Server.Items
{
    public class OrnateCrownOfTheHarrower : BoneHelm
    {
        [Constructible]
        public OrnateCrownOfTheHarrower()
        {
            Hue = 0x4F6;
            Attributes.RegenHits = 2;
            Attributes.RegenStam = 3;
            Attributes.WeaponDamage = 25;
        }

        public OrnateCrownOfTheHarrower(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061095; // Ornate Crown of the Harrower
        public override int ArtifactRarity => 11;

        public override int BasePoisonResistance => 17;

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
                if (Hue == 0x55A)
                {
                    Hue = 0x4F6;
                }

                PoisonBonus = 0;
            }
        }
    }
}
