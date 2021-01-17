namespace Server.Items
{
    public class MuggSatchel : Backpack
    {
        [Constructible]
        public MuggSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new Pickaxe());
            DropItem(new Pickaxe());
        }

        public MuggSatchel(Serial serial)
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
