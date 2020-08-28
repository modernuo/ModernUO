namespace Server.Items
{
    [TypeAlias("Server.Items.Lollipop")]
    public class Lollipops : CandyCane
    {
        [Constructible]
        public Lollipops(int amount = 1)
            : base(0x468D + Utility.Random(3)) =>
            Stackable = true;

        public Lollipops(Serial serial)
            : base(serial)
        {
        }

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
