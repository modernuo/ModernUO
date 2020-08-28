namespace Server.Items
{
    public class DecoFlower : Item
    {
        [Constructible]
        public DecoFlower() : base(0x18DA)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoFlower(Serial serial) : base(serial)
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
