namespace Server.Items
{
    public class Taffy : CandyCane
    {
        [Constructible]
        public Taffy(int amount = 1)
            : base(0x469D) =>
            Stackable = true;

        public Taffy(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1096949; /* taffy */

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
