namespace Server.Items
{
    public class HoveringWisp : Item
    {
        [Constructible]
        public HoveringWisp() : base(0x2100)
        {
        }

        public HoveringWisp(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1072881; // hovering wisp

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
