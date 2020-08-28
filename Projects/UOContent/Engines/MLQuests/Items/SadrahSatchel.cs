namespace Server.Items
{
    public class SadrahSatchel : Backpack
    {
        [Constructible]
        public SadrahSatchel()
        {
            Hue = Utility.RandomBrightHue();
            DropItem(new Bloodmoss(10));
            DropItem(new MortarPestle());
        }

        public SadrahSatchel(Serial serial)
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
