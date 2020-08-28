namespace Server.Items
{
    public class DecoPumice : Item
    {
        [Constructible]
        public DecoPumice() : base(0xF8B)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoPumice(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
