namespace Server.Items
{
    public class NightsKiss : Dagger
    {
        [Constructible]
        public NightsKiss()
        {
            ItemID = 0xF51;
            Hue = 0x455;
            WeaponAttributes.HitLeechHits = 40;
            Slayer = SlayerName.Repond;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 35;
        }

        public NightsKiss(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063475;

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
        }
    }
}
