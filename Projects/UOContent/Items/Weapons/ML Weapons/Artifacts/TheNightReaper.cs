namespace Server.Items
{
    public class TheNightReaper : RepeatingCrossbow
    {
        [Constructible]
        public TheNightReaper()
        {
            ItemID = 0x26CD;
            Hue = 0x41C;

            Slayer = SlayerName.Exorcism;
            Attributes.NightSight = 1;
            Attributes.WeaponSpeed = 25;
            Attributes.WeaponDamage = 55;
        }

        public TheNightReaper(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072912; // The Night Reaper

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
