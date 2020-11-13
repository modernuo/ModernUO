namespace Server.Items.Holiday
{
    [TypeAlias("Server.Items.AngelDecoration"), Flippable(0x46FA, 0x46FB)]
    public class AngelDecoration : Item
    {
        public AngelDecoration() : base(0x46FA)
        {
            LootType = LootType.Blessed;

            Weight = 30;
        }

        public AngelDecoration(Serial serial)
            : base(serial)
        {
        }

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
