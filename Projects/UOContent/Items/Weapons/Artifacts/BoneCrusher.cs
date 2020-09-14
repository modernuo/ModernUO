namespace Server.Items
{
    public class BoneCrusher : WarMace
    {
        [Constructible]
        public BoneCrusher()
        {
            ItemID = 0x1406;
            Hue = 0x60C;
            WeaponAttributes.HitLowerDefend = 50;
            Attributes.BonusStr = 10;
            Attributes.WeaponDamage = 75;
        }

        public BoneCrusher(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1061596; // Bone Crusher
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

            if (Hue == 0x604)
            {
                Hue = 0x60C;
            }

            if (ItemID == 0x1407)
            {
                ItemID = 0x1406;
            }
        }
    }
}
