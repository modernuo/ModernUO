namespace Server.Items
{
    public class AsandosSatchel : Backpack
    {
        [Constructible]
        public AsandosSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new SackFlour());
            DropItem(new Skillet());
        }

        public AsandosSatchel(Serial serial)
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
