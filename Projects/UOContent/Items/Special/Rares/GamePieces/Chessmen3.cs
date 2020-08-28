namespace Server.Items
{
    public class Chessmen3 : Item
    {
        [Constructible]
        public Chessmen3() : base(0xE14)
        {
            Movable = true;
            Stackable = false;
        }

        public Chessmen3(Serial serial) : base(serial)
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
