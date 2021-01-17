namespace Server.Items
{
    public class ScribeStone : Item
    {
        [Constructible]
        public ScribeStone() : base(0xED4)
        {
            Movable = false;
            Hue = 0x105;
        }

        public ScribeStone(Serial serial) : base(serial)
        {
        }

        public override string DefaultName => "a Scribe Supply Stone";

        public override void OnDoubleClick(Mobile from)
        {
            var scribeBag = new ScribeBag();

            if (!from.AddToBackpack(scribeBag))
            {
                scribeBag.Delete();
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
