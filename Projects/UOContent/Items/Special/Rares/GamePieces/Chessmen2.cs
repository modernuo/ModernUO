namespace Server.Items
{
    public class Chessmen2 : Item
    {
        [Constructible]
        public Chessmen2() : base(0xE12)
        {
            Movable = true;
            Stackable = false;
        }

        public Chessmen2(Serial serial) : base(serial)
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
