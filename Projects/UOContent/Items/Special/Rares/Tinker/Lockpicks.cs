namespace Server.Items
{
    public class Lockpicks : Item
    {
        [Constructible]
        public Lockpicks() : base(Utility.Random(2) + 0x14FD)
        {
            Movable = true;
            Stackable = false;
        }

        public Lockpicks(Serial serial) : base(serial)
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
