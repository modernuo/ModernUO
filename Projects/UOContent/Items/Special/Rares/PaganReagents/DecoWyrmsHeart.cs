namespace Server.Items
{
    public class DecoWyrmsHeart : Item
    {
        [Constructible]
        public DecoWyrmsHeart() : base(0xF91)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoWyrmsHeart(Serial serial) : base(serial)
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
