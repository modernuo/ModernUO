namespace Server.Items
{
    public class PumpkinScarecrow : Item
    {
        [Constructible]
        public PumpkinScarecrow()
            : base(Utility.RandomBool() ? 0x469B : 0x469C)
        {
        }

        public PumpkinScarecrow(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1096947;

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
