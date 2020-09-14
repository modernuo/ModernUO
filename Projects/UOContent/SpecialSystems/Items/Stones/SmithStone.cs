namespace Server.Items
{
    public class SmithStone : Item
    {
        [Constructible]
        public SmithStone() : base(0xED4)
        {
            Movable = false;
            Hue = 0x476;
        }

        public SmithStone(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a Blacksmith Supply Stone";

        public override void OnDoubleClick(Mobile from)
        {
            var SmithBag = new SmithBag();

            if (!from.AddToBackpack(SmithBag))
            {
                SmithBag.Delete();
            }
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
