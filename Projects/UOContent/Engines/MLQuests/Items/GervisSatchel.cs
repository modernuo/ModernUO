namespace Server.Items
{
    public class GervisSatchel : Backpack
    {
        [Constructible]
        public GervisSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new IronIngot(10));
            DropItem(new SmithHammer());
        }

        public GervisSatchel(Serial serial)
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
