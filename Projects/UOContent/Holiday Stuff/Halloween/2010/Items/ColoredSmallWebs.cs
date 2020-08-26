namespace Server.Items
{
    public class ColoredSmallWebs : Item
    {
        [Constructible]
        public ColoredSmallWebs()
            : base(Utility.RandomBool() ? 0x10d6 : 0x10d7) =>
            Hue = Utility.RandomBool() ? 0x455 : 0x4E9;

        public ColoredSmallWebs(Serial serial)
            : base(serial)
        {
        }

        public override double DefaultWeight => 5;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
