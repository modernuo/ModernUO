namespace Server.Items
{
    public class EnchantedTitanLegBone : ShortSpear
    {
        [Constructible]
        public EnchantedTitanLegBone()
        {
            Hue = 0x8A5;
            WeaponAttributes.HitLowerDefend = 40;
            WeaponAttributes.HitLightning = 40;
            Attributes.AttackChance = 10;
            Attributes.WeaponDamage = 20;
            WeaponAttributes.ResistPhysicalBonus = 10;
        }

        public EnchantedTitanLegBone(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1063482;

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
