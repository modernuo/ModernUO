namespace Server.Items
{
    [Flippable(0x312D, 0x312E)]
    public class SeveredElfEars : Item
    {
        [Constructible]
        public SeveredElfEars(int amount = 1) : base(Utility.RandomList(0x312D, 0x312E))
        {
            Stackable = true;
            Amount = amount;
        }

        public SeveredElfEars(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
