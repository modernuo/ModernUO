namespace Server.Items
{
    public class DragonFlameSectBadge : Item
    {
        [Constructible]
        public DragonFlameSectBadge() : base(0x23E) => LootType = LootType.Blessed;

        public DragonFlameSectBadge(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073141; // A Dragon Flame Sect Badge

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
