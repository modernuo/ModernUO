namespace Server.Items
{
    public class ExcellentIronMaiden : Item
    {
        [Constructible]
        public ExcellentIronMaiden()
            : base(0x3f15)
        {
        }

        public ExcellentIronMaiden(Serial serial)
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
