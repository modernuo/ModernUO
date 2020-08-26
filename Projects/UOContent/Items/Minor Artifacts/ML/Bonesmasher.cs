namespace Server.Items
{
    public class Bonesmasher : DiamondMace
    {
        [Constructible]
        public Bonesmasher()
        {
            ItemID = 0x2D30;
            Hue = 0x482;

            SkillBonuses.SetValues(0, SkillName.Macing, 10.0);

            WeaponAttributes.HitLeechMana = 40;
            WeaponAttributes.SelfRepair = 2;
        }

        public Bonesmasher(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075030; // Bonesmasher

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
