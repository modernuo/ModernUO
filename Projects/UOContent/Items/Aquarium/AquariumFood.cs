namespace Server.Items
{
    public class AquariumFood : Item
    {
        [Constructible]
        public AquariumFood() : base(0xEFC)
        {
        }

        public AquariumFood(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074819; // Aquarium food

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
