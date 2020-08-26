namespace Server.Items
{
    public class DecoArrowShafts : Item
    {
        [Constructible]
        public DecoArrowShafts() : base(Utility.Random(2) + 0x1024)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoArrowShafts(Serial serial) : base(serial)
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
