namespace Server.Items
{
    public class BladeOfInsanity : Katana
    {
        [Constructible]
        public BladeOfInsanity()
        {
            Hue = 0x76D;
            WeaponAttributes.HitLeechStam = 100;
            Attributes.RegenStam = 2;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 50;
        }

        public BladeOfInsanity(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061088; // Blade of Insanity
        public override int ArtifactRarity => 11;

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Hue == 0x44F)
            {
                Hue = 0x76D;
            }
        }
    }
}
