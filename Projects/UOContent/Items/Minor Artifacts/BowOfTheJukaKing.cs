namespace Server.Items
{
    public class BowOfTheJukaKing : Bow
    {
        [Constructible]
        public BowOfTheJukaKing()
        {
            Hue = 0x460;
            WeaponAttributes.HitMagicArrow = 25;
            Slayer = SlayerName.ReptilianDeath;
            Attributes.AttackChance = 15;
            Attributes.WeaponDamage = 40;
        }

        public BowOfTheJukaKing(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1070636;

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
