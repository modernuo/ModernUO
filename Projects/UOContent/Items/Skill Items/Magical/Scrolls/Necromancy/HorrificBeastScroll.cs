namespace Server.Items
{
    public class HorrificBeastScroll : SpellScroll
    {
        [Constructible]
        public HorrificBeastScroll(int amount = 1) : base(105, 0x2265, amount)
        {
        }

        public HorrificBeastScroll(Serial serial) : base(serial)
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
