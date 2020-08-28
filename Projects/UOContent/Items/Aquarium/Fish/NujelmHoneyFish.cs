namespace Server.Items
{
    public class NujelmHoneyFish : BaseFish
    {
        [Constructible]
        public NujelmHoneyFish() : base(0x3B06)
        {
        }

        public NujelmHoneyFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073830; // A Nujel'm Honey Fish

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
