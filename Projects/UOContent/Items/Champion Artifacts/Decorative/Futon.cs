namespace Server.Items
{
    [Flippable]
    public class Futon : Item
    {
        [Constructible]
        public Futon() : base(Utility.RandomDouble() > 0.5 ? 0x295C : 0x295E)
        {
        }

        public Futon(Serial serial) : base(serial)
        {
        }

        public void Flip()
        {
            ItemID = ItemID switch
            {
                0x295C => 0x295D,
                0x295E => 0x295F,
                0x295D => 0x295C,
                0x295F => 0x295E,
                _      => ItemID
            };
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
