namespace Server.Items
{
    public class PlayingCards2 : Item
    {
        [Constructible]
        public PlayingCards2() : base(0xFA2)
        {
            Movable = true;
            Stackable = false;
        }

        public PlayingCards2(Serial serial) : base(serial)
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
