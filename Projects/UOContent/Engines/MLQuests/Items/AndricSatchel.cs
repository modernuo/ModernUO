namespace Server.Items
{
    public class AndricSatchel : Backpack
    {
        [Constructible]
        public AndricSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new Feather(10));
            DropItem(new FletcherTools());
        }

        public AndricSatchel(Serial serial)
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
