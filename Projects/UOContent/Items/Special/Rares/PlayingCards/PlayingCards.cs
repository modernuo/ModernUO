namespace Server.Items
{
    public class PlayingCards : Item
    {
        [Constructible]
        public PlayingCards() : base(0xFA3)
        {
            Movable = true;
            Stackable = false;
        }

        public PlayingCards(Serial serial) : base(serial)
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
