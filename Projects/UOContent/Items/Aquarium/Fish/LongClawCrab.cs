namespace Server.Items
{
    public class LongClawCrab : BaseFish
    {
        [Constructible]
        public LongClawCrab() : base(0x3AFC) => Hue = 0x527;

        public LongClawCrab(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073827; // A Long Claw Crab

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
