namespace Server.Items
{
    public class AlbinoCourtesanFish : BaseFish
    {
        [Constructible]
        public AlbinoCourtesanFish() : base(0x3B04)
        {
        }

        public AlbinoCourtesanFish(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074592; // Albino Courtesan Fish

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
