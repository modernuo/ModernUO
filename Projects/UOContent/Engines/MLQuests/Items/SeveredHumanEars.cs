namespace Server.Items
{
    [Flippable(0x312F, 0x3130)]
    public class SeveredHumanEars : Item
    {
        [Constructible]
        public SeveredHumanEars(int amount = 1) : base(Utility.RandomList(0x312F, 0x3130))
        {
            Stackable = true;
            Amount = amount;
        }

        public SeveredHumanEars(Serial serial) : base(serial)
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
