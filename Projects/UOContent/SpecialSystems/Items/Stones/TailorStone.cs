namespace Server.Items
{
    public class TailorStone : Item
    {
        [Constructible]
        public TailorStone() : base(0xED4)
        {
            Movable = false;
            Hue = 0x315;
        }

        public TailorStone(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a Tailor Supply Stone";

        public override void OnDoubleClick(Mobile from)
        {
            var tailorBag = new TailorBag();

            if (!from.AddToBackpack(tailorBag))
            {
                tailorBag.Delete();
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
