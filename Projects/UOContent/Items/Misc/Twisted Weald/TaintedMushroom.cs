namespace Server.Items
{
    public class TaintedMushroom : Item
    {
        [Constructible]
        public TaintedMushroom() : base(Utility.RandomMinMax(0x222E, 0x2231))
        {
        }

        public TaintedMushroom(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1075088; // Dread Horn Tainted Mushroom
        public override bool ForceShowProperties => true;

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
