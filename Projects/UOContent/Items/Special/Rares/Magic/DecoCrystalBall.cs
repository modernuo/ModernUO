namespace Server.Items
{
    public class DecoCrystalBall : Item
    {
        [Constructible]
        public DecoCrystalBall() : base(0xE2E)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoCrystalBall(Serial serial) : base(serial)
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
