namespace Server.Items
{
    public class ShaminoCrossbow : RepeatingCrossbow
    {
        [Constructible]
        public ShaminoCrossbow()
        {
            Hue = 0x504;
            LootType = LootType.Blessed;

            Attributes.AttackChance = 15;
            Attributes.WeaponDamage = 40;
            WeaponAttributes.SelfRepair = 10;
            WeaponAttributes.LowerStatReq = 100;
        }

        public ShaminoCrossbow(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1062915; // Shamino�s Best Crossbow

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
