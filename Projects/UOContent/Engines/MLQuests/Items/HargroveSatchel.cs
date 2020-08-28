namespace Server.Items
{
    public class HargroveSatchel : Backpack
    {
        [Constructible]
        public HargroveSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new Gold(15));
            DropItem(new Hatchet());
        }

        public HargroveSatchel(Serial serial)
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
