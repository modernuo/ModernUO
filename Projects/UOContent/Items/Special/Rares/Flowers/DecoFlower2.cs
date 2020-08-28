namespace Server.Items
{
    public class DecoFlower2 : Item
    {
        [Constructible]
        public DecoFlower2() : base(0x18D9)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoFlower2(Serial serial) : base(serial)
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
