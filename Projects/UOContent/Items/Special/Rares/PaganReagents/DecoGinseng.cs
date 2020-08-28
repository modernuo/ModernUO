namespace Server.Items
{
    public class DecoGinseng : Item
    {
        [Constructible]
        public DecoGinseng() : base(0x18E9)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoGinseng(Serial serial) : base(serial)
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
