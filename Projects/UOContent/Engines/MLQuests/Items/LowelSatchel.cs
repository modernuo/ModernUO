namespace Server.Items
{
    public class LowelSatchel : Backpack
    {
        [Constructible]
        public LowelSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new Board(10));
            DropItem(new DovetailSaw());
        }

        public LowelSatchel(Serial serial)
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
