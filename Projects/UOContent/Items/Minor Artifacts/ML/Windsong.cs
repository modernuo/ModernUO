namespace Server.Items
{
    public class Windsong : MagicalShortbow
    {
        [Constructible]
        public Windsong()
        {
            Hue = 0xF7;

            Attributes.WeaponDamage = 35;
            WeaponAttributes.SelfRepair = 3;

            Velocity = 25;
        }

        public Windsong(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075031; // Windsong

        public override int InitMinHits => 255;
        public override int InitMaxHits => 255;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
