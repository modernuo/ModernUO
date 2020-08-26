namespace Server.Items
{
    public class HuntersHeaddress : DeerMask
    {
        [Constructible]
        public HuntersHeaddress()
        {
            Hue = 0x594;

            SkillBonuses.SetValues(0, SkillName.Archery, 20);

            Attributes.BonusDex = 8;
            Attributes.NightSight = 1;
            Attributes.AttackChance = 15;
        }

        public HuntersHeaddress(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061595; // Hunter's Headdress

        public override int ArtifactRarity => 11;

        public override int BaseColdResistance => 23;

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
                        Resistances.Cold = 0;
                        break;
                    }
            }
        }
    }
}
