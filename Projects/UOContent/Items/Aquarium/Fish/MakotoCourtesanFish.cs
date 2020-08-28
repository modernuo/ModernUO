namespace Server.Items
{
    public class MakotoCourtesanFish : BaseFish
    {
        [Constructible]
        public MakotoCourtesanFish() : base(0x3AFD)
        {
        }

        public MakotoCourtesanFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073835; // A Makoto Courtesan Fish

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
